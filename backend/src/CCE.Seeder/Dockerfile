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
COPY src/CCE.Seeder/CCE.Seeder.csproj                   src/CCE.Seeder/

RUN dotnet restore "src/CCE.Seeder/CCE.Seeder.csproj"

# Source generator input — CCE.Domain's source generator reads
# permissions.yaml from backend root (two levels up from src/CCE.Domain/).
COPY permissions.yaml ./

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish "src/CCE.Seeder/CCE.Seeder.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage — aspnet:8.0 (NOT runtime:8.0) because the seeder
# transitively pulls Microsoft.AspNetCore.Identity.EntityFrameworkCore
# at compile time; the smaller `runtime:8.0` lacks the ASP.NET Core
# shared-framework assemblies. Same image as API Dockerfiles → shares
# layers across registry pulls.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user (aspnet:8.0 ships an `app` user uid 1654).
USER app

COPY --from=build --chown=app:app /app/publish .

# The seeder also needs the API's appsettings.json to resolve the
# connection string. Copy it from the build stage; production
# overrides via env-vars (DOTNET_ConnectionStrings__Default etc.).
COPY --from=build --chown=app:app /src/src/CCE.Api.External/appsettings.json /app/src/CCE.Api.External/appsettings.json

# No HEALTHCHECK — the migrator is a one-shot console.
# No EXPOSE — it doesn't listen on a port.
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CCE.Seeder.dll"]
# Default command — overridden in compose for prod migration:
#   command: ["--migrate", "--seed-reference"]
CMD []
