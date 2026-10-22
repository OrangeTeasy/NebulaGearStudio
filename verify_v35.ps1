$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$source=Join-Path $root '母版库'
$installed='D:\SolidWorks多形体建模器'
$deployed=Join-Path $installed '母版库'
$sourceFiles=@(Get-ChildItem -LiteralPath $source -File -Filter '*.SLDPRT')
$deployedFiles=@(Get-ChildItem -LiteralPath $deployed -File -Filter '*.SLDPRT')
if($sourceFiles.Count -ne 15 -or $deployedFiles.Count -ne 15){throw 'Expected exactly 15 source and installed masters.'}
$hashCount=0
foreach($file in $sourceFiles){
  $target=Join-Path $deployed $file.Name
  if(!(Test-Path -LiteralPath $target)){throw ('Missing installed master: '+$file.Name)}
  if($file.Length -ne (Get-Item -LiteralPath $target).Length){throw ('Master length mismatch: '+$file.Name)}
  try {
    $sourceHash=(Get-FileHash -LiteralPath $file.FullName -ErrorAction Stop).Hash
    $targetHash=(Get-FileHash -LiteralPath $target -ErrorAction Stop).Hash
    if($sourceHash -ne $targetHash){throw ('Master checksum mismatch: '+$file.Name)}
    $hashCount++
  } catch [System.IO.IOException] { }
}
$app=Join-Path $installed 'NebulaGearStudioV3.exe'
if((Get-Item -LiteralPath $app).VersionInfo.FileVersion -ne '3.5.0.0'){throw 'Installed application version is not 3.5.0.0.'}
$setup=Join-Path $root 'SetupV35.exe'
if((Get-Item -LiteralPath $setup).VersionInfo.FileVersion -ne '3.5.0.0'){throw 'Installer version is not 3.5.0.0.'}
$resources=[Reflection.Assembly]::LoadFrom($setup).GetManifestResourceNames()
$libraryResources=@($resources | Where-Object { $_ -match '^Payload\.Library\d+$' })
if($libraryResources.Count -ne 15){throw 'Installer does not contain 15 masters.'}
$desktop=[Environment]::GetFolderPath('DesktopDirectory')
$desktopSetup=Join-Path $desktop 'NebulaGearStudioV3.5_圆角界面版安装程序.exe'
$desktopZip=Join-Path $desktop 'NebulaGearStudioV3.5_圆角界面版安装包.zip'
if(!(Test-Path -LiteralPath $desktopSetup) -or !(Test-Path -LiteralPath $desktopZip)){throw 'Desktop installer or ZIP missing.'}
if((Get-FileHash -LiteralPath $setup).Hash -ne (Get-FileHash -LiteralPath $desktopSetup).Hash){throw 'Desktop installer checksum mismatch.'}
Write-Output ('PASS_V35;SOURCE_MASTERS='+$sourceFiles.Count+';INSTALLED_MASTERS='+$deployedFiles.Count+';EMBEDDED_MASTERS='+$libraryResources.Count+';HASHED_MASTERS='+$hashCount+';VERSION=3.5.0.0')
Write-Output ('SETUP='+$desktopSetup)
Write-Output ('ZIP='+$desktopZip)
