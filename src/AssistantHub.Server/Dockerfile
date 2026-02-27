#
#
# Build stage
#
#

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies
COPY AssistantHub.Core/AssistantHub.Core.csproj AssistantHub.Core/
COPY AssistantHub.Server/AssistantHub.Server.csproj AssistantHub.Server/
RUN dotnet restore AssistantHub.Server/AssistantHub.Server.csproj

# Build and publish
COPY . .
RUN dotnet publish AssistantHub.Server/AssistantHub.Server.csproj -c Release -o /app/publish

# Install Playwright browsers (Chromium and Firefox).
# CrawlSharp uses Playwright for headless browser crawling and requires both.
# Pre-installing here avoids a slow download on first crawl at runtime.
RUN pwsh /app/publish/playwright.ps1 install chromium
RUN pwsh /app/publish/playwright.ps1 install firefox


#
#
# Runtime stage
#
#

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install common utilities and Playwright system dependencies.
# The Playwright libs (libnss3, libgbm1, etc.) are required for headless
# Chromium and Firefox to run inside the container.
RUN apt-get update && apt-get install -y --no-install-recommends \
    gosu \
    curl wget iputils-ping dnsutils traceroute vim sqlite3 \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libdbus-1-3 \
    libatspi2.0-0 \
    libx11-6 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libxcb1 \
    libxkbcommon0 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2t64 \
    fonts-liberation \
    libgtk-3-0t64 \
    libxshmfence1 \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Copy pre-installed Playwright browsers from build stage
COPY --from=build /root/.cache/ms-playwright /home/appuser/.cache/ms-playwright

# Create a non-root user for running the application.
# Playwright's Firefox requires a non-root user for its sandbox to work.
# The entrypoint script handles chown at startup (to fix volume mount
# ownership) and then drops to appuser via gosu.
RUN useradd -m -u 1001 appuser \
    && chown -R appuser:appuser /app \
    && chown -R appuser:appuser /home/appuser

COPY AssistantHub.Server/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 8800
ENTRYPOINT ["/entrypoint.sh"]
