$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$read=$builder.GetMethod('ReadMasterEquations',[Reflection.BindingFlags]'Static,NonPublic')
$create=$builder.GetMethod('CreateFromMaster',[Reflection.BindingFlags]'Static,NonPublic')
$inspect=$builder.GetMethod('InspectTemplateEquations',[Reflection.BindingFlags]'Static,NonPublic')
$type=$assembly.GetType('SolidWorksPrimitiveBuilderV2.MasterPartOptions',$true)
$library=Get-ChildItem -LiteralPath $root -Directory | Where-Object { (Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.SLDPRT').Count -eq 15 } | Select-Object -First 1
if(-not $library){throw 'Master directory not found.'}
$file=Get-ChildItem -LiteralPath $library.FullName -File -Filter '*.SLDPRT' | Where-Object { $_.Length -eq 616500 } | Select-Object -First 1
if(-not $file){throw 'Gear master not found.'}
$before=[string]$read.Invoke($null,@($file.Name))
$match=[regex]::Match($before,'(?m)^"z"\s*=\s*(\d+)')
if(-not $match.Success){throw 'Gear tooth count not found.'}
$old=[int]$match.Groups[1].Value
$new=$old+1
$after=[regex]::Replace($before,'(?m)^"z"\s*=\s*\d+',('"z" = '+$new),1)
$options=[Activator]::CreateInstance($type,$true)
$flags=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$type.GetField('FileName',$flags).SetValue($options,$file.Name)
$type.GetField('DisplayName',$flags).SetValue($options,'GearParameterRegression')
$type.GetField('Equations',$flags).SetValue($options,$after)
$out=Join-Path $root 'library-tests'
$result=$create.Invoke($null,@($options,[string]$out,[bool]$false,[bool]$false))
$path=[string]$result.GetType().GetField('PartPath',$flags).GetValue($result)
Write-Output $result.GetType().GetField('Message',$flags).GetValue($result)
if(!(Test-Path -LiteralPath $path)){throw 'Generated gear is missing.'}
$outputEquations=[string]$inspect.Invoke($null,@($path))
$sourceEquations=[string]$inspect.Invoke($null,@($file.FullName))
Write-Output (($outputEquations -split "`r?`n") | Where-Object { $_ -match '^"[mz]"' } | Select-Object -First 4)
if($outputEquations -notmatch ('(?m)^"z"\s*=\s*'+$new+'\b')){throw 'Generated gear does not contain the new tooth count.'}
if($sourceEquations -notmatch ('(?m)^"z"\s*=\s*'+$old+'\b')){throw 'Source master was modified.'}
Write-Output ('PASS_PARAMETER_EDIT='+$old+'->'+$new+';PART='+$path)
