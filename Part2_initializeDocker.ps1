# Run this command as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if(-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Requires admin rights. Exiting..."
}

# STEP 4: To set the WSL default version to 2.
# Any distribution installed after this, would run on WSL 2
wsl --set-default-version 2

wsl --install -d "Debian"

wsl --cd $(pwd) --exec "./initializeDocker.sh"
 
echo "Setting up user environment variables"
# STEP5: Add environment variable to enable WSL support on Visual Studio Container Tools
[Environment]::SetEnvironmentVariable("VSCT_WslDaemon", "1", "User")
[Environment]::SetEnvironmentVariable("DOCKER_HOST", "tcp://localhost:2375", "User")

echo "Create test certificate"
dotnet dev-certs https --trust
dotnet dev-certs https -ep ./aspnetapp.pfx -p 12345

echo "Build project for docker-cli package resolution"
dotnet build ".\src\ExchangeRateManager\ExchangeRateManager.csproj"

echo "Reload environment Variables"
$env:PATH = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
$env:DOCKER_HOST = [System.Environment]::GetEnvironmentVariable("DOCKER_HOST","User")
$env:VSCT_WslDaemon = [System.Environment]::GetEnvironmentVariable("VSCT_WslDaemon","User")

echo "Add host alias for the tested services, if ran locally"
$hostsPath = "C:\Windows\System32\drivers\etc\hosts"
Add-Content -Path $hostsPath -Value "127.0.0.1 valkey"  -PassThru
Add-Content -Path $hostsPath -Value "127.0.0.1 postgres" -PassThru
Add-Content -Path $hostsPath -Value "127.0.0.1 redisinsight" -PassThru
Add-Content -Path $hostsPath -Value "127.0.0.1 rabbitmq" -PassThru

echo "Testing docker ps"
docker ps