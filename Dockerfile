FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# optional test run can be skipped in constrained environments (xunit.analyzers often fails in containers)
ARG RUN_TESTS=false

# Copy the solution and project files
COPY ["src/bcmasto/BcMasto.csproj", "bcmasto/"]
COPY ["src/bcmasto.tests/bcmasto.tests.csproj", "bcmasto.tests/"]
COPY ["src/bcmasto.sln", "."]
COPY [".stylecop.json", ".editorconfig", "../"]

# Restore dependencies
RUN dotnet restore bcmasto.sln

# Copy the rest of the source code
COPY src/ .

# Run tests if requested (skipped by default to avoid analyzer package issues in some build contexts)
RUN if [ "$RUN_TESTS" = "true" ]; then \
    dotnet test bcmasto.tests/bcmasto.tests.csproj --no-restore --verbosity minimal /p:RunAnalyzers=false; \
    else \
    echo "Skipping tests (RUN_TESTS=false)"; \
    fi

# Build the application
RUN cd bcmasto && dotnet build -c Release --no-restore

# Publish the application
RUN cd bcmasto && dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create non-root user for security
RUN useradd -m -u 10001 appuser && \
    chown -R appuser:appuser /app
USER appuser

EXPOSE 4444

ENV ASPNETCORE_URLS=http://+:4444
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BcMasto.dll"]
