$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$builder = $assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder', $true)
$shapeType = $assembly.GetType('SolidWorksPrimitiveBuilderV2.ShapeKind', $true)
$create = $builder.GetMethod('Create', [Reflection.BindingFlags]'Static,NonPublic')
$out = Join-Path $root 'basic-tests'
New-Item -ItemType Directory -Force -Path $out | Out-Null

foreach ($entry in @(
    @('Sphere',   0.040, 0.000, 0.000),
    @('Cylinder', 0.030, 0.060, 0.000),
    @('Cone',     0.030, 0.060, 0.000),
    @('Frustum',  0.040, 0.020, 0.060),
    @('Washer',   0.050, 0.020, 0.004),
    @('Bushing',  0.040, 0.020, 0.030)
)) {
    $kind = [Enum]::Parse($shapeType, $entry[0])
    try {
        $null = $create.Invoke($null, @($kind, [double]$entry[1], [double]$entry[2], [double]$entry[3], [string]$out, [bool]$false, [bool]$false))
        Write-Output ('PASS=' + $entry[0])
    }
    catch {
        $inner = $_.Exception.InnerException
        Write-Output ('FAIL=' + $entry[0] + ';' + $(if ($null -ne $inner) { $inner.Message } else { $_.Exception.Message }))
    }
}
