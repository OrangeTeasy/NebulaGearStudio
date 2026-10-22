$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$method=$builder.GetMethod('CreateRegularPolyhedron',[Reflection.BindingFlags]'Static,NonPublic')
$optionsType=$assembly.GetType('SolidWorksPrimitiveBuilderV2.PolyhedronOptions',$true)
$out=Join-Path $PSScriptRoot 'polyhedron-tests'
New-Item -ItemType Directory -Force -Path $out | Out-Null
foreach($faces in @(4,6,8)) {
  $options=[Activator]::CreateInstance($optionsType,$true)
  $optionsType.GetField('Faces',[Reflection.BindingFlags]'Instance,Public,NonPublic').SetValue($options,[int]$faces)
  $result=$method.Invoke($null,@($options,[string]$out,[bool]$false,[bool]$false))
  $path=$result.GetType().GetField('PartPath',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($result)
  if(!(Test-Path -LiteralPath $path)){throw 'Polyhedron part was not generated.'}
  Write-Output ('PASS_POLYHEDRON_'+$faces+'='+$path)
}
