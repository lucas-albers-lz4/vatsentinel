$asmPath = Join-Path $env:USERPROFILE '.nuget\packages\krafs.rimworld.ref\1.6.4633\lib\net472\Assembly-CSharp.dll'
$baseDir = Split-Path $asmPath

if (-not $script:resolverRegistered) {
    $script:resolverRegistered = $true
    [System.AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({
        param($sender, $args)
        $name = ($args.Name -split ',')[0] + '.dll'
        $candidate = Join-Path $baseDir $name
        if (Test-Path $candidate) {
            return [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)
        }
        return $null
    })
}

$assembly = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($asmPath)
try {
    $types = $assembly.GetTypes()
} catch [System.Reflection.ReflectionTypeLoadException] {
    $_.Exception.LoaderExceptions | ForEach-Object { Write-Warning $_.Message }
    $types = $_.Exception.Types
}

$types |
    Where-Object { $_ -and $_.FullName -like '*Vat*' } |
    Sort-Object FullName |
    ForEach-Object { $_.FullName }

