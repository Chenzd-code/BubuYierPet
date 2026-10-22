$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'The x64 .NET Framework C# compiler is required.' }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) { throw 'No source files found in src.' }
& $compiler /nologo /utf8output /target:winexe /platform:x64 /optimize+ "/out:$root\BubuYierPet.exe" "/win32icon:$root\assets\pet.ico" /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "/r:$framework\WPF\PresentationFramework.dll" "/r:$framework\WPF\PresentationCore.dll" "/r:$framework\WPF\WindowsBase.dll" $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Host 'Built BubuYierPet.exe. Keep assets alongside it.'
