param(
    [string] $TestParent = (Join-Path ([IO.Path]::GetTempPath()) 'SecureFileManager.Stage05')
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parent = [IO.Path]::GetFullPath($TestParent)
if ($parent.Equals($repository, [StringComparison]::OrdinalIgnoreCase) -or
    $parent.StartsWith($repository.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or
    $parent -match '(?i)(^|[\\/])Storage([\\/]|$)') { throw 'Choose a test parent outside the repository and working Storage.' }
$runDirectory = [IO.Path]::GetFullPath((Join-Path $parent ('run-' + [Guid]::NewGuid().ToString('N'))))
if (-not $runDirectory.StartsWith($parent.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or
    (Test-Path -LiteralPath $runDirectory)) { throw 'Unsafe or existing test run directory.' }
for ($ancestor = [IO.DirectoryInfo]::new($parent); $null -ne $ancestor; $ancestor = $ancestor.Parent) {
    if ($ancestor.Exists -and ($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Test parent ancestors must not be reparse points.' }
}
[void][IO.Directory]::CreateDirectory($runDirectory)
for ($ancestor = [IO.DirectoryInfo]::new($runDirectory); $null -ne $ancestor; $ancestor = $ancestor.Parent) {
    if (($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Test directory ancestors must not be reparse points.' }
}
$variables = @('SFM_RUN_SQL_TESTS','SFM_ENVIRONMENT','SFM_STORAGE_TEST_ROOT','SFM_EXISTING_TEST_DATABASE')
$previous = @{}
foreach ($name in $variables) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
function Remove-OwnedTree([string] $path) {
    $full = [IO.Path]::GetFullPath($path)
    if ($full -ne $runDirectory -and -not $full.StartsWith($runDirectory + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Cleanup escaped the owned run directory.'
    }
    $attributes = [IO.File]::GetAttributes($full)
    if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) { [IO.Directory]::Delete($full, $false) }
        else { [IO.File]::Delete($full) }
    } elseif (($attributes -band [IO.FileAttributes]::Directory) -ne 0) {
        foreach ($child in [IO.Directory]::EnumerateFileSystemEntries($full)) { Remove-OwnedTree $child }
        [IO.Directory]::Delete($full, $false)
    } else { [IO.File]::Delete($full) }
}
$testExit = 1
try {
    $env:SFM_RUN_SQL_TESTS = '1'; $env:SFM_ENVIRONMENT = 'Development'
    $env:SFM_STORAGE_TEST_ROOT = $runDirectory
    [Environment]::SetEnvironmentVariable('SFM_EXISTING_TEST_DATABASE', $null, 'Process')
    Write-Host "Owned test directory: $runDirectory"
    Push-Location $repository
    try { dotnet test SecureFileManager.sln -c Debug --logger 'trx;LogFileName=stage05.trx'; $testExit = $LASTEXITCODE }
    finally { Pop-Location }
} finally {
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    Remove-OwnedTree $runDirectory
    Write-Host 'Owned test directory removed without following reparse points.'
}
exit $testExit
