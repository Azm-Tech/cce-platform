# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Packages.props Directory.Build.props NuGet.config* ./

COPY src/CCE.Api.Common/CCE.Api.Common.csproj           src/CCE.Api.Common/
COPY src/CCE.Api.Internal/CCE.Api.Internal.csproj       src/CCE.Api.Internal/
COPY src/CCE.Application/CCE.Application.csproj         src/CCE.Application/
COPY src/CCE.Domain/CCE.Domain.csproj                   src/CCE.Domain/
COPY src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj src/CCE.Domain.SourceGenerators/
COPY src/CCE.Infrastructure/CCE.Infrastructure.csproj   src/CCE.Infrastructure/
COPY src/CCE.Integration/CCE.Integration.csproj         src/CCE.Integration/

RUN dotnet restore "src/CCE.Api.Internal/CCE.Api.Internal.csproj"

# Source generator input — CCE.Domain reads permissions.yaml (../../).
COPY permissions.yaml ./

COPY src/ src/
RUN dotnet publish "src/CCE.Api.Internal/CCE.Api.Internal.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CCE.Api.Internal.dll"]
