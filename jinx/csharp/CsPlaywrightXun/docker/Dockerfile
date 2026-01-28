# Enterprise Automation Framework - Main Dockerfile
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["CsPlaywrightXun/CsPlaywrightXun.csproj", "CsPlaywrightXun/"]
COPY ["CsPlaywrightXun.slnx", "./"]

# Restore dependencies
RUN dotnet restore "CsPlaywrightXun/CsPlaywrightXun.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/CsPlaywrightXun"
RUN dotnet build "CsPlaywrightXun.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CsPlaywrightXun.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install system dependencies for Playwright
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    ca-certificates \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libatspi2.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libwayland-client0 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libxss1 \
    libxtst6 \
    lsb-release \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r automation && useradd -r -g automation -s /bin/bash automation

# Copy published application
COPY --from=publish /app/publish .

# Create necessary directories
RUN mkdir -p /app/config /app/reports /app/logs /app/screenshots /app/testdata && \
    chown -R automation:automation /app

# Copy configuration files
COPY --chown=automation:automation CsPlaywrightXun/src/config/ /app/config/

# Install Playwright browsers
RUN dotnet tool install --global Microsoft.Playwright.CLI && \
    export PATH="$PATH:/root/.dotnet/tools" && \
    playwright install chromium firefox webkit && \
    playwright install-deps

# Switch to non-root user
USER automation

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PLAYWRIGHT_BROWSERS_PATH=/app/.playwright

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD dotnet --info || exit 1

# Expose port (if needed for web interface)
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "CsPlaywrightXun.dll"]