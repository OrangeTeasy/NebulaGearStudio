$ErrorActionPreference='Stop'
$desktop=[Environment]::GetFolderPath('DesktopDirectory')
$setup=Join-Path $PSScriptRoot 'SetupV352.exe'
$target=Join-Path $desktop 'NebulaGearStudioV3.5.2_公式直建安装程序.exe'
$zip=Join-Path $desktop 'NebulaGearStudioV3.5.2_公式直建安装包.zip'
if(!(Test-Path -LiteralPath $setup)){throw '请先运行 build.ps1 编译安装程序。'}
Copy-Item -LiteralPath $setup -Destination $target -Force
Compress-Archive -LiteralPath $setup,(Join-Path $PSScriptRoot 'README.txt'),(Join-Path $PSScriptRoot 'girl-greenhouse.png'),(Join-Path $PSScriptRoot 'girl-greenhouse-rounded.png') -DestinationPath $zip -Force
if((Get-FileHash -LiteralPath $setup).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw '桌面安装程序校验失败。'}
[PSCustomObject]@{Installer=$target;Zip=$zip;Bytes=(Get-Item -LiteralPath $target).Length;Version=(Get-Item -LiteralPath $target).VersionInfo.FileVersion}
