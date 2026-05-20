# syntax=docker/dockerfile:1.7

# ----- build stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json ./
COPY Rbac.sln ./
COPY src/Rbac.Domain/Rbac.Domain.csproj         src/Rbac.Domain/
COPY src/Rbac.Shared/Rbac.Shared.csproj         src/Rbac.Shared/
COPY src/Rbac.Application/Rbac.Application.csproj src/Rbac.Application/
COPY src/Rbac.Infrastructure/Rbac.Infrastructure.csproj src/Rbac.Infrastructure/
COPY src/Rbac.Api/Rbac.Api.csproj               src/Rbac.Api/
COPY tests/Rbac.Tests/Rbac.Tests.csproj         tests/Rbac.Tests/

RUN dotnet restore src/Rbac.Api/Rbac.Api.csproj

COPY . .

RUN dotnet publish src/Rbac.Api/Rbac.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ----- runtime stage -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as non-root
RUN groupadd --gid 1000 app && useradd --uid 1000 --gid app --shell /bin/sh --create-home app

COPY --from=build --chown=app:app /app/publish ./
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Rbac.Api.dll"]
