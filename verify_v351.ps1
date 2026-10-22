$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$source=Join-Path $root '母版库'
$installed='D:\SolidWorks多形体建模器'
$deployed=Join-Path $installed '母版库'
$sourceFiles=@(Get-ChildItem -LiteralPath $source -File -Filter '*.SLDPRT' | Where-Object { !$_.Name.StartsWith('~$') })
$deployedFiles=@(Get-ChildItem -LiteralPath $deployed -File -Filter '*.SLDPRT' | Where-Object { !$_.Name.StartsWith('~$') })
if($sourceFiles.Count -ne 15 -or $deployedFiles.Count -ne 15){throw 'Expected exactly 15 masters.'}
foreach($file in $sourceFiles){
  $target=Join-Path $deployed $file.Name
  if(!(Test-Path -LiteralPath $target) -or $file.Length -ne (Get-Item -LiteralPath $target).Length){throw ('Master mismatch: '+$file.Name)}
}
$polySource=Join-Path $source '正n面体.PRTDOT'
$polyTarget=Join-Path $deployed '正n面体.PRTDOT'
if(!(Test-Path -LiteralPath $polyTarget) -or (Get-Item -LiteralPath $polySource).Length -ne (Get-Item -LiteralPath $polyTarget).Length){throw 'Polyhedron template mismatch.'}
foreach($year in @(2022,2023)){if(!(Test-Path -LiteralPath (Join-Path $deployed ('SW'+$year)))){throw ('Missing old-release directory: '+$year)}}
$app=Join-Path $installed 'NebulaGearStudioV3.exe'
$setup=Join-Path $root 'SetupV351.exe'
foreach($binary in @($app,$setup)){if((Get-Item -LiteralPath $binary).VersionInfo.FileVersion -ne '3.5.1.0'){throw ('Wrong version: '+$binary)}}
$resources=[Reflection.Assembly]::LoadFrom($setup).GetManifestResourceNames()
if(@($resources | Where-Object { $_ -match '^Payload\.Library\d+$' }).Count -ne 15 -or $resources -notcontains 'Payload.Polyhedron'){throw 'Installer resources incomplete.'}
$desktop=[Environment]::GetFolderPath('DesktopDirectory')
$desktopSetup=Join-Path $desktop 'NebulaGearStudioV3.5.1_跨版本适配安装程序.exe'
$desktopZip=Join-Path $desktop 'NebulaGearStudioV3.5.1_跨版本适配安装包.zip'
if(!(Test-Path -LiteralPath $desktopSetup) -or !(Test-Path -LiteralPath $desktopZip)){throw 'Desktop package missing.'}
if((Get-FileHash -LiteralPath $setup).Hash -ne (Get-FileHash -LiteralPath $desktopSetup).Hash){throw 'Desktop setup checksum mismatch.'}
Write-Output ('PASS_V351;MASTERS='+$deployedFiles.Count+';POLYHEDRON=present;VERSION=3.5.1.0')
Write-Output ('SETUP='+$desktopSetup)
Write-Output ('ZIP='+$desktopZip)
