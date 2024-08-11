#!/bin/bash
#https://dotnetting.net/2022/01/how-to-use-visual-studio-without-docker-desktop-to-debug-a-.net-core-application-running-in-a-container-inside-wsl/

echo "Update Packages"
sudo apt update
sudo apt dist-upgrade -y

echo "Remove previous version of docker..."
sudo apt-get remove docker docker-engine docker.io containerd runc

echo "Installing dependencies"
sudo apt-get install apt-transport-https ca-certificates curl gnupg lsb-release

echo "Setting repos for docker"
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
echo \
  "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

echo "Instaling docker"
sudo apt update
sudo apt install docker-ce docker-ce-cli containerd.io docker-compose

echo "Configure user"
#https://stackoverflow.com/questions/63416280/how-to-expose-docker-tcp-socket-on-wsl2-wsl-installed-docker-not-docker-deskt
#TODO: use TLS secure way
sudo '{"hosts": ["tcp://0.0.0.0:2375", "unix:///var/run/docker.sock"]}' >> /etc/docker/daemon.json
sudo usermod -aG docker $USER


echo starting docker
sudo service docker start
