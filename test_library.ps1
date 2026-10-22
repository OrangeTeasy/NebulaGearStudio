$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$read=$builder.GetMethod('ReadMasterEquations',[Reflection.BindingFlags]'Static,NonPublic')
$library=Get-ChildItem -LiteralPath $root -Directory | Where-Object { (Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.SLDPRT').Count -eq 15 } | Select-Object -First 1
if($null -eq $library){throw 'Master library directory was not found.'}
$files=Get-ChildItem -LiteralPath $library.FullName -File -Filter '*.SLDPRT'
foreach($file in $files){
  try { $text=$read.Invoke($null,@($file.Name)); $count=($text -split "`r?`n").Count; Write-Output ('PASS='+$file.Name+';EQUATIONS='+$count) }
  catch { Write-Output ('FAIL='+$file.Name+';'+$_.Exception.InnerException.Message) }
}
