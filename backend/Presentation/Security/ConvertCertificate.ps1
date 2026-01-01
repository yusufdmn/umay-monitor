# PowerShell script to convert PEM to PFX using OpenSSL
# Run this script from the Security directory

param(
    [string]$Password = "ServerMonitoring2025!"
)

$certPem = "cert.pem"
$keyPem = "key.pem"
$pfxOutput = "certificate.pfx"

# Check if OpenSSL is available
$opensslPath = Get-Command openssl -ErrorAction SilentlyContinue

if ($opensslPath) {
    Write-Host "Converting PEM to PFX using OpenSSL..."
    & openssl pkcs12 -export -out $pfxOutput -inkey $keyPem -in $certPem -password "pass:$Password"
    Write-Host "Conversion completed: $pfxOutput"
} else {
    Write-Host "OpenSSL not found. Please install OpenSSL or use the manual method."
    Write-Host "Alternative: Use online converter or install OpenSSL from https://slproweb.com/products/Win32OpenSSL.html"
}
