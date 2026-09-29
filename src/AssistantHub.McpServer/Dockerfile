#
#
# Run the docker build command from the repository root.
#
#


#
#
# Build stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore dependencies
COPY src/AssistantHub.Core/AssistantHub.Core.csproj src/AssistantHub.Core/
COPY sdk/csharp/AssistantHub.Sdk/AssistantHub.Sdk.csproj sdk/csharp/AssistantHub.Sdk/
COPY src/AssistantHub.McpServer/AssistantHub.McpServer.csproj src/AssistantHub.McpServer/
RUN dotnet restore "src/AssistantHub.McpServer/AssistantHub.McpServer.csproj"

# Copy source
COPY src/AssistantHub.Core src/AssistantHub.Core
COPY sdk/csharp/AssistantHub.Sdk sdk/csharp/AssistantHub.Sdk
COPY src/AssistantHub.McpServer src/AssistantHub.McpServer

# Build the application
WORKDIR /repo/src/AssistantHub.McpServer
RUN dotnet build "AssistantHub.McpServer.csproj" -c Release -f net10.0 -o /app/build /p:GeneratePackageOnBuild=false /m:1


#
#
# Publish stage
#
#
FROM build AS publish
RUN dotnet publish "AssistantHub.McpServer.csproj" -c Release -f net10.0 -o /app/publish /p:UseAppHost=false /p:GeneratePackageOnBuild=false /p:ErrorOnDuplicatePublishOutputFiles=false


#
#
# Runtime stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final

# The base image excludes man pages, so update-alternatives warns about every
# link to a missing page (vim, traceroute). Keep just those pages.
RUN printf '%s\n' \
    'path-include=/usr/share/man/*vim.1.gz' \
    'path-include=/usr/share/man/*.db.1.gz' \
    'path-include=/usr/share/man/*.db.8.gz' \
    > /etc/dpkg/dpkg.cfg.d/zz-alternatives-man

# debconf's pre-install hook only batches interactive questions, which a
# non-interactive build never asks; without apt-utils it prints a notice.
RUN echo '#clear DPkg::Pre-Install-Pkgs;' > /etc/apt/apt.conf.d/zz-no-preconfigure

RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends iputils-ping traceroute net-tools curl wget dnsutils iproute2 vim file && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8820 8821 8822
ENTRYPOINT ["dotnet", "AssistantHub.McpServer.dll"]
