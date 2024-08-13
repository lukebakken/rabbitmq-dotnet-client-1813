# ExchangeRateManager

This is an example project that combines multiple technologies around a .NET Web API application. It also serves as a scaffold project for bootstrapping my .NET projects developments. Took about 3 weeks to implement this on my free time and weekends.

This RESTful service that consumes Alpha Vantage Web API for the Foreign Exchange (ForEx) Rates between two currencies.

The request calls the third party API and processes the response, returning:
- a 200 with the expected message,
- a 402 if the service use has reached the limit on a ProblemDetails structure,
- a 400 if the inserted currencies are not valid on a ProblemDetails structure.

Getting the rates works on two possible ways.
 - Prefer live data, using stored data as fallback.
 - Prefer stored data, using live data if the stored expired after a set period of time or does not exists, falling back to the expired if the external service fails.

The response is always immediately returned. When consuming live data, the service will enqueue a Hangfire background job to update the rate, saving it on a PostgreSQL database, using Entity Framework as ORM.

Optionally the entities are cached on a ValKey/Redis Distributed cache or In memory cache for faster response.

If there is a new rate on the source, it will also trigger the RabbitMQ message service to broadcast the update to other subscribed services.

## Features
The project has the following features architectures and technologies:
- Clean architecture using Controller Service Repository structure
- Sub-projects organized by component level
- AutoMapping profiles for mapping between types
- Data Transfer Objects (DTOs)
- Dependencies self discovery at startup. No need to inject them manually
- Keyed services automatic selection using appSettings.json
- Categorization at component level using `IClient` `IService` and `IRepository` interfaces
- Unit tests and integration tests
- Use of Fixures and Fakers for automatically generation of realistic test data.
- Use of Distributed cache
- Message queueing
- Technologies:
  - ASP.NET 8
  - PostgreSQL
  - Entity Framework
  - Valkey/Redis distributed cache
  - RabbitMQ Message queueing service
  - Hangfire for background and scheduled jobs

## Project Structure
The project has the following structure:

- `src`
  - `ExchangeRateManager`\
    Project entry point, Controllers Service Registration, configurations and startup.
  - `ExchangeRateManager.ApiClients`\
    API Clients, AlphaVantage API and an equivalent stub.
  - `ExchangeRateManager.Common`\
    Core and common components, constants, misc, any uncategorizable stuff.
  - `ExchangeRateManager.Dtos`\
    Data Transfer Objects, Requests and response POCOs.
  - `ExchangeRateManager.IntegrationTests`\
    Integration tests for the whole web API service.
  - `ExchangeRateManager.MappingProfiles`\
    AutoMapper mapping profiles. No shallow copies or convertions are allowed on the loose.

    **I REALLY MEAN IT 💣🔪💀**
  - `ExchangeRateManager.Repositories`\
    The data access layer, entities, ORM, caching and repositories.
  - `ExchangeRateManager.Services`\
    Business logic, service components between controllers and repositories
  - `ExchangeRateManager.UnitTests`\
    Unit tests for testing each public function atomically.
  - `ExchangeRateManagerApp.MessageQueueListener`\
    A second console application that listens to RabbitMQ message queue.
  
## How to install
This project should work agnostic to the Operating system and is designed to run on a docker container. Although I didn't test on a linux native host, I used  Windows 10 with latest version of WSL2 on a Debian Bookworm WSL container. It should also work the same if using on a LXC container.

### Requirements
- VSCode
- Visual Studio 2022 (any version)
  - C# and Web development tools
  - Container tools
  - .NET SDK 8.0 or higher
  - Fine Code Coverage Extension
- WSL2
- A cup of coffee ☕ or tea 🍵

### Install steps

1. Clone the repository using git
2. On the source folder there are two parted scripts
   1. `Part1_InstallWSLFeature.ps1` - Ensures that the Windows Subsystem for Linux is installed. (requires admin rights)
   2. Restart the system
   3. `Part2_initializeDocker.ps1` - Does a set of multiple tasks:
      - Sets WSL to version 2
      - Installs the latest Debian distro
      - Runs the `initalizeDocker.sh` on linux side:
        - Updates packages with `apt update` and `apt dist-upgrade`
        - Removes any previous version of docker
        - Installs docker and other dependencies
        - Exposes a port the docker user to allow access through a remote connection using docker-cli (unsafe TCP connection, requires more investigation)
        - starts docker
      - Sets and reloads environment variables
      - Creates the test certificate (requires latest dotnet sdk installed)
      - Builds the project solution (requires latest dotnet sdk installed, also does some setup on first time)
      - Reloads the en host aliases to be able to run the app locally
      - tests `docker ps`

When building the project for the first time it will also install the `docker-cli` package. This nuget package is a remote docker command line that we can use to manage docker on the linux side using Visual studio container tools. Although it is partially supported as I was not able to use it to handle docker compose scripts (*legacy `docker-compose` commands*) , as Container tools expects to run using Docker Desktop, not a WSL image. On first build it will run the `/src/ExchangeRateManager/AddDockerToPath.ps1` script that adds the `docker-cli` package to `PATH` user environment variable.

### Run docker compose

After the installation we need a final step, that is to run docker compose.
On the `/src` folder there are four files:
- `docker-compose.yml` - The base compose structure
- `dock-compose.local.yml` - Compose file that complements local environment setup
- `dock-compose.override.yml` - Compose file used as placeholder for a possible deployment on other environments, by replacing the respective environment variables set as `${...}` during a build stage.
- `docker-compose-up.bat`
  - Sets a volume folder for Redis Insights app with the expected user ownership
  - Runs the local compose files as:

    ```sh
    docker compose -f .\docker-compose.yml -f .\docker-compose.local.yml up -d
    ```
    For updating the local compose environment you can just call this last command.

For the first time just run the `docker-compose-up.bat`

## Points to improve / TODO List:
- Improve unit and functional tests
- Use Serilog, improve and customize logging.
- Send logs to a logging service or store them into a table.
- Add authentication/Authorization layers and use JWT tokens
- The HTTPS certificate is only used for example purposes and safe local development. If the service is behind a gateway like nginx we can disable this.
- If the services will only use small clumps of star or snowflake data, change Connection to use a NoSQL database (Ex: MongoDB)
- Implement pipelines for deployment into another environments. Improve and test the compose script (docker-compose.override.yml) for other environments as is is just an example mockup.
- Adopt CI/CD tecnologies like jenkins, gitlab pipelines, azure devops pipelines, terraform, octopus
- Redis/Valkey allows to have persistent shared cache. If the service does not require any kind of relational data from the time being, consider using a shared cache only. No ORM like EF is required.
- Consider switch RabbitMQ.Client to MassTransit Framework, if consumer operations are needed in the future or requirement for better support for asynchronous operations https://masstransit.io/quick-starts/rabbitmq
- Use TLS connection to access docker remotely. Current commands exposes the docker host cli. See initializeDocker.sh https://stackoverflow.com/questions/63416280/how-to-expose-docker-tcp-socket-on-wsl2-wsl-installed-docker-not-docker-deskt

### And that's it. Enjoy. Feel free for feedback for anything that could be improved. 🍺