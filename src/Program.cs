// win-open —— macOS 风格的 `open` 命令，Windows 版。
// 单个 exe，不依赖运行库之外的任何东西；用系统自带的 .NET Framework 4.x 编译器编译（见 build.ps1）。
//
//   open                      打开当前目录（资源管理器）
//   open <路径|网址> ...      文件用默认程序、目录用资源管理器、网址用默认浏览器
//   open -a <程序> <路径>     用指定程序打开（程序名、.cmd/.bat 或完整路径都行）
//   open -R <路径>            在资源管理器里定位并选中（mac 的 Reveal in Finder）
//   open -e / -t <路径>       用编辑器打开（$VISUAL、$EDITOR、codium、code、notepad 依次尝试）
//   open -W ...               等程序退出再返回
//   open -n ...               只打印会做什么，不真的打开
//
// Git Bash 里不需要任何转换：MSYS 会自动把 /c/Users/... 这种路径转成 C:\Users\... 再传给原生 exe；
// 网址含 :// 不会被转。检测到 MSYSTEM 环境变量时把输出切到 UTF-8，中文不乱码。

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

[assembly: AssemblyTitle("open")]
[assembly: AssemblyDescription("macOS-style open command for Windows")]
[assembly: AssemblyProduct("win-open")]
[assembly: AssemblyCopyright("Copyright (c) 2026 cee. MIT License.")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

static class Program
{
    const string Version = "1.0.0";

    static int Main(string[] argv)
    {
        if (Environment.GetEnvironmentVariable("MSYSTEM") != null)
            Console.OutputEncoding = Encoding.UTF8;   // Git Bash 的终端是 UTF-8

        string app = null;
        bool reveal = false, edit = false, dry = false, wait = false;
        var targets = new List<string>();

        for (int i = 0; i < argv.Length; i++)
        {
            string a = argv[i];
            if (a == "--") { for (int j = i + 1; j < argv.Length; j++) targets.Add(argv[j]); break; }
            else if (a == "-h" || a == "--help" || a == "/?") { Usage(); return 0; }
            else if (a == "-v" || a == "--version") { Console.WriteLine("open " + Version); return 0; }
            else if (a == "-a")
            {
                if (++i >= argv.Length) { Err("-a 后面要跟程序名 / -a needs an application"); return 2; }
                app = argv[i];
            }
            else if (a == "-R") reveal = true;
            else if (a == "-e" || a == "-t") edit = true;
            else if (a == "-W" || a == "--wait") wait = true;
            else if (a == "-n" || a == "--dry-run") dry = true;
            else if (a.Length > 1 && a[0] == '-' && !File.Exists(a) && !Directory.Exists(a))
            {
                Err("不认识的选项 / unknown option: " + a);
                Usage();
                return 2;
            }
            else targets.Add(a);
        }
        if (targets.Count == 0) targets.Add(".");

        int rc = 0;
        foreach (string t in targets)
        {
            bool isUrl = Regex.IsMatch(t, @"^[A-Za-z][A-Za-z0-9+.\-]*://") || Regex.IsMatch(t, @"^(mailto|tel|sms):");
            string target = t;
            if (!isUrl)
            {
                if (File.Exists(t) || Directory.Exists(t)) target = Path.GetFullPath(t);
                else { Err(t + " 不存在 / no such file or directory"); rc = 1; continue; }
            }
            try
            {
                if (reveal)
                {
                    if (isUrl) { Err("-R 不能用于网址 / -R does not apply to URLs: " + t); rc = 1; continue; }
                    Run("explorer.exe", "/select," + Quote(target), dry, wait);
                }
                else if (app != null) Run(Resolve(app), Quote(target), dry, wait);
                else if (edit) Run(Editor(), Quote(target), dry, wait);
                else if (dry) Console.WriteLine("默认程序 / default handler " + Quote(target));
                else
                {
                    var p = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                    if (wait && p != null) p.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Err("打不开 / cannot open " + t + ": " + ex.Message);
                rc = 1;
            }
        }
        return rc;
    }

    static void Run(string exe, string args, bool dry, bool wait)
    {
        if (dry) { Console.WriteLine(exe + " " + args); return; }
        var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true });
        if (wait && p != null) p.WaitForExit();
    }

    static string Quote(string s) { return "\"" + s + "\""; }

    // 编辑器优先级：$VISUAL、$EDITOR、codium、code、notepad。只取第一个词当程序名。
    static string Editor()
    {
        string[] cands = { Environment.GetEnvironmentVariable("VISUAL"), Environment.GetEnvironmentVariable("EDITOR"), "codium", "code", "notepad" };
        foreach (string c in cands)
        {
            if (string.IsNullOrEmpty(c) || c.Trim().Length == 0) continue;
            string exe = c.Trim().Split(' ')[0];
            string found = FindOnPath(exe);
            if (found != null) return found;
        }
        return "notepad";
    }

    // ShellExecute 不会给 "codium" 补 .cmd，所以自己沿 PATH + PATHEXT 找一遍；找不到就原样交给系统。
    static string Resolve(string exe)
    {
        if (File.Exists(exe)) return Path.GetFullPath(exe);
        return FindOnPath(exe) ?? exe;
    }

    static string FindOnPath(string exe)
    {
        if (exe.IndexOfAny(new[] { '\\', '/' }) >= 0) return File.Exists(exe) ? Path.GetFullPath(exe) : null;
        string[] exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';');
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            string d = dir.Trim();
            if (d.Length == 0) continue;
            string b;
            try { b = Path.Combine(d, exe); } catch (ArgumentException) { continue; }
            // 先试 PATHEXT 里的扩展名，再看裸文件名：VS Code / VSCodium 的 bin 目录里同时放着
            // 给 Unix 用的无扩展名 shell 脚本 "codium" 和给 Windows 用的 "codium.cmd"，裸名优先会选错。
            foreach (string e in exts) if (e.Length > 0 && File.Exists(b + e)) return b + e;
            if (File.Exists(b) && HasExecExt(b, exts)) return b;
        }
        return null;
    }

    static bool HasExecExt(string path, string[] exts)
    {
        string ext = Path.GetExtension(path);
        foreach (string e in exts) if (string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static void Err(string m) { Console.Error.WriteLine("open: " + m); }

    static void Usage()
    {
        Console.WriteLine(
@"用法 / usage: open [-a 程序] [-R] [-e|-t] [-W] [-n] [--] [路径|网址 ...]
  无参数        打开当前目录                       (no args: open the current folder)
  -a 程序       用指定程序打开                     (open with the given application)
  -R            在资源管理器中定位选中             (reveal in Explorer)
  -e, -t        用编辑器打开 $VISUAL/$EDITOR/codium/code/notepad   (open in editor)
  -W            等程序退出再返回                   (wait for the application to exit)
  -n            只打印动作，不执行                 (dry run)
  -v            版本                               (version)");
    }
}
