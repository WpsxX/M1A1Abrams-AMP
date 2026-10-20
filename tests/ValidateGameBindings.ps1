param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Gunner HEAT PC'
)

# Read metadata only: loading Unity assemblies into PowerShell can require the game runtime.
$ErrorActionPreference = 'Stop'
$assemblyPath = Join-Path $GameDir 'Bin\GHPC_Data\Managed\Assembly-CSharp.dll'
$stream = [System.IO.File]::OpenRead($assemblyPath)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
    $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    $types = @{}
    foreach ($handle in $metadata.TypeDefinitions) {
        $definition = $metadata.GetTypeDefinition($handle)
        $name = $metadata.GetString($definition.Namespace) + '.' + $metadata.GetString($definition.Name)
        $fields = @($definition.GetFields() | ForEach-Object {
            $metadata.GetString($metadata.GetFieldDefinition($_).Name)
        })
        $methods = @($definition.GetMethods() | ForEach-Object {
            $method = $metadata.GetMethodDefinition($_)
            $parameters = @($method.GetParameters() | ForEach-Object {
                $parameter = $metadata.GetParameter($_)
                if ($parameter.SequenceNumber -gt 0) { $metadata.GetString($parameter.Name) }
            })
            [pscustomobject]@{ Name = $metadata.GetString($method.Name); Parameters = $parameters }
        })
        $types[$name] = [pscustomobject]@{ Fields = $fields; Methods = $methods }
    }

    $checks = @(
        @{ Type = 'GHPC.Weapons.FireControlSystem'; Fields = '_averageTraverseRate', '_lastAngleAdjust', '_fixParallaxForVectorMode', '_useSeparateLead', '_trackingEnabled', '_leadUpdatesLocked', '_autoDumpViaPalmSwitches'; Methods = 'AdjustAimByRatio', 'doTrackingUpdate', 'GetLeadWorldOffset' },
        @{ Type = 'GHPC.Camera.CameraSlot'; Fields = '_reparentTarget', '_pairedOptic'; Methods = 'ThisActiveChanged' },
        @{ Type = 'GHPC.Crew.CrewManager'; Methods = 'DisableGunner', 'DisableCommander', 'IsCrewMemberConscious', 'DisableCrewMemberBase' },
        @{ Type = 'GHPC.Mission.UnitSpawner'; Methods = 'SpawnUnit' },
        @{ Type = 'GHPC.Weapons.AmmoFeed'; Fields = '_feedClipMain', '_queuedClipTypeLockedIn', '_totalCycleTime'; Methods = 'set_LoadedClipType', 'set_AmmoTypeInBreech', 'set_QueuedClipType', 'TryFinalFeedNewClip', 'TryFinalFeedNewRound', 'ToggleExclusiveItems' },
        @{ Type = 'GHPC.Weapons.LoadoutManager'; Fields = '_totalAmmoTypes', '_totalAmmoCount'; Methods = 'RefreshSnapshot', 'RegisterAllBallistics', 'SpawnCurrentLoadout' },
        @{ Type = 'GHPC.Weapons.WeaponSystem'; Fields = 'CodexEntry'; Methods = 'Fire', 'newRoundInBreech', 'SetCycleTime' },
        @{ Type = 'GHPC.Weapons.LiveRound'; Fields = '_terrainHit', '_didTerrainHit', '_fuzeCompleted', '_materialHit'; Methods = 'logHit', 'createExplosion' },
        @{ Type = 'GHPC.UI.Hud.WeaponHud'; Fields = '_playerInput', '_hudText', '_sb'; Methods = 'Update' },
        @{ Type = 'GHPC.Player.PlayerInput'; Fields = 'ammoKeys' },
        @{ Type = 'GHPC.Camera.BufferedCameraFollow'; Fields = 'SNAPPINESS', 'MAX_AIM_DISTANCE', 'aimSensitivity3P', 'commanderHead' }
    )
    $passed = 0
    foreach ($check in $checks) {
        $type = $types[$check.Type]
        if ($null -eq $type) { throw "Missing type: $($check.Type)" }
        foreach ($field in $check.Fields) {
            if ($type.Fields -notcontains $field) { throw "Missing field: $($check.Type).$field" }
            $passed++
        }
        foreach ($method in $check.Methods) {
            if ($type.Methods.Name -notcontains $method) { throw "Missing method: $($check.Type).$method" }
            $passed++
        }
    }

    $impact = @($types['GHPC.Weapons.LiveRound'].Methods | Where-Object {
        $_.Name -eq 'logHit' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0] -eq 'hitPoint'
    })
    if ($impact.Count -ne 1) { throw 'LiveRound.logHit must have one hitPoint parameter for the feedback patch.' }
    $passed++
    $explosion = @($types['GHPC.Weapons.LiveRound'].Methods | Where-Object {
        $_.Name -eq 'createExplosion' -and $_.Parameters.Count -eq 5
    })
    if ($explosion.Count -ne 1) { throw 'MPAT requires the five-parameter createExplosion overload.' }
    $passed++
    $spawn = @($types['GHPC.Mission.UnitSpawner'].Methods | Where-Object {
        $_.Name -eq 'SpawnUnit' -and $_.Parameters.Count -eq 4
    })
    if ($spawn.Count -eq 0) { throw 'M1 conversion requires a four-parameter SpawnUnit overload.' }
    $passed++

    $feedbackPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'CrowsShotFeedback.cs'
    $feedback = Get-Content -LiteralPath $feedbackPath -Raw
    if ($feedback -notmatch 'HarmonyPatch\(typeof\(LiveRound\),\s*"logHit"' -or $feedback -match 'typeof\(OldLiveRound\)') {
        throw 'Shot feedback must patch the active LiveRound class, not the legacy OldLiveRound.'
    }
    $passed++
    Write-Output "PASS: $passed game member/signature/source binding checks. Metadata only; no game runtime exercised."
} finally {
    $pe.Dispose()
    $stream.Dispose()
}
