param([double]$Chamfer=0.15,[double]$Fillet=0.3,[int]$Teeth=20,[double]$Shift=0,[double]$Bore=8)
$ErrorActionPreference='Stop'
$a=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV3.exe'))
$type=$a.GetType('SolidWorksPrimitiveBuilderV2.GearOptions')
$gear=[Activator]::CreateInstance($type,$true)
$type.GetField('Chamfer').SetValue($gear,$Chamfer)
$type.GetField('RootFillet').SetValue($gear,$Fillet)
$type.GetField('Teeth').SetValue($gear,$Teeth)
$type.GetField('Shift').SetValue($gear,$Shift)
$type.GetField('Bore').SetValue($gear,$Bore)
$method=$a.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder').GetMethod('CreateGear',[Reflection.BindingFlags]'Static,NonPublic')
try {
 $result=$method.Invoke($null,[object[]]@($gear,[string](Join-Path $PSScriptRoot 'gear-tests'),$true,$true))
 $result.GetType().GetFields([Reflection.BindingFlags]'Instance,NonPublic') | ForEach-Object { $_.Name+'='+$_.GetValue($result) }
} catch { $_.Exception.ToString(); exit 1 }
