$ErrorActionPreference='Stop'
$installDir='D:\SolidWorks多形体建模器'
$desktop=[System.Environment]::GetFolderPath('DesktopDirectory')
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV21.exe') -Destination (Join-Path $installDir 'SolidWorks多形体建模器V2.1.exe')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'avatar-v21.png') -Destination (Join-Path $installDir 'avatar-v21.png')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'avatar-v21.ico') -Destination (Join-Path $installDir 'avatar-v21.ico')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination (Join-Path $installDir '使用说明.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SetupV21.exe') -Destination (Join-Path $desktop 'SolidWorks建模器V2.1_齿轮版安装程序.exe')
Compress-Archive -LiteralPath (Join-Path $PSScriptRoot 'SetupV21.exe'),(Join-Path $PSScriptRoot 'README.txt') -DestinationPath (Join-Path $desktop 'SolidWorks建模器V2.1_齿轮版安装包.zip') -Force
$shell=New-Object -ComObject WScript.Shell
$link=$shell.CreateShortcut((Join-Path $desktop 'SolidWorks多形体建模器（D盘安装版）.lnk'))
$link.TargetPath=Join-Path $installDir 'SolidWorks多形体建模器V2.1.exe'
$link.WorkingDirectory=$installDir
$link.IconLocation=(Join-Path $installDir 'avatar-v21.ico')+',0'
$link.Description='SolidWorks V2.1 · 渐开线齿轮与基础形体'
$link.Save()
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public static class IconRefresh { [DllImport("shell32.dll")] public static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b); }'
[IconRefresh]::SHChangeNotify(0x8000000,0,[IntPtr]::Zero,[IntPtr]::Zero)
[PSCustomObject]@{Target=$link.TargetPath;Icon=$link.IconLocation;Version=(Get-Item -LiteralPath $link.TargetPath).VersionInfo.FileVersion}
Get-FileHash -LiteralPath $link.TargetPath,(Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV21.exe')
