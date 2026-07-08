ARG SAFARSUITE_LOCAL_SERVER_RUNTIME_BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
FROM ${SAFARSUITE_LOCAL_SERVER_RUNTIME_BASE_IMAGE} AS runtime-base
USER root
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    SAFARSUITE_LOCAL_API_ASPNETCORE_URLS=http://0.0.0.0:8080 \
    SAFARSUITE_LOCAL_SERVER_CONFIG_DIR=/etc/safarsuite/local-server \
    SAFARSUITE_LOCAL_SERVER_STATE_DIR=/var/lib/safarsuite/local-server

ENTRYPOINT []
HEALTHCHECK NONE

COPY src/SafarSuite.LocalServer.Api/docker/safarsuite-local-api /usr/local/bin/safarsuite-local-api
COPY src/SafarSuite.LocalServer.Api/docker/safarsuite-local-worker /usr/local/bin/safarsuite-local-worker
COPY src/SafarSuite.LocalServer.Api/docker/safarsuite-local-agent /usr/local/bin/safarsuite-local-agent

RUN sed -i 's/\r$//' /usr/local/bin/safarsuite-local-api /usr/local/bin/safarsuite-local-worker /usr/local/bin/safarsuite-local-agent \
    && chmod +x /usr/local/bin/safarsuite-local-api /usr/local/bin/safarsuite-local-worker /usr/local/bin/safarsuite-local-agent \
    && mkdir -p /etc/safarsuite/local-server/certs /var/lib/safarsuite/local-server \
    && chown -R app:app /etc/safarsuite /var/lib/safarsuite /app

USER app
EXPOSE 8080
CMD ["safarsuite-local-api"]

FROM runtime-base AS runtime-from-publish
ARG SAFARSUITE_LOCAL_SERVER_PUBLISH_DIR=artifacts/localserver-api-publish
COPY --chown=app:app ${SAFARSUITE_LOCAL_SERVER_PUBLISH_DIR}/ ./

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY src/SafarSuite.ControlDesk.Contracts/SafarSuite.ControlDesk.Contracts.csproj src/SafarSuite.ControlDesk.Contracts/
COPY src/SafarSuite.LocalServer.Domain/SafarSuite.LocalServer.Domain.csproj src/SafarSuite.LocalServer.Domain/
COPY src/SafarSuite.LocalServer.Application/SafarSuite.LocalServer.Application.csproj src/SafarSuite.LocalServer.Application/
COPY src/SafarSuite.LocalServer.Infrastructure/SafarSuite.LocalServer.Infrastructure.csproj src/SafarSuite.LocalServer.Infrastructure/
COPY src/SafarSuite.LocalServer.Api/SafarSuite.LocalServer.Api.csproj src/SafarSuite.LocalServer.Api/
RUN dotnet restore src/SafarSuite.LocalServer.Api/SafarSuite.LocalServer.Api.csproj

COPY src ./src
RUN dotnet publish src/SafarSuite.LocalServer.Api/SafarSuite.LocalServer.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM runtime-base AS runtime
COPY --from=build --chown=app:app /app/publish ./
