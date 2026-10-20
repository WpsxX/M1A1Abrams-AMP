param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Gunner HEAT PC',
    [string]$Scratch = 'I:\GHPC\_build'
)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$managed = Join-Path $GameDir 'Bin\GHPC_Data\Managed'
$csc = 'C:\Program Files\dotnet\sdk\10.0.401\Roslyn\bincore\csc.exe'
$framework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
$reticleExe = Join-Path $Scratch 'ReticleSizeRulesTest.exe'
& $csc /nologo /target:exe "/out:$reticleExe" "/reference:$framework\mscorlib.dll" "/reference:$managed\UnityEngine.CoreModule.dll" "/reference:$managed\netstandard.dll" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\Unity.TextMeshPro.dll" (Join-Path $project 'ReticleSizeRules.cs') (Join-Path $PSScriptRoot 'ReticleSizeRulesTest.cs')
if ($LASTEXITCODE -ne 0) { throw 'Reticle test compilation failed' }
& $reticleExe $managed
if ($LASTEXITCODE -ne 0) { throw 'Reticle sizing test failed' }

# Read official assembly metadata; no Unity scene or game installation changes.
Add-Type -Path (Join-Path $GameDir 'Bin\MelonLoader\net35\Mono.Cecil.dll')
$module = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $managed 'Assembly-CSharp.dll'))
try {
    $checks = @{
        'GHPC.Weapons.MissileGuidanceUnit' = @('AddMissile','PerformGuidance','set_RangeSetting','add_GuidanceStopped','remove_GuidanceStopped')
        'GHPC.Weapons.AmmoFeed' = @('_missileGuidance','MissileGuidanceStopped','_feedClipMain','_feedClipAux','_clipFeedTime','_roundFeedTime','_clipFeedStage','_roundFeedStage','set_LoadedClipType','set_Reloading','set_Cycling','set_WaitingOnMissile')
        'GHPC.Weapons.LoadoutManager' = @('StopRestock','LoadedAmmoTypes')
        'GHPC.Effects.ParticleEffectsManager' = @('_impactEffectsScriptable')
        'GHPC.Effects.ImpactDecalsManager' = @('_ImpactDecalsScriptable')
        'GHPC.PhysicsHelpers.RaycastColliderUtils' = @('Raycast')
        # Everything the AMP fuze reaches into. AmpProxyRound is driven from the round's own
        # flight step (DoUpdate) and rewrites the native detonation bookkeeping before it
        # commits the airburst, so each of these names is load-bearing: a renamed field would
        # silently turn the fuze off or swallow the explosion effect again.
        'GHPC.Weapons.LiveRound' = @('Start','DoUpdate','createExplosion','Shooter',
            '_hasStartBeenCalled','_detonated','_rangedFuseActive','_rangedFuseCountdown',
            '_terrainHit','_didTerrainHit','_materialHit','_impactNormal',
            '_didFuzedEffect','_didTerrainImpactVfx','_didNonTerrainImpactVfx',
            '_fuzeCompleted','_failedToDetonate','_exploded')
        'GHPC.UI.Hud.WeaponHud' = @('_timeLeft','_hudText','_sb','get_IsInitialized')
        'GHPC.NwhChassis' = @('_maxForwardSpeed','_maxReverseSpeed')
        'Reticle.ReticleMesh' = @('cachedReticles','Load')
    }
    foreach ($typeName in $checks.Keys) {
        $type = $module.Types | Where-Object FullName -eq $typeName
        if (-not $type) { throw "Missing type: $typeName" }
        foreach ($member in $checks[$typeName]) {
            if ($type.Fields.Name -notcontains $member -and $type.Methods.Name -notcontains $member) {
                throw "Missing member: ${typeName}::$member"
            }
        }
    }
    'PASS: guidance, reload and effects bindings match the installed game API.'
}
finally { $module.Dispose() }

foreach ($name in @('CrowsAmmoRules','AmmoLoadoutRules','AmpFuzeState')) {
    $exe = Join-Path $Scratch ($name + 'Test.exe')
    & $csc /nologo /target:exe "/out:$exe" "/reference:$framework\mscorlib.dll" (Join-Path $project ($name + '.cs')) (Join-Path $PSScriptRoot ($name + 'Test.cs'))
    if ($LASTEXITCODE -ne 0) { throw "$name test compilation failed" }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw "$name test failed" }
}

# Exercise the actual conversion and guidance modules with native API substitutes.
& dotnet run --project (Join-Path $PSScriptRoot 'AmmoIntegrationTests.csproj') -c Release '-p:BaseIntermediateOutputPath=obj/ammo-integration/' '-p:BaseOutputPath=bin/ammo-integration/'
if ($LASTEXITCODE -ne 0) { throw 'Ammo conversion/guidance integration regression failed' }
