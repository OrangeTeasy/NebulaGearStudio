$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$read=$builder.GetMethod('ReadMasterEquations',[Reflection.BindingFlags]'Static,NonPublic')
$create=$builder.GetMethod('CreateFromMaster',[Reflection.BindingFlags]'Static,NonPublic')
$type=$assembly.GetType('SolidWorksPrimitiveBuilderV2.MasterPartOptions',$true)
$library=Get-ChildItem -LiteralPath $root -Directory | Where-Object { (Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.SLDPRT').Count -eq 15 } | Select-Object -First 1
$file=Get-ChildItem -LiteralPath $library.FullName -File -Filter '*.SLDPRT' | Where-Object { $_.Length -gt 900000 } | Select-Object -First 1
$equations=$read.Invoke($null,@($file.Name))
$options=[Activator]::CreateInstance($type,$true)
$flags=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$type.GetField('FileName',$flags).SetValue($options,$file.Name)
$type.GetField('DisplayName',$flags).SetValue($options,'LibraryRegression')
$type.GetField('Equations',$flags).SetValue($options,$equations)
$out=Join-Path $root 'library-tests'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$result=$create.Invoke($null,@($options,[string]$out,[bool]$true,[bool]$true))
$path=$result.GetType().GetField('PartPath',$flags).GetValue($result)
if(!(Test-Path -LiteralPath $path)){throw 'Generated library part is missing.'}
if(!(Test-Path -LiteralPath ([IO.Path]::ChangeExtension($path,'.STEP')))){throw 'STEP export is missing.'}
if(!(Test-Path -LiteralPath ([IO.Path]::ChangeExtension($path,'.3MF')))){throw '3MF export is missing.'}
Write-Output ('PASS_GENERATE='+$file.Name+';PART='+$path)
