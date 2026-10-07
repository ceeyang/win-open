# win-open 一键安装：把最新 Release 的 open.exe 装到 %USERPROFILE%\.local\bin（或 -Dest），不在 PATH 里就加入用户 PATH。
#   irm https://raw.githubusercontent.com/ceeyang/win-open/main/install.ps1 | iex
#   .\install.ps1 -Dest D:\tools\bin
# 在仓库目录里运行且 dist\open.exe 存在时直接用本地编译产物，不联网。
param([string]$Dest = (Join-Path $env:USERPROFILE '.local\bin'))
$ErrorActionPreference = 'Stop'

$local = if ($PSScriptRoot) { Join-Path $PSScriptRoot 'dist\open.exe' } else { $null }
$tmp = $null
if ($local -and (Test-Path $local)) {
    $src = $local
} else {
    $api = 'https://api.github.com/repos/ceeyang/win-open/releases/latest'
    $rel = Invoke-RestMethod -Uri $api -Headers @{ 'User-Agent' = 'win-open-installer' }
    $asset = $rel.assets | Where-Object { $_.name -like 'win-open-*.zip' } | Select-Object -First 1
    if (-not $asset) { throw "Release $($rel.tag_name) 里没有 win-open-*.zip" }
    $tmp = Join-Path $env:TEMP ('win-open-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    $zip = Join-Path $tmp $asset.name
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing
    Expand-Archive -Path $zip -DestinationPath $tmp -Force
    $src = Join-Path $tmp 'open.exe'
    "下载 $($rel.tag_name)：$($asset.browser_download_url)"
}

New-Item -ItemType Directory -Force -Path $Dest | Out-Null
Copy-Item $src (Join-Path $Dest 'open.exe') -Force
"已安装：$(Join-Path $Dest 'open.exe')"

$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
$parts = @($userPath -split ';' | Where-Object { $_ })
if ($parts -notcontains $Dest.TrimEnd('\')) {
    [Environment]::SetEnvironmentVariable('Path', (($parts + $Dest) -join ';'), 'User')
    $env:Path = "$env:Path;$Dest"
    "已加入用户 PATH：$Dest（新开的终端生效）"
}
if ($tmp) { Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue }
& (Join-Path $Dest 'open.exe') -v
