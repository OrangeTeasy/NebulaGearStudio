$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$read=$builder.GetMethod('ReadMasterEquations',[Reflection.BindingFlags]'Static,NonPublic')
$catalog=$assembly.GetType('SolidWorksPrimitiveBuilderV2.MasterPartCatalog',$true)
$shape=$assembly.GetType('SolidWorksPrimitiveBuilderV2.ShapeKind',$true)
$names=$catalog.GetProperty('ShapeNames',[Reflection.BindingFlags]'Static,NonPublic').GetValue($null,$null)
Write-Output ('SHAPE_COUNT='+$names.Length)
foreach($item in $names){Write-Output ('MODE='+$item)}
$masterDir=Get-ChildItem -LiteralPath $root -Directory | Where-Object { (Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.SLDPRT').Count -eq 15 } | Select-Object -First 1
if(-not $masterDir){throw 'Master directory not found'}
$master=Get-ChildItem -LiteralPath $masterDir.FullName -File -Filter '*.SLDPRT' | Where-Object { $_.Length -eq 616500 } | Select-Object -First 1
if(-not $master){throw 'Gear master not found'}
$text=$read.Invoke($null,@($master.Name))
($text -split "`r?`n") | Select-Object -First 12
