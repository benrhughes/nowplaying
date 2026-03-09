FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# frontend tests stage
FROM node:22-slim AS frontend-tests
WORKDIR /app
COPY package.json package-lock.json vitest.config.js ./
COPY src/nowplaying/wwwroot/js ./src/nowplaying/wwwroot/js
COPY src/nowplaying.frontend.tests ./src/nowplaying.frontend.tests
RUN npm ci && npm test

FROM build AS dotnet-build
# Ensure frontend tests pass before proceeding
COPY --from=frontend-tests /app/package.json /tmp/package.json
WORKDIR /src

# Copy the solution and project files separately to leverage Docker layer caching
COPY ["src/nowplaying/NowPlaying.csproj", "nowplaying/"]
COPY ["src/nowplaying.tests/nowplaying.tests.csproj", "nowplaying.tests/"]
COPY ["src/nowplaying.sln", "."]
COPY [".stylecop.json", ".editorconfig", "../"]

# Restore dependencies (this layer is cached unless dependencies change)
RUN dotnet restore nowplaying.sln

# Copy the rest of the source code only after dependencies are restored
COPY src/ .

RUN dotnet test nowplaying.tests/nowplaying.tests.csproj --no-restore --verbosity minimal 

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
