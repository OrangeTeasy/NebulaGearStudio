$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
$a=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV3.exe'))
foreach($name in @('CosmicMainForm','HelicalGearDialog','WormSetDialog')) {
 $form=[Activator]::CreateInstance($a.GetType('SolidWorksPrimitiveBuilderV2.'+$name),$true)
 $form.Show()
 [Windows.Forms.Application]::DoEvents()
 $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
 $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)))
 $bitmap.Save((Join-Path $PSScriptRoot ($name+'-v3.png')),[Drawing.Imaging.ImageFormat]::Png)
 $bitmap.Dispose()
 $form.Close()
 $form.Dispose()
}
