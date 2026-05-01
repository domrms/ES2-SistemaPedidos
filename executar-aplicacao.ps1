param(
    [string]$Configuracao = "Debug"
)

$ErrorActionPreference = "Stop"

$raiz = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

$worker = Join-Path $raiz "src\ES2-SistemaPedidos.Worker"
$api = Join-Path $raiz "src\ES2-SistemaPedidos.Api"

Write-Host "Iniciando Worker em paralelo..."
$processoWorker = Start-Process `
    -FilePath $dotnet `
    -ArgumentList @("run", "--project", $worker, "--configuration", $Configuracao) `
    -PassThru

try {
    Write-Host "Iniciando API. O Swagger sera aberto pelo launchSettings em http://localhost:5000/swagger"
    & $dotnet run --project $api --configuration $Configuracao --launch-profile "ES2-SistemaPedidos.Api"
}
finally {
    if ($processoWorker -and -not $processoWorker.HasExited) {
        Write-Host "Encerrando Worker..."
        Stop-Process -Id $processoWorker.Id
    }
}
