$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()

$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'SolidWorksPrimitiveBuilderV3.exe'))
$form = [Activator]::CreateInstance($assembly.GetType('SolidWorksPrimitiveBuilderV2.CosmicMainForm'), $true)
try {
    $form.Show()
    [Windows.Forms.Application]::DoEvents()

    $form.Activate()
    Start-Sleep -Milliseconds 250
    [Windows.Forms.Application]::DoEvents()
    $normalBitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
    $normalGraphics = [Drawing.Graphics]::FromImage($normalBitmap)
    $normalGraphics.CopyFromScreen($form.Left, $form.Top, 0, 0, $form.Size)
    $normalGraphics.Dispose()
    $normalPath = Join-Path $PSScriptRoot 'v39-bilingual-normal-check.png'
    $normalBitmap.Save($normalPath, [Drawing.Imaging.ImageFormat]::Png)
    $normalBitmap.Dispose()

    $form.WindowState = [Windows.Forms.FormWindowState]::Maximized
    for ($i = 0; $i -lt 18; $i++) {
        [Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 20
    }
    $form.Activate()
    $fullBitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
    $fullGraphics = [Drawing.Graphics]::FromImage($fullBitmap)
    $fullGraphics.CopyFromScreen($form.Left, $form.Top, 0, 0, $form.Size)
    $fullGraphics.Dispose()
    $fullPath = Join-Path $PSScriptRoot 'v39-bilingual-key-full-check.png'
    $fullBitmap.Save($fullPath, [Drawing.Imaging.ImageFormat]::Png)
    $fullBitmap.Dispose()

    function Find-ControlByTypeName([Windows.Forms.Control]$parent, [string]$typeName) {
        foreach ($child in $parent.Controls) {
            if ($child.GetType().Name -eq $typeName) { return $child }
            $nested = Find-ControlByTypeName $child $typeName
            if ($null -ne $nested) { return $nested }
        }
        return $null
    }
    $glow = Find-ControlByTypeName $form 'HoverGlowLabel'
    $hoverPath = Join-Path $PSScriptRoot 'v39-gallery-glow-area-check.png'
    if ($null -ne $glow -and $glow.Visible) {
        # 放在图片底边下方的新增留白交互区，而不是文字上。
        [Windows.Forms.Cursor]::Position = $glow.PointToScreen((New-Object Drawing.Point([int]($glow.Width / 2), 34)))
        for ($i = 0; $i -lt 5; $i++) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 30 }
        $hoverBitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
        $hoverGraphics = [Drawing.Graphics]::FromImage($hoverBitmap)
        $hoverGraphics.CopyFromScreen($form.Left, $form.Top, 0, 0, $form.Size)
        $hoverGraphics.Dispose()
        $hoverBitmap.Save($hoverPath, [Drawing.Imaging.ImageFormat]::Png)
        $hoverBitmap.Dispose()
    }

    $switchButton = $null
    function Find-ButtonByText([Windows.Forms.Control]$parent, [string]$text) {
        foreach ($child in $parent.Controls) {
            if ($child -is [Windows.Forms.Button] -and $child.Text -eq $text) { return $child }
            $nested = Find-ButtonByText $child $text
            if ($null -ne $nested) { return $nested }
        }
        return $null
    }
    $switchButton = Find-ButtonByText $form '转入 CREO'
    $creoPath = Join-Path $PSScriptRoot 'v39-creo-mode-ui-check.png'
    if ($null -ne $switchButton) {
        $switchButton.PerformClick()
        for ($i = 0; $i -lt 8; $i++) { [Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 30 }
        $creoBitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
        $creoGraphics = [Drawing.Graphics]::FromImage($creoBitmap)
        $creoGraphics.CopyFromScreen($form.Left, $form.Top, 0, 0, $form.Size)
        $creoGraphics.Dispose()
        $creoBitmap.Save($creoPath, [Drawing.Imaging.ImageFormat]::Png)
        $creoBitmap.Dispose()
    }

    $labels = @($form.Controls | ForEach-Object { $_.Controls } | Where-Object { $_ -is [Windows.Forms.Label] } | Select-Object -ExpandProperty Text)
    [PSCustomObject]@{
        NormalCapture = $normalPath
        FullCapture = $fullPath
        HoverCapture = $hoverPath
        CreoCapture = $creoPath
        HasLengthLabel = $labels -contains '长度  Length'
        HasWidthLabel = $labels -contains '宽度  Width'
        HasHeightLabel = $labels -contains '高度  Height'
        HasKeyCaption = $labels -contains '密钥接入  Key'
    }
}
finally {
    $form.Close()
    $form.Dispose()
}
