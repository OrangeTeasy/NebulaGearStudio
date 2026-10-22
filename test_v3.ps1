param([ValidateSet('All','Helical','Worm','Wheel')][string]$Component='All')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$out=Join-Path $root 'v3-tests'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'SolidWorksPrimitiveBuilderV3.exe'))
$builder=$assembly.GetType('SolidWorksPrimitiveBuilderV2.SolidWorksBuilder',$true)
$flags=[Reflection.BindingFlags]'Static,NonPublic,InvokeMethod'

function New-Options([string]$typeName,[hashtable]$values){
  $type=$assembly.GetType($typeName,$true)
  $obj=[Activator]::CreateInstance($type,$true)
  foreach($key in $values.Keys){$type.GetField($key,[Reflection.BindingFlags]'Instance,Public,NonPublic').SetValue($obj,$values[$key])}
  return $obj
}

function Run-Model([string]$method,[object]$options){
  $methodInfo=$builder.GetMethod($method,[Reflection.BindingFlags]'Static,NonPublic')
  if($null -eq $methodInfo){throw "Method not found: $method"}
  $options=$options.PSObject.BaseObject
  $argsList=New-Object 'object[]' 4
  $argsList[0]=$options
  $argsList[1]=[string]$script:out
  $argsList[2]=[bool]$false
  $argsList[3]=[bool]$false
  $result=$methodInfo.Invoke($null,$argsList)
  $resultType=$result.GetType()
  $path=$resultType.GetField('PartPath',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($result)
  $message=$resultType.GetField('Message',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($result)
  Write-Output $message
  if(!(Test-Path -LiteralPath $path)){throw "Missing SLDPRT: $path"}
  Write-Output ("PASS_METHOD="+$method)
  Write-Output ("PART="+$path)
}

if($Component -in @('All','Helical')){
  $helical=New-Options 'SolidWorksPrimitiveBuilderV2.HelicalGearOptions' @{NormalModule=[double]2;Teeth=[int]20;NormalPressure=[double]20;Width=[double]10;Bore=[double]8;HelixAngle=[double]15;Shift=[double]0;Addendum=[double]1;Clearance=[double]0.25;LeftHand=$false}
  Run-Model 'CreateHelicalGear' $helical
}

$worm=New-Options 'SolidWorksPrimitiveBuilderV2.WormSetOptions' @{Module=[double]2;Pressure=[double]20;WormLength=[double]18;DiameterFactor=[double]10;Starts=[int]2;LeftHand=$false}
if($Component -in @('All','Worm')){Run-Model 'CreateWormPart' $worm}
$wheel=New-Options 'SolidWorksPrimitiveBuilderV2.WormSetOptions' @{Module=[double]2;Pressure=[double]20;DiameterFactor=[double]10;WheelWidth=[double]10;WheelBore=[double]10;Starts=[int]2;WheelTeeth=[int]30;LeftHand=$false}
if($Component -in @('All','Wheel')){Run-Model 'CreateWormWheelPart' $wheel}
