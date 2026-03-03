FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the solution and project files
COPY ["src/bcmasto/BcMasto.csproj", "src/bcmasto/"]
COPY ["src/bcmasto.tests/bcmasto.tests.csproj", "src/bcmasto.tests/"]
COPY ["src/bcmasto.sln", "src/"]

# Restore dependencies
RUN cd src && dotnet restore bcmasto.sln

# Copy the rest of the source code
COPY src/ src/

# Run tests
RUN cd src && dotnet test bcmasto.tests/bcmasto.tests.csproj --no-restore --verbosity minimal

# Build the application
RUN cd src/bcmasto && dotnet build -c Release --no-restore

# Publish the application
RUN cd src/bcmasto && dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy client files to wwwroot
COPY client/ wwwroot/

# Create non-root user for security
RUN useradd -m -u 1000 appuser && \
    chown -R appuser:appuser /app
USER appuser

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BcMasto.dll"]
