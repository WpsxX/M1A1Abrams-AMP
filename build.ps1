# Builds the M1A1Abrams mod without going through msbuild or cmd.exe.
#
# build-m1a1.ps1 runs `dotnet msbuild` for a diagnostic log (whose contents it never actually
# reads - the reference list is built from the directories on its own) and then shells out to
# cmd.exe for the redirect. Neither survives this harness's stdio handling, so this variant drops
# the msbuild step and invokes csc.exe directly from PowerShell.

param(
    [string]$ProjectDir = '',
    [string]$Configuration = 'Release',
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Gunner HEAT PC',
    [string]$SdkVersion = '10.0.401',
    [switch]$NoDeploy
)

if (-not $ProjectDir) { $ProjectDir = $PSScriptRoot }

$ErrorActionPreference = 'Stop'

$csproj = Join-Path $ProjectDir 'M1A1Abrams.csproj'
$scratch = 'I:\GHPC\_build'
$rsp = Join-Path $scratch 'att.rsp'
$cscOut = Join-Path $scratch 'att_csc.log'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null

$csc = "C:\Program Files\dotnet\sdk\$SdkVersion\Roslyn\bincore\csc.exe"
if (-not (Test-Path $csproj)) { throw "project not found: $csproj" }
if (-not (Test-Path $csc)) { throw "csc not found: $csc" }

$managed = Join-Path $GameDir 'Bin\GHPC_Data\Managed'
$melon = Join-Path $GameDir 'Bin\MelonLoader\net35'
$framework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (-not (Test-Path $managed)) { throw "game Managed dir not found: $managed" }
if (-not (Test-Path $framework)) { throw "net472 reference assemblies not found: $framework" }

# The framework's own assemblies come first, so mscorlib/System are never taken from one of the
# game's same-named DLLs. NB: the framework's Facades folder is deliberately *not* scanned - the
# game's own netstandard.dll (2.1) is the one Unity's assemblies were built against, and the
# net472 facade (2.0) leaves every UnityEngine reference unresolved (CS0012 on Object).
$references = New-Object System.Collections.Generic.List[string]
$seen = New-Object System.Collections.Generic.HashSet[string]

# Native/IL-less modules that live next to the reference assemblies but are not metadata
# assemblies (csc: CS1509/CS0009 if referenced).
[void]$seen.Add('System.EnterpriseServices.Wrapper.dll')
[void]$seen.Add('System.EnterpriseServices.Thunk.dll')

# MonoMod.Backports re-declares IReadOnlyList<T> & co. for net35. Referencing it next to net472's
# mscorlib makes every use of those types ambiguous (CS0433), and nothing here needs it.
[void]$seen.Add('MonoMod.Backports.dll')

foreach ($scope in @($framework, $managed, $melon)) {
    foreach ($file in (Get-ChildItem $scope -Filter '*.dll')) {
        if (-not $seen.Add($file.Name)) { continue }
        $references.Add($file.FullName)
    }
}

[xml]$project = Get-Content -LiteralPath $csproj
$sources = @($project.Project.ItemGroup.Compile | ForEach-Object {
    if ($_.Include) { Join-Path $ProjectDir $_.Include }
})
foreach ($source in $sources) {
    if (-not (Test-Path -LiteralPath $source)) { throw "source not found: $source" }
}

if ($sources.Count -eq 0) { throw "no source files found under $ProjectDir" }

$out = Join-Path $ProjectDir "obj\$Configuration\M1A1Abrams.dll"
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null

$arguments = New-Object System.Collections.Generic.List[string]
$arguments.Add('/nologo')
$arguments.Add('/target:library')
$arguments.Add('/warn:4')
$arguments.Add('/optimize+')
$arguments.Add('/deterministic+')
$arguments.Add('/debug:pdbonly')
$arguments.Add('/filealign:512')
$arguments.Add('/define:TRACE')
$arguments.Add('/nostdlib+')
$arguments.Add('/noconfig')
$arguments.Add('/errorreport:prompt')
$arguments.Add("/out:`"$out`"")
foreach ($reference in $references) { $arguments.Add("/reference:`"$reference`"") }
foreach ($source in $sources) { $arguments.Add("`"$source`"") }

# csc only honours UTF-8 in a response file when it carries a BOM; without one it reads the file
# as ANSI and the project's Chinese directory name turns into '?' characters.
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText($rsp, ($arguments -join "`r`n"), $utf8Bom)

Write-Host "[build] csc ($($sources.Count) sources, $($references.Count) references) ..."
# Start-Process with file redirection on purpose: this harness's sandbox refuses to hand a child
# process a piped stdout, so `& $csc ... | Out-File` fails with "access denied". Its -PassThru
# process object reports an empty ExitCode here as well, so the log is what decides success.
$cscErr = Join-Path $scratch 'att_csc.err'
Remove-Item $cscOut, $cscErr -Force -ErrorAction SilentlyContinue
[void](Start-Process -FilePath $csc -ArgumentList "@$rsp" -NoNewWindow -Wait -PassThru `
    -RedirectStandardOutput $cscOut -RedirectStandardError $cscErr)

$log = @()
if (Test-Path $cscOut) { $log += Get-Content $cscOut }
if (Test-Path $cscErr) { $log += Get-Content $cscErr }
$log | Where-Object { $_ -notmatch 'warning CS2023' } | ForEach-Object { Write-Host "  $_" }

if ($log -match 'error CS|error :') { throw "compilation failed" }
if (-not (Test-Path $out)) { throw "compilation produced no output: $out" }

$dll = Join-Path $ProjectDir "bin\$Configuration\M1A1Abrams.dll"
New-Item -ItemType Directory -Force -Path (Split-Path $dll) | Out-Null
Copy-Item $out $dll -Force
$pdb = [System.IO.Path]::ChangeExtension($out, '.pdb')
if (Test-Path $pdb) { Copy-Item $pdb ([System.IO.Path]::ChangeExtension($dll, '.pdb')) -Force }

Write-Host "[build] ok: $dll ($((Get-Item $dll).Length) bytes)"

if (-not $NoDeploy) {
    $target = Join-Path $GameDir 'Bin\Mods\M1A1Abrams.dll'
    if (Test-Path $target) {
        $backup = "$target.bak-att"
        Copy-Item $target $backup -Force
    }
    Copy-Item $dll $target -Force
    Write-Host "[build] deployed: $target"

    # The symbols go with it. MelonLoader reads a .pdb sitting beside the mod assembly, and without
    # one every exception it catches is a stack of method names with no file and no line - which is
    # the difference between a bug report and a guess.
    $target_pdb = [System.IO.Path]::ChangeExtension($target, '.pdb')
    if (Test-Path $pdb) {
        Copy-Item $pdb $target_pdb -Force
        Write-Host "[build] deployed: $target_pdb"
    }
}
