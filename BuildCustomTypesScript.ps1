Push-Location;

Try {
    $Items = @(Get-ChildItem -Path:$ProjectDir -Filter:'*.cs' | ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) | Write-Output });

	Set-Location -Path:'../PSPass';

    [System.IO.File]::WriteAllText(((Get-Location) | Join-Path -ChildPath:'CustomTypes.ps1'), (@(
        '$TypeCsCode = @(' | Write-Output
        for ($i = 0; $i -lt $Items.Count; $i++) {
            "@'" | Write-Output;
            $Items[$i] | Write-Output;
            if ($i -lt ($Items.Count - 1)) {
                "'@," | Write-Output;
            } else {
                "'@" | Write-Output;
            }
        }
        ');' | Write-Output
        '$TypeCsFiles = @();' | Write-Output
        'Try {' | Write-Output
        '    $TypeCsCode | ForEach-Object {' | Write-Output
        '        $TempFile = [System.IO.Path]::GetTempPath() | Join-Path -ChildPath:(([Guid]::NewGuid().ToString("n") + ".cs"));' | Write-Output
        '        [System.IO.File]::WriteAllText($TempFile, $_);' | Write-Output
        '        $TypeCsFiles += $TempFile;' | Write-Output
        '    }' | Write-Output
        '    Add-Type -Path:$TypeCsFiles;' | Write-Output
        '} Catch {' | Write-Output
        '    throw;' | Write-Output
        '} Finally {' | Write-Output
        '    foreach ($f in $TypeCsFiles) {' | Write-Output
        '        Try {' | Write-Output
        '            [System.IO.File]::Delete($f);' | Write-Output
        '        } Catch { }' | Write-Output
        '    }' | Write-Output
        '}' | Write-Output
    ) | Out-String));
} Catch {
    throw;
} Finally {
    Pop-Location;
}