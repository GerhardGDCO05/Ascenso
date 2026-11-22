FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY Ascenso.Becas.Domain/Ascenso.Becas.Domain.csproj Ascenso.Becas.Domain/
COPY Ascenso.Becas.Domain/packages.lock.json Ascenso.Becas.Domain/
COPY Ascenso.Becas.Application/Ascenso.Becas.Application.csproj Ascenso.Becas.Application/
COPY Ascenso.Becas.Application/packages.lock.json Ascenso.Becas.Application/
COPY Ascenso.Becas.Infrastructure/Ascenso.Becas.Infrastructure.csproj Ascenso.Becas.Infrastructure/
COPY Ascenso.Becas.Infrastructure/packages.lock.json Ascenso.Becas.Infrastructure/
COPY Ascenso.Becas.Api/Ascenso.Becas.Api.csproj Ascenso.Becas.Api/
COPY Ascenso.Becas.Api/packages.lock.json Ascenso.Becas.Api/

RUN dotnet restore Ascenso.Becas.Api/Ascenso.Becas.Api.csproj --locked-mode

COPY . .
RUN dotnet publish Ascenso.Becas.Api/Ascenso.Becas.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/almacenamiento \
    && chown -R app:app /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__Provider=Local \
    Storage__Local__RutaBase=/app/almacenamiento

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Ascenso.Becas.Api.dll"]
