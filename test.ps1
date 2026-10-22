$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'BubuYierPet.exe'
$results = Join-Path $PSScriptRoot ('test-results\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $results | Out-Null
foreach ($test in @(@('--keyboard-test', 'keyboard.txt'), @('--self-test', 'state.txt'))) {
    $report = Join-Path $results $test[1]
    $process = Start-Process -FilePath $exe -ArgumentList @($test[0], ('"' + $report + '"')) -Wait -PassThru
    if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $report)) { throw ('Test failed: ' + $test[0]) }
    Get-Content -LiteralPath $report
}
Write-Host "Reports: $results"
