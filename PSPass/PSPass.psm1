$Script:LastLocalBackup = $null;

Function Get-AppDataPath {
    Param(
        [switch]$Local
    )
    $SpecialFolder = &{ if ($Local) { [System.Environment+SpecialFolder]::LocalApplicationData } else { [System.Environment+SpecialFolder]::ApplicationData } };
    $AppDataPath = [System.Environment]::GetFolderPath($SpecialFolder) | Join-Path -ChildPath:'Leonard T. Erwine';
    if (-not (Test-Path $AppDataPath)) { New-Item -Path:$AppDataPath -ItemType:'Directory' | Out-Null }
    $AppDataPath = $AppDataPath | Join-Path -ChildPath:'PowerShell';
    if (-not (Test-Path $AppDataPath)) { New-Item -Path:$AppDataPath -ItemType:'Directory' | Out-Null }
    $AppDataPath = $AppDataPath | Join-Path -ChildPath:'PSSecureTransfer';
    if (-not (Test-Path $AppDataPath)) { New-Item -Path:$AppDataPath -ItemType:'Directory' | Out-Null }
    $AppDataPath | Write-Output;
}

Function Get-PSPassDrive {
    Param()
    
    if ($Script:PSPassDrive -eq $null) {
        $PSProvider = Get-PSProvider -PSProvider:'FileSystem';
        $CredentialsStoragePath = Get-CredentialsStoragePath;
        $Script:PSPassDrive = Get-PSDrive | Where-Object { $_.Name -eq 'PSPass' };
        if ($Script:PSPassDrive -ne $null) {
            if ($_.Provider -ne $PSProvider -or $_.Root -ne $CredentialsStoragePath) {
                Remove-PSDrive -Name:'PSPass';
                $Script:PSPassDrive = Get-PSDrive | Where-Object { $_.Name -eq 'PSPass' };
                if ($Script:PSPassDrive -ne $null) { throw 'Failed to remove existing "PSPass" PSDrive.' }
            }
        }

        if ($Script:PSPassDrive -eq $null) {
            $Script:PSPassDrive = New-PSDrive -Name:'PSPass' -PSProvider:FileSystem -Root:(Get-CredentialsStoragePath) -Description "Maps to the credentials storage location.";
        }
    }
    
    $Script:PSPassDrive | Write-Output;
}

Function ConvertTo-SafeFileName {
    Param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true)]
        [string[]]$InputText,
        
        [switch]$AllowExtension,
        
        [switch]$IgnorePathSeparatorChars
    )
    
    Begin {
        [char[]]$InvalidFileNameChars = [System.IO.Path]::GetInvalidFileNameChars();
        if ($IgnorePathSeparatorChars) {
            [char[]]$InvalidFileNameChars = $InvalidFileNameChars | Where-Object { $char -ne [System.IO.Path]::DirectorySeparatorChar -and $char -ne [System.IO.Path]::AltDirectorySeparatorChar };
        }
        if ($InvalidFileNameChars -notcontains '_') { [char[]]$InvalidFileNameChars += [char]'_' }
        if (-not $AllowExtension) { [char[]]$InvalidFileNameChars += [char]'.' }
    }
    
    Process {
        foreach ($text in $InputText) {
            if ($text -ne $null -and $text.Length -gt 0) {
                $StringBuilder = New-Object -TypeName:'System.Text.StringBuilder';
                foreach ($char in $text.ToCharArray()) {
                    if ($InvalidFileNameChars -contains $char) {
                        $StringBuilder.AppendFormat('_0x{0:x2}_', [int]$char) | Out-Null;
                    } else {
                        $StringBuilder.Append($char) | Out-Null;
                    }
                }
                
                $StringBuilder.ToString() | Write-Output;
            }
        }
    }
}

