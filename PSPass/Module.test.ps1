if ((Get-Module -Name:'PSPass') -ne $null) { Remove-Module -Name:'PSPass' }
Import-Module -Name:'PSPass';

Set-PSPassLocation