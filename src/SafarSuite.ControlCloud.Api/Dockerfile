ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/SafarSuite.ControlDesk.Contracts/SafarSuite.ControlDesk.Contracts.csproj src/SafarSuite.ControlDesk.Contracts/
COPY src/SafarSuite.ControlCloud.Domain/SafarSuite.ControlCloud.Domain.csproj src/SafarSuite.ControlCloud.Domain/
COPY src/SafarSuite.ControlCloud.Application/SafarSuite.ControlCloud.Application.csproj src/SafarSuite.ControlCloud.Application/
COPY src/SafarSuite.ControlCloud.Infrastructure/SafarSuite.ControlCloud.Infrastructure.csproj src/SafarSuite.ControlCloud.Infrastructure/
COPY src/SafarSuite.ControlCloud.Api/SafarSuite.ControlCloud.Api.csproj src/SafarSuite.ControlCloud.Api/

RUN dotnet restore src/SafarSuite.ControlCloud.Api/SafarSuite.ControlCloud.Api.csproj

COPY src ./src
RUN dotnet publish src/SafarSuite.ControlCloud.Api/SafarSuite.ControlCloud.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
USER root
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build --chown=app:app /app/publish ./
RUN mkdir -p App_Data && chown -R app:app /app

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SafarSuite.ControlCloud.Api.dll"]
