param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Gunner HEAT PC',
    [string]$Scratch = 'I:\GHPC\_build',
    [string]$SdkVersion = '10.0.401'
)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force -Path $Scratch | Out-Null
$csc = "C:\Program Files\dotnet\sdk\$SdkVersion\Roslyn\bincore\csc.exe"
$testExe = Join-Path $Scratch 'AmpUpgradeRulesTest.exe'
& $csc /nologo /target:exe '/reference:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll' "/out:$testExe" (Join-Path $project 'AmpUpgradeRules.cs') (Join-Path $PSScriptRoot 'AmpUpgradeRulesTest.cs')
if ($LASTEXITCODE -ne 0) { throw 'Upgrade test compilation failed' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Upgrade rules failed' }

# Check the private members used by reflection/Harmony against the official game assembly.
# This reads metadata only; it does not run Unity or change the installation.
Add-Type -Path (Join-Path $GameDir 'Bin\MelonLoader\net35\Mono.Cecil.dll')
$module = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $GameDir 'Bin\GHPC_Data\Managed\Assembly-CSharp.dll'))
try {
    $checks = @{
        'GHPC.Weapons.AmmoFeed' = @('CalculateTotalTime','ClipReloadStages','SlowReloadMultiplier')
        'GHPC.Weapons.AmmoRack' = @('_retrievalDelaySeconds','_storageDelaySeconds')
        'GHPC.AI.CrewAI' = @('RandomiseIdentifyTargetTimer','ScheduleCommsSweep','_identifyTargetDurationRange','_identifyTargetTimer','_sweepCommsCheckTimer')
        'GHPC.AI.UnitAI' = @('get_SpotTimeMaxDistance','set_SpotTimeMax','set_SpotTimeMin')
        'GHPC.AI.GunnerAI' = @('_sweepAngle','_sweepDirectionShiftSpeed','_sweepSpeed')
        'GHPC.Equipment.VehicleSmokeManager' = @('_smokeGrenadeRequiresCrewBrain','_smokeScreenRequiresCrewBrain','_launchAngle','_distanceRange','_smokeSlots','_smokeGroups','_currentSmokeGroupIndex')
        'GHPC.Camera.BufferedCameraFollow' = @('SNAPPINESS','MAX_AIM_DISTANCE')
        'GHPC.VariableArmor' = @('_armorType')
        'GHPC.Weaponry.Grenade' = @('_effectPrefab','_fuseTimeRange')
        'GHPC.NwhChassis' = @('_originalEnginePower','_originalShiftDuration','_maxForwardSpeed','_maxReverseSpeed','SetEnginePowerPercent','SetTransmissionIntegrityPercent','VehicleController')
        'NWH.VehiclePhysics.Transmission' = @('initialShiftDuration','set_ForwardGears','set_ReverseGears')
    }
    foreach ($typeName in $checks.Keys) {
        $type = $module.Types | Where-Object { $_.FullName -eq $typeName }
        if (-not $type) { throw "Missing type: $typeName" }
        foreach ($member in $checks[$typeName]) {
            if (($type.Fields.Name -notcontains $member) -and ($type.Methods.Name -notcontains $member)) {
                throw "Missing member: ${typeName}::$member"
            }
        }
    }
    $feed = $module.Types | Where-Object FullName -eq 'GHPC.Weapons.AmmoFeed'
    $stage = $feed.NestedTypes | Where-Object Name -eq ReloadStage
    if ($stage.Fields.Name -notcontains 'Duration') { throw 'Missing ReloadStage.Duration' }
    'PASS: all new reflection/Harmony targets exist in the installed game.'
}
finally { $module.Dispose() }
