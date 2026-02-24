FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AssistantHub.Core/AssistantHub.Core.csproj AssistantHub.Core/
COPY AssistantHub.Server/AssistantHub.Server.csproj AssistantHub.Server/
RUN dotnet restore AssistantHub.Server/AssistantHub.Server.csproj
COPY . .
RUN dotnet publish AssistantHub.Server/AssistantHub.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl wget iputils-ping dnsutils traceroute vim sqlite3 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8800
ENTRYPOINT ["dotnet", "AssistantHub.Server.dll"]
