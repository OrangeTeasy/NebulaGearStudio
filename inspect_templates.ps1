$ErrorActionPreference = 'Stop'
$paths = @(
  'C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件\③斜齿轮v2.0(右旋).PRTDOT',
  'C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件\⑨蜗轮v2.1(右旋).PRTDOT',
  'C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件\⑩蜗杆v2.0.PRTDOT',
  'C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2024\templates\常用零件\⑮正n面体.PRTDOT'
)
$sw = New-Object -ComObject SldWorks.Application
foreach ($path in $paths) {
  $errors = 0; $warnings = 0
  $doc = $sw.OpenDoc6($path, 1, 1, '', [ref]$errors, [ref]$warnings)
  Write-Output ('TEMPLATE=' + [IO.Path]::GetFileName($path))
  Write-Output ('OPEN_ERRORS=' + $errors + ';WARNINGS=' + $warnings)
  if ($null -ne $doc) {
    $feature = $doc.FirstFeature()
    while ($null -ne $feature) {
      Write-Output (($feature.Name) + ' | ' + ($feature.GetTypeName2()))
      $feature = $feature.GetNextFeature()
    }
    $sw.CloseDoc($doc.GetTitle())
  }
}