Function ConvertFrom-SafeFileName {
    Param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true)]
        [string[]]$InputText
    )
    
    Begin {
        $Regex = New-Object -TypeName:'System.Text.RegularExpressions.Regex' -ArgumentList:('_0x(?<hex>[\da-f]{2})_',
            ([System.Text.RegularExpressions.RegexOptions]::Compiled -bor [System.Text.RegularExpressions.RegexOptions]::Ignorecase));
    }
    
    Process {
        foreach ($text in $InputText) {
            if ($text -ne $null -and $text.Length -gt 0) {
                $MatchCollection = $Regex.Matches($text);
                if ($MatchCollection.Count -eq 0) {
                    $text | Write-Output;
                } else {
                    $StringBuilder = New-Object -TypeName:'System.Text.StringBuilder';
                    $previousEnd = 0;
                    $MatchCollection | ForEach-Object {
                        $Match = $_;
                        if ($Match.Index -gt $previousEnd) { $StringBuilder.Append($text.SubString($previousEnd, $Match.Index - $previousEnd)) | Out-Null }
                        [char]$char = [System.Convert]::ToInt32($Match.Groups['hex'].Value, 16);
                        $StringBuilder.Append($char) | Out-Null;
                        $previousEnd = $Match.Index + $Match.Length;
                    }
                    
                    if ($previousEnd -lt $text.Length) { $StringBuilder.Append($text.SubString($previousEnd)) }
                
                    $StringBuilder.ToString() | Write-Output;
                }
            }
        }
    }
}

Function ConvertTo-LiteralPath {
    Param(
        [Parameter(Mandatory = $true, Position = 0)]
        [AllowNull()]
        [AllowEmptyString()]
        [string]$Path
    )
    
    $PSPassDrive = Get-PSPassDrive;
    $Root = $PSPassDrive.Root;
    if ($Path -eq $null -or $Path -eq '') {
        if ($PSPassDrive.CurrentLocation -ne $null -and $PSPassDrive.CurrentLocation.Length -gt 0) {
            $Root | Join-Path -ChildPath:($PSPassDrive.CurrentLocation) | Write-Output;
        } else {
            $Root | Write-Output;
        }
    } else {
        if ([System.IO.Path]::IsPathRooted($Path)) {
            $Root | Join-Path -ChildPath:(ConvertTo-SafeFileName -Path:$Path -IgnorePathSeparatorChars) | Write-Output;
        } else {
            if ($PSPassDrive.CurrentLocation -ne $null -and $PSPassDrive.CurrentLocation.Length -gt 0) {
                $Root | Join-Path -ChildPath:(ConvertTo-SafeFileName -Path:$Path -IgnorePathSeparatorChars) | Write-Output;
            } else {
                ($Root | Join-Path -ChildPath:($PSPassDrive.CurrentLocation)) | Join-Path -ChildPath:(ConvertTo-SafeFileName -Path:$Path -IgnorePathSeparatorChars) | Write-Output;
            }
        }
    }
}

Function Show-Credentials {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $false, Position = 0, ParameterSetname = "Relative")]
        [string]$Path,
        
        [Parameter(Mandatory = $true, Position = 0, ParameterSetname = "Literal")]
        [string]$LiteralPath,
        
        [Parameter(Mandatory = $false, Position = 1)]
        [string]$Indent = "",
        
        [switch]$Recursive
    )
    
    $ParentPath = $LiteralPath;
    if ($Path -ne $null -and $Path.Length -gt 0) { $ParentPath = ConvertTo-LiteralPath -Path:$Path }
    
    if (Test-Path -Path:$ParentPath) {
        $ParentItem = Get-Item -Path:$ParentPath;
        if ($ParentItem.PSIsContainer) {
            Get-ChildItem -Path:$ParentPath | ForEach-Object {
                if ($_.PSIsContainer) {
                    ('{0}{1}\' -f $Indent, (ConvertFrom-SafeFileName -Name:([System.IO.Path]::GetFileNameWithoutExtension($_.Name)))) | Write-Output;
                    if ($Recursive) { Show-Credentials -LiteralPath:($_.Fullname) -Indent:($Indent + "`t") -Recursive; }
                } else {
                    if ($_.Extension -ieq '.xml') { ($Indent + (ConvertFrom-SafeFileName -Name:([System.IO.Path]::GetFileNameWithoutExtension($_.Name)))) | Write-Output }
                }
            }
        } else {
            if ($_.Extension -ieq '.xml') { ($Indent + (ConvertFrom-SafeFileName -Name:([System.IO.Path]::GetFileNameWithoutExtension($_.Name)))) | Write-Output }
        }
    }
}
