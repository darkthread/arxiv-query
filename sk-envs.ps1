param (
    [switch]
    $reset
)
Add-Type -AssemblyName System.Security
function Get-ProtectedEnvironmentVariable {
    param (
        [Parameter(Mandatory = $true)]
        [string]$varName,
        [bool]$setByConsoleWhenEmpty = $false
    )
    $additionalEntropy = @(2, 8, 8, 2, 5, 2, 5, 2)
    $val = [Environment]::GetEnvironmentVariable($varName, "User")
    if ([string]::IsNullOrEmpty($val) -or $reset) {
        if (!$setByConsoleWhenEmpty) { return $val }
        Write-Host "Please input `[$varName]`:"
        $val = Read-Host
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($val)
        $protectedBytes = [System.Security.Cryptography.ProtectedData]::Protect($bytes, $additionalEntropy, "CurrentUser")
        $enc = [Convert]::ToBase64String($protectedBytes)
        [Environment]::SetEnvironmentVariable($varName, $enc, "User")
        return $val
    }
    try {
        $bytes = [Convert]::FromBase64String($val)
        $unprotectedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect($bytes, $additionalEntropy, "CurrentUser")
        return [System.Text.Encoding]::UTF8.GetString($unprotectedBytes)
    }
    catch {
        Write-Error $_.Exception
        throw "Failed to decrypt."
    }
}


Get-ProtectedEnvironmentVariable 'SK_EndPoint' $true
Get-ProtectedEnvironmentVariable 'SK_ApiKey' $true