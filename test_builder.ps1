param(
    [ValidateSet('Box','Sphere','Cylinder','Cone','Frustum')]
    [string]$Shape = 'Sphere'
)

$assemblyPath = Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV2.exe'
$outputPath = [string](Join-Path $PSScriptRoot 'test-output')
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$builderType = $assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder')
$shapeType = $assembly.GetType('SolidWorksPrimitiveBuilderV2.ShapeKind')
$method = $builderType.GetMethod('Create', [Reflection.BindingFlags]'Static,NonPublic')
$shapeValue = [Enum]::Parse($shapeType, $Shape)

switch ($Shape) {
    'Box'      { $dimensions = @([double]0.04, [double]0.03, [double]0.02) }
    'Sphere'   { $dimensions = @([double]0.05, [double]0.0, [double]0.0) }
    'Cylinder' { $dimensions = @([double]0.04, [double]0.06, [double]0.0) }
    'Cone'     { $dimensions = @([double]0.04, [double]0.06, [double]0.0) }
    'Frustum'  { $dimensions = @([double]0.05, [double]0.03, [double]0.06) }
}

$arguments = [object[]]@($shapeValue, [double]$dimensions[0], [double]$dimensions[1], [double]$dimensions[2], [string]$outputPath, [bool]$true, [bool]$true)
$result = $method.Invoke($null, $arguments)
$result.GetType().GetFields([Reflection.BindingFlags]'Instance,NonPublic') |
    ForEach-Object { $_.Name + '=' + $_.GetValue($result) }
