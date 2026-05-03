# syntax=docker/dockerfile:1.7
# Build stage — restore + publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy props + global config first so package restore can cache.
COPY Directory.Packages.props Directory.Build.props NuGet.config* ./

# Copy every csproj before the rest of the source so `dotnet restore`
# layers are cached as long as csproj files don't change.
COPY src/CCE.Api.Common/CCE.Api.Common.csproj           src/CCE.Api.Common/
COPY src/CCE.Api.External/CCE.Api.External.csproj       src/CCE.Api.External/
COPY src/CCE.Application/CCE.Application.csproj         src/CCE.Application/
COPY src/CCE.Domain/CCE.Domain.csproj                   src/CCE.Domain/
COPY src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj src/CCE.Domain.SourceGenerators/
COPY src/CCE.Infrastructure/CCE.Infrastructure.csproj   src/CCE.Infrastructure/
COPY src/CCE.Integration/CCE.Integration.csproj         src/CCE.Integration/

RUN dotnet restore "src/CCE.Api.External/CCE.Api.External.csproj"

# Source generator input — CCE.Domain's source generator reads
# permissions.yaml from backend root (two levels up from src/CCE.Domain/).
COPY permissions.yaml ./

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish "src/CCE.Api.External/CCE.Api.External.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user (aspnet:8.0 ships an `app` user uid 1654).
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CCE.Api.External.dll"]
