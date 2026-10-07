# win-open

macOS 的 `open` 命令，Windows 版。一个不到 10 KB 的 `open.exe`，在 PowerShell、cmd、Git Bash 里都能直接用，不依赖 Node、Python 或任何系统自带 .NET Framework 之外的东西。

[English below](#english)

```
open                      # 在资源管理器里打开当前目录
open report.pdf           # 用默认程序打开文件
open https://example.com  # 用默认浏览器打开网址
open -R notes.md          # 在资源管理器里定位并选中（mac 的 Reveal in Finder）
open -e notes.md          # 用编辑器打开（$VISUAL / $EDITOR / codium / code / notepad）
open -a notepad notes.md  # 用指定程序打开
open -W -a mspaint a.png  # 等程序退出再返回
open -n .                 # 只打印会做什么，不真的打开
```

## 为什么要有它

Windows 自带的等价物是 `start`，但 `start "" "文件"` 的引号规则反直觉，PowerShell 里又得写 `Invoke-Item`，Git Bash 里两个都不顺手。现有的开源方案各缺一块：

| 方案 | 形态 | 覆盖的 shell | mac 风格参数 |
|---|---|---|---|
| [neosmart/open](https://github.com/neosmart/open) | C++ exe | cmd、WSL | 无，只做"双击等价" |
| [winopen](https://github.com/yannlugrin/winopen) / [wsl-open](https://github.com/4U6U57/wsl-open) | Shell 脚本 | **仅 WSL** | winopen 有全套 |
| [open-cli](https://github.com/sindresorhus/open-cli) | Node 包，命令名 `open-cli` | 跨平台 | `-- 程序`、`--wait`，无 `-R`、无编辑器 |
| **win-open** | 单个 exe | PowerShell、cmd、Git Bash | `-a` `-R` `-e` `-W` `-n` |

## 安装

任选一种。装完新开一个终端，输入 `open -v` 应打印版本号。

**winget**

```powershell
winget install ceeyang.win-open
```

**Scoop**

```powershell
scoop bucket add win-open https://github.com/ceeyang/win-open
scoop install open
```

**一键脚本**（下载最新 Release 的 `open.exe` 到 `%USERPROFILE%\.local\bin`，不在 PATH 里就加进用户 PATH）

```powershell
irm https://raw.githubusercontent.com/ceeyang/win-open/main/install.ps1 | iex
```

**手动**：从 [Releases](https://github.com/ceeyang/win-open/releases) 下载 zip，把 `open.exe` 放到任何在 PATH 里的目录。

**从源码编译**（只需要 Windows 自带的 .NET Framework 4.x，不用装 SDK）

```powershell
git clone https://github.com/ceeyang/win-open
cd win-open
.\build.ps1            # 产出 dist\open.exe
```

## 用法

```
open [-a 程序] [-R] [-e|-t] [-W] [-n] [--] [路径|网址 ...]
```

| 参数 | 作用 | macOS 对应 |
|---|---|---|
| （无） | 打开当前目录 | `open .` |
| `路径` | 文件用默认程序打开，目录用资源管理器打开，可一次给多个 | 同 |
| `网址` | `http(s)://`、任何 `scheme://`、`mailto:`、`tel:` 交给默认处理程序 | 同 |
| `-a 程序` | 用指定程序打开。程序名（`notepad`、`codium`）、`.cmd`/`.bat` 或完整路径都行，会沿 PATH + PATHEXT 查找 | `-a` |
| `-R` | 在资源管理器里定位并选中该文件或目录 | `-R` |
| `-e`、`-t` | 用编辑器打开。依次尝试 `$VISUAL`、`$EDITOR`、`codium`、`code`、`notepad` | `-e` 用 TextEdit，`-t` 用默认文本编辑器 |
| `-W` | 等被打开的程序退出再返回 | `-W` |
| `-n` | 只打印会执行的动作，不执行 | 无（mac 的 `-n` 是新开实例） |
| `--` | 后面的全是路径，即使以 `-` 开头 | 同 |
| `-h`、`-v` | 帮助、版本 | |

返回码：0 成功；1 有目标打不开或不存在；2 参数错误。多个目标时逐个处理，一个失败不影响其余。

## 在 Git Bash 里

不需要任何额外配置。MSYS 在调用原生 exe 时会自动把 `/d/works/x.pdf` 这类路径转成 `D:\works\x.pdf`，网址因为含 `://` 不会被转。检测到 `MSYSTEM` 环境变量时输出切到 UTF-8，中文提示不乱码。

## 工作原理

`open.exe` 是一个 C# 控制台程序（`src/Program.cs`，约 150 行），用 `.NET Framework 4.x` 自带的 `csc.exe` 编译，所以任何 Windows 10/11 都能直接运行和编译。核心就是 `ShellExecute`：文件、目录、网址都交给系统的默认关联；`-R` 调 `explorer.exe /select,`；`-a` 和 `-e` 先沿 PATH 把程序名解析成具体文件再启动，因为 `ShellExecute` 不会给 `codium` 自动补 `.cmd`。

## 卸载

- winget：`winget uninstall ceeyang.win-open`
- Scoop：`scoop uninstall open`
- 一键脚本或手动：删掉 `%USERPROFILE%\.local\bin\open.exe`（PATH 项可留着）

## 常见问题

**和 mac 的 `open` 有什么不一样？** 没有 `-f`（从 stdin 读）、`-b`（bundle id）、`-g`（后台）、`-n`（新实例）、`--args`。`-n` 在这里是 dry run。

**`open xxx.cmd` 会执行脚本而不是打开它编辑。** 和 mac 上 `open xxx.sh` 一样，默认关联就是执行；想编辑用 `open -e xxx.cmd`。

**PowerShell 执行策略会不会挡住？** 不会，是 exe 不是脚本。

**为什么不用 `start`？** `start` 的第一个带引号参数会被当成窗口标题，`start "D:\a b.pdf"` 只会开一个空 cmd 窗口，得写成 `start "" "D:\a b.pdf"`；而且 Git Bash 里没有它。

**杀毒软件报毒？** 自己编译一份即可：`.\build.ps1`，源码就 150 行。

## 开发与发布

```powershell
.\build.ps1 -Zip      # dist\open.exe + dist\win-open-<版本>.zip，并打印 sha256
```

版本号只在 `src/Program.cs` 里改一处（`Version` 常量和 `AssemblyVersion`）。发新版的顺序：改版本 → `build.ps1 -Zip` → `gh release create v<版本> dist\win-open-<版本>.zip` → 把 sha256 填进 `bucket/open.json` 和 `winget/` 下的清单 → 向 [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) 提 PR。

## 许可证

MIT，见 [LICENSE](LICENSE)。

---

## English

`open` for Windows, modelled on the macOS command. One dependency-free `open.exe` (under 10 KB, needs only the .NET Framework that ships with Windows) that works the same from PowerShell, cmd and Git Bash.

### Install

```powershell
winget install ceeyang.win-open
# or
scoop bucket add win-open https://github.com/ceeyang/win-open
scoop install open
# or: download open.exe from Releases and put it on your PATH
# or: one-liner that installs to %USERPROFILE%\.local\bin and adds it to PATH
irm https://raw.githubusercontent.com/ceeyang/win-open/main/install.ps1 | iex
```

Build from source with nothing but the C# compiler that ships with Windows: `.\build.ps1`.

### Usage

```
open [-a app] [-R] [-e|-t] [-W] [-n] [--] [path|url ...]
```

| Flag | Meaning |
|---|---|
| (none) | open the current folder in Explorer |
| `path` / `url` | default handler: files open in their associated app, folders in Explorer, URLs in the default browser; several targets allowed |
| `-a app` | open with a specific application (name, `.cmd`/`.bat`, or full path; resolved along PATH + PATHEXT) |
| `-R` | reveal in Explorer with the item selected |
| `-e`, `-t` | open in an editor: `$VISUAL`, `$EDITOR`, `codium`, `code`, `notepad`, first one found |
| `-W` | wait for the launched application to exit |
| `-n` | dry run: print what would be launched |
| `--` | treat everything after it as paths |
| `-h`, `-v` | help, version |

Exit codes: 0 success, 1 a target was missing or could not be opened, 2 bad arguments.

### Git Bash

No configuration needed. MSYS converts `/d/works/x.pdf` to `D:\works\x.pdf` when calling a native exe; URLs are left alone. Output switches to UTF-8 when `MSYSTEM` is set.

### Why not the alternatives

`start` has awkward quoting and does not exist in Git Bash. [neosmart/open](https://github.com/neosmart/open) only does the double-click equivalent. [winopen](https://github.com/yannlugrin/winopen) and [wsl-open](https://github.com/4U6U57/wsl-open) are WSL-only. [open-cli](https://github.com/sindresorhus/open-cli) needs Node, is called `open-cli`, and has no reveal or editor flags.

### Differences from macOS

Not implemented: `-f` (stdin), `-b` (bundle id), `-g` (background), `-n` as "new instance" (here `-n` is dry run), `--args`.

### License

MIT.
