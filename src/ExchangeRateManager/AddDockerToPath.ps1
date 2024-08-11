param(
	[string] $dockerPath)

$currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
$machinePath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
if (-not $currentPath.Contains($dockerPath)) {
  Write-Information "Adding ""docker-cli"" package to PATH environment variable"
  [Environment]::SetEnvironmentVariable("PATH", "$currentPath$dockerPath;", "User")

  # Currently Docker compose on WSL2 is not supported by visual studio Container tools.
  # Only docker desktop with legacy "docker-compose".
  # Tried to fool Visual studio to think that "docker.exe compose" from docker-cli package is "docker-compose" command.
  #
  # Set-Content "$dockerPath\docker-compose.cmd" "@echo off`ndocker.exe compose %*"
  write-warning "Restart visual studio for changes to take effect. ""docker"" will fail until the session is restarted."
}