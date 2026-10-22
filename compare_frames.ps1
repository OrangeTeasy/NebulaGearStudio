Add-Type -AssemblyName System.Drawing
$a=New-Object Drawing.Bitmap((Join-Path $PSScriptRoot 'v31-frame1.png'))
$b=New-Object Drawing.Bitmap((Join-Path $PSScriptRoot 'v31-frame2.png'))
$changed=0L
$strong=0L
$sampled=0L
for($y=0;$y -lt $a.Height;$y+=2){
  for($x=0;$x -lt $a.Width;$x+=2){
    $p=$a.GetPixel($x,$y)
    $q=$b.GetPixel($x,$y)
    $sampled++
    if($p.ToArgb() -ne $q.ToArgb()){$changed++}
    if(([Math]::Abs($p.R-$q.R)+[Math]::Abs($p.G-$q.G)+[Math]::Abs($p.B-$q.B)) -gt 45){$strong++}
  }
}
$a.Dispose();$b.Dispose()
[PSCustomObject]@{Sampled=$sampled;Changed=$changed;ChangedPercent=[Math]::Round(100*$changed/$sampled,3);Strong=$strong;StrongPercent=[Math]::Round(100*$strong/$sampled,3)}
