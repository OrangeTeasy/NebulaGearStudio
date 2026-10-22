$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$root=$PSScriptRoot
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$kindType=$assembly.GetType('SolidWorksPrimitiveBuilderV2.ShapeKind',$true)
$dialogType=$assembly.GetType('SolidWorksPrimitiveBuilderV2.MasterPartDialog',$true)
$kind=[Enum]::Parse($kindType,'SpurGear')
$flags=[Reflection.BindingFlags]'Instance,Public,NonPublic'
$dialog=[Activator]::CreateInstance($dialogType,$flags,$null,@($kind),$null)
try {
  $show=$dialogType.GetMethod('ShowParameters',[Reflection.BindingFlags]'Instance,NonPublic')
  $show.Invoke($dialog,@(('"m" = 2' + [Environment]::NewLine + '"z" = 50'))) | Out-Null
  $area=$dialogType.GetField('parameterArea',$flags).GetValue($dialog)
  $labels=@($area.Controls | Where-Object { $_ -is [System.Windows.Forms.Label] })
  $fields=@($area.Controls | Where-Object { $_ -is [System.Windows.Forms.TextBox] })
  if($labels.Count -ne 2 -or $fields.Count -ne 2){throw 'Expected two parameter-name and input pairs.'}
  $fields[1].Text='51'
  $dialogType.GetMethod('Finish',[Reflection.BindingFlags]'Instance,NonPublic').Invoke($dialog,@()) | Out-Null
  $options=$dialogType.GetField('Options',$flags).GetValue($dialog)
  $equations=$options.GetType().GetField('Equations',$flags).GetValue($options)
  if($equations -notmatch '"z"\s*=\s*51'){throw 'Edited input was not collected.'}
  Write-Output ('PASS_UI_INPUTS=2;EQUATIONS='+($equations -replace "`r?`n",';'))
} finally { $dialog.Dispose() }
