$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$app=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$compat=$app.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksCompatibility',$true)
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$parse=$compat.GetMethod('YearFromRevision',$flags)
$resolve=$compat.GetMethod('ResolveMother',$flags)
$future=$compat.GetMethod('IsFutureTemplatePath',$flags)
foreach($case in @(@('30.5.0',2022),@('31.5.0',2023),@('32.5.0',2024),@('33.0.0',2025))){
  $actual=[int]$parse.Invoke($null,@([string]$case[0]))
  if($actual -ne [int]$case[1]){throw ('Revision conversion failed: '+$case[0])}
}
$library=Get-ChildItem -LiteralPath $root -Directory | Where-Object { (Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.SLDPRT').Count -eq 15 } | Select-Object -First 1
if(-not $library){throw 'Master library not found.'}
$file=Get-ChildItem -LiteralPath $library.FullName -File -Filter '*.SLDPRT' | Select-Object -First 1
$current=[string]$resolve.Invoke($null,@($library.FullName,2024,$file.Name))
$newer=[string]$resolve.Invoke($null,@($library.FullName,2025,$file.Name))
if($current -ne $file.FullName -or $newer -ne $file.FullName){throw '2024+ model routing failed.'}
foreach($year in @(2022,2023)){
  try { $resolve.Invoke($null,@($library.FullName,$year,$file.Name)) | Out-Null; throw 'Old release incorrectly used a 2024 master.' }
  catch {
    $cause=$_.Exception
    while($cause.InnerException){$cause=$cause.InnerException}
    if($cause -isnot [IO.FileNotFoundException]){throw}
  }
}
if(-not [bool]$future.Invoke($null,@('C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\Part.prtdot',2023))){throw 'Future template was not rejected.'}
if([bool]$future.Invoke($null,@('C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2023\templates\Part.prtdot',2023))){throw 'Same-year template was rejected.'}
$references=@($app.GetReferencedAssemblies() | ForEach-Object { $_.Name })
if($references -contains 'SolidWorks.Interop.sldworks'){throw 'Interop assembly is not embedded.'}
$setup=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SetupV352.exe'))
$resources=@($setup.GetManifestResourceNames())
if(@($resources | Where-Object { $_ -match '^Payload\.Library\d+$' }).Count -ne 15){throw 'Installer must embed 15 masters.'}
if($resources -notcontains 'Payload.Polyhedron'){throw 'Installer is missing the polyhedron template.'}
$catalog=$app.GetType('SolidWorksPrimitiveBuilderV2.MasterPartCatalog',$true)
$names=@($catalog.GetProperty('ShapeNames',[Reflection.BindingFlags]'Static,NonPublic').GetValue($null,$null))
foreach($direct in @('垫片（公式直建）','轴套（公式直建）')){if($names -notcontains $direct){throw ('Direct mode missing: '+$direct)}}
Write-Output 'PASS_COMPATIBILITY=2022,2023,2024,2025;MASTERS=15;POLYHEDRON=embedded;DIRECT=washer,bushing;INTEROP=embedded'
