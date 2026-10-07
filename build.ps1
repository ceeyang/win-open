# 编译 open.exe 并打发布包。只依赖 Windows 自带的 .NET Framework 4.x C# 编译器，不用装 SDK。
#   .\build.ps1          -> dist\open.exe
#   .\build.ps1 -Zip     -> 另外打出 dist\win-open-<版本>.zip（exe + README + LICENSE），给 GitHub Release / winget / Scoop 用
param([switch]$Zip)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '找不到 .NET Framework 4.x 的 csc.exe' }

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$exe = Join-Path $dist 'open.exe'
# /codepage:65001：源码是无 BOM 的 UTF-8，不指定的话 csc 按系统代码页（GBK）读，中文字符串会变乱码
& $csc /nologo /optimize+ /target:exe /platform:anycpu /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'src\Program.cs')
if ($LASTEXITCODE -ne 0) { throw "编译失败 ($LASTEXITCODE)" }
$ver = (Select-String -Path (Join-Path $root 'src\Program.cs') -Pattern 'const string Version = "([^"]+)"').Matches[0].Groups[1].Value
"built $exe (v$ver, $((Get-Item $exe).Length) bytes)"

if ($Zip) {
    # 变量名不能叫 $zip：PowerShell 变量不分大小写，会和 -Zip 开关撞在一起
    $zipPath = Join-Path $dist "win-open-$ver.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path $exe, (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE') -DestinationPath $zipPath
    $sha = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()
    "zip   $zipPath"
    "sha256 $sha"
}
