ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/SafarSuite.ControlDesk.Contracts/SafarSuite.ControlDesk.Contracts.csproj src/SafarSuite.ControlDesk.Contracts/
COPY src/SafarSuite.ControlDesk.Domain/SafarSuite.ControlDesk.Domain.csproj src/SafarSuite.ControlDesk.Domain/
COPY src/SafarSuite.ControlDesk.Application/SafarSuite.ControlDesk.Application.csproj src/SafarSuite.ControlDesk.Application/
COPY src/SafarSuite.ControlDesk.Infrastructure/SafarSuite.ControlDesk.Infrastructure.csproj src/SafarSuite.ControlDesk.Infrastructure/
COPY src/SafarSuite.ControlDesk.Api/SafarSuite.ControlDesk.Api.csproj src/SafarSuite.ControlDesk.Api/

RUN dotnet restore src/SafarSuite.ControlDesk.Api/SafarSuite.ControlDesk.Api.csproj

COPY src ./src
RUN dotnet publish src/SafarSuite.ControlDesk.Api/SafarSuite.ControlDesk.Api.csproj \
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
ENTRYPOINT ["dotnet", "SafarSuite.ControlDesk.Api.dll"]
