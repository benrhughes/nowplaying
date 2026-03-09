FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# frontend tests stage
FROM node:22-slim AS frontend-tests
WORKDIR /app
COPY package.json package-lock.json vitest.config.js ./
COPY src/nowplaying/wwwroot/js ./src/nowplaying/wwwroot/js
COPY src/nowplaying.frontend.tests ./src/nowplaying.frontend.tests
RUN npm ci && npm test -- --run

FROM build AS dotnet-build
# Ensure frontend tests pass before proceeding
COPY --from=frontend-tests /app/package.json /tmp/package.json
WORKDIR /src
# optional test run can be skipped in constrained environments (xunit.analyzers often fails in containers)
ARG RUN_TESTS=false

# Copy the solution and project files
COPY ["src/nowplaying/NowPlaying.csproj", "nowplaying/"]
COPY ["src/nowplaying.tests/nowplaying.tests.csproj", "nowplaying.tests/"]
COPY ["src/nowplaying.sln", "."]
COPY [".stylecop.json", ".editorconfig", "../"]

# Restore dependencies
RUN dotnet restore nowplaying.sln

# Copy the rest of the source code
COPY src/ .

RUN dotnet test nowplaying.tests/nowplaying.tests.csproj --no-restore --verbosity minimal /p:RunAnalyzers=false

# Build the application
RUN cd nowplaying && dotnet build -c Release --no-restore

# Publish the application
RUN cd nowplaying && dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy published application
COPY --from=dotnet-build /app/publish .

# Create non-root user for security
RUN useradd -m -u 10001 appuser && \
    chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NowPlaying.dll"]
