$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
  & $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll '/link:D:\SW\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll' /win32icon:girl-greenhouse-rounded.ico /win32manifest:app.manifest /resource:girl-greenhouse.png,Payload.Hero /resource:girl-lakeside-pier.png,Payload.Lakeside /resource:girl-rainy-window.png,Payload.Rainy /resource:girl-stairwell.png,Payload.Stairwell /out:NebulaGearStudioV5.exe Program.cs Gear.cs AdvancedGears.cs MasterLibrary.cs Compatibility.cs CosmicUI.cs
  if($LASTEXITCODE -ne 0){throw 'Build failed'}
  $libraryNames=@('圆柱直齿轮.SLDPRT','圆柱斜齿轮右旋.SLDPRT','圆柱斜齿轮左旋.SLDPRT','直齿圈.SLDPRT','斜齿圈右旋.SLDPRT','斜齿圈左旋.SLDPRT','锥齿轮.SLDPRT','涡杆.SLDPRT','涡轮右旋.SLDPRT','涡轮左旋.SLDPRT','压缩弹簧.SLDPRT','拉伸弹簧.SLDPRT','扭转弹簧.SLDPRT','截锥弹簧无紧固圈.SLDPRT','截锥弹簧有紧固圈.SLDPRT')
  $stage=Join-Path $PSScriptRoot 'build-stage'
  New-Item -ItemType Directory -Path $stage -Force | Out-Null
  $libraryResources=for($i=0;$i -lt $libraryNames.Count;$i++){
    $staged=Join-Path $stage ('master-'+$i+'.SLDPRT')
    Copy-Item -LiteralPath (Join-Path (Join-Path $PSScriptRoot '母版库') $libraryNames[$i]) -Destination $staged -Force
    '/resource:'+$staged+',Payload.Library'+$i
  }
  $spurStage=Join-Path $stage 'spur-master.SLDPRT'
  $helicalStage=Join-Path $stage 'helical-master.SLDPRT'
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot '圆柱直齿轮方程母版.SLDPRT') -Destination $spurStage -Force
  Copy-Item -LiteralPath (Join-Path $PSScriptRoot '圆柱斜齿轮左旋方程母版.SLDPRT') -Destination $helicalStage -Force
  $polyhedronStage=Join-Path $stage 'polyhedron.PRTDOT'
  Copy-Item -LiteralPath (Join-Path (Join-Path $PSScriptRoot '母版库') '正n面体.PRTDOT') -Destination $polyhedronStage -Force
  & $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll /win32icon:girl-greenhouse-rounded.ico /win32manifest:installer.manifest /resource:NebulaGearStudioV5.exe,Payload.AppExe /resource:girl-greenhouse.png,Payload.Hero /resource:girl-greenhouse-rounded.ico,Payload.IconIco ('/resource:'+$spurStage+',Payload.SpurGearMaster') ('/resource:'+$helicalStage+',Payload.HelicalGearMaster') ('/resource:'+$polyhedronStage+',Payload.Polyhedron') $libraryResources /resource:README.txt,Payload.Readme /out:SetupV50.exe Installer.cs
  if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
} finally {Pop-Location}
