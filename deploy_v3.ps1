$ErrorActionPreference='Stop'
$installDir='D:\SolidWorks多形体建模器'
$desktop=[System.Environment]::GetFolderPath('DesktopDirectory')
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV3.exe') -Destination (Join-Path $installDir 'NebulaGearStudioV3.exe') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'girl-greenhouse.png') -Destination (Join-Path $installDir 'girl-greenhouse.png') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'girl-greenhouse-rounded.png') -Destination (Join-Path $installDir 'girl-greenhouse-rounded.png') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'girl-greenhouse-rounded.ico') -Destination (Join-Path $installDir 'girl-greenhouse-rounded.ico') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '圆柱直齿轮方程母版.SLDPRT') -Destination (Join-Path $installDir '圆柱直齿轮方程母版.SLDPRT') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '圆柱斜齿轮左旋方程母版.SLDPRT') -Destination (Join-Path $installDir '圆柱斜齿轮左旋方程母版.SLDPRT') -Force
$libraryDir=Join-Path $installDir '母版库'
New-Item -ItemType Directory -Path $libraryDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $libraryDir 'SW2022') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $libraryDir 'SW2023') -Force | Out-Null
$libraryNames=@('圆柱直齿轮.SLDPRT','圆柱斜齿轮右旋.SLDPRT','圆柱斜齿轮左旋.SLDPRT','直齿圈.SLDPRT','斜齿圈右旋.SLDPRT','斜齿圈左旋.SLDPRT','锥齿轮.SLDPRT','涡杆.SLDPRT','涡轮右旋.SLDPRT','涡轮左旋.SLDPRT','压缩弹簧.SLDPRT','拉伸弹簧.SLDPRT','扭转弹簧.SLDPRT','截锥弹簧无紧固圈.SLDPRT','截锥弹簧有紧固圈.SLDPRT')
foreach($name in $libraryNames){ Copy-Item -LiteralPath (Join-Path (Join-Path $PSScriptRoot '母版库') $name) -Destination (Join-Path $libraryDir $name) -Force }
Copy-Item -LiteralPath (Join-Path (Join-Path $PSScriptRoot '母版库') '正n面体.PRTDOT') -Destination (Join-Path $libraryDir '正n面体.PRTDOT') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination (Join-Path $installDir '使用说明.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SetupV351.exe') -Destination (Join-Path $desktop 'NebulaGearStudioV3.5.1_跨版本适配安装程序.exe') -Force
Compress-Archive -LiteralPath (Join-Path $PSScriptRoot 'SetupV351.exe'),(Join-Path $PSScriptRoot 'README.txt'),(Join-Path $PSScriptRoot 'girl-greenhouse.png'),(Join-Path $PSScriptRoot 'girl-greenhouse-rounded.png') -DestinationPath (Join-Path $desktop 'NebulaGearStudioV3.5.1_跨版本适配安装包.zip') -Force
$shell=New-Object -ComObject WScript.Shell
$shortcutPath=Join-Path $desktop 'SolidWorks多形体建模器（D盘安装版）.lnk'
$link=$shell.CreateShortcut($shortcutPath)
$link.TargetPath=Join-Path $installDir 'NebulaGearStudioV3.exe'
$link.WorkingDirectory=$installDir
$link.IconLocation=(Join-Path $installDir 'girl-greenhouse-rounded.ico')+',0'
$link.Description='Nebula Gear Studio V3.5.1 · 跨版本适配'
$link.Save()
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public static class IconRefreshV3 { [DllImport("shell32.dll")] public static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b); }'
[IconRefreshV3]::SHChangeNotify(0x8000000,0,[IntPtr]::Zero,[IntPtr]::Zero)
[PSCustomObject]@{Target=$link.TargetPath;Icon=$link.IconLocation;Version=(Get-Item -LiteralPath $link.TargetPath).VersionInfo.FileVersion;Installer=(Join-Path $desktop 'NebulaGearStudioV3.5.1_跨版本适配安装程序.exe')}
Get-FileHash -LiteralPath (Join-Path $installDir 'girl-greenhouse.png'),(Join-Path $PSScriptRoot 'girl-greenhouse.png')
