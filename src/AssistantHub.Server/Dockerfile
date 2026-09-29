#
#
# Build stage
#
#

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore dependencies
COPY src/AssistantHub.Core/AssistantHub.Core.csproj src/AssistantHub.Core/
COPY src/AssistantHub.Server/AssistantHub.Server.csproj src/AssistantHub.Server/
RUN dotnet restore src/AssistantHub.Server/AssistantHub.Server.csproj

# Build and publish
COPY src/AssistantHub.Core src/AssistantHub.Core
COPY src/AssistantHub.Server src/AssistantHub.Server
RUN dotnet publish src/AssistantHub.Server/AssistantHub.Server.csproj -c Release -o /app/publish /p:GeneratePackageOnBuild=false

# Pre-install Playwright Firefox browser.
# CrawlSharp uses Playwright Firefox for headless browser crawling.
# Pre-installing here avoids a slow download on first crawl at runtime.
# The browser never runs in this stage (the runtime stage installs its system
# libraries), so Playwright's host dependency check is skipped here.
RUN PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS=1 pwsh /app/publish/playwright.ps1 install firefox


#
#
# Runtime stage
#
#

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# The base image excludes man pages, so update-alternatives warns about every
# link to a missing page (vim, traceroute, xdg-utils). Keep just those pages.
RUN printf '%s\n' \
    'path-include=/usr/share/man/*vim.1.gz' \
    'path-include=/usr/share/man/*.db.1.gz' \
    'path-include=/usr/share/man/*.db.8.gz' \
    'path-include=/usr/share/man/*xdg-open.1.gz' \
    > /etc/dpkg/dpkg.cfg.d/zz-alternatives-man

# debconf's pre-install hook only batches interactive questions, which a
# non-interactive build never asks; without apt-utils it prints a notice.
RUN echo '#clear DPkg::Pre-Install-Pkgs;' > /etc/apt/apt.conf.d/zz-no-preconfigure

# Install common utilities and Playwright system dependencies.
# The Playwright libs (libnss3, libgbm1, etc.) are required for headless
# Chromium and Firefox to run inside the container. dbus-x11 satisfies GTK's
# D-Bus session dependency; without it apt picks dbus-user-session, which
# installs systemd (unused in a container, and noisy to configure).
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    gosu \
    dbus-x11 \
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

# Create a non-root user for running the application.
# Playwright's Firefox requires a non-root user for its sandbox to work.
# The entrypoint script handles chown at startup (to fix volume mount
# ownership) and then drops to appuser via gosu. The user is created before
# the copies below so its home directory does not exist yet.
RUN useradd -m -u 1001 appuser \
    && chown appuser:appuser /app

# Copy published application from build stage
COPY --from=build --chown=appuser:appuser /app/publish .

# Copy pre-installed Playwright browsers from build stage
COPY --from=build --chown=appuser:appuser /root/.cache/ms-playwright /home/appuser/.cache/ms-playwright

COPY src/AssistantHub.Server/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 8800
ENTRYPOINT ["/entrypoint.sh"]
