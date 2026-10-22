$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$method=$builder.GetMethod('InspectTemplateEquations',[Reflection.BindingFlags]'Static,NonPublic')
$paths=@(
 'C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件\⑨蜗轮v2.1(右旋).PRTDOT',
 'C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件\⑩蜗杆v2.0.PRTDOT'
)
foreach($path in $paths){ Write-Output ('TEMPLATE='+[IO.Path]::GetFileName($path)); Write-Output ($method.Invoke($null,@($path))) }
