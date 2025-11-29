$asmPath = Join-Path $env:USERPROFILE '.nuget\packages\krafs.rimworld.ref\1.6.4633\ref\net472\Assembly-CSharp.dll'
$baseDir = Split-Path $asmPath

[System.AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
    param($sender, $args)
    $name = ($args.Name -split ',')[0] + '.dll'
    $candidate = Join-Path $baseDir $name
    if (Test-Path $candidate) {
        return [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)
    }
    return $null
})

$assembly = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($asmPath)
$type = $assembly.GetType('RimWorld.CompVatGrower', $false, $false)
if ($type) {
    Write-Output "Found type: $($type.FullName)"
} else {
    Write-Output "Type not found."
}

