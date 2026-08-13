FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenPdcAdapter.slnx .
COPY src/OpenPdc/OpenPdc.Client/OpenPdc.Client.csproj         src/OpenPdc/OpenPdc.Client/
COPY src/Shared/OpenObjects.Client/OpenObjects.Client.csproj   src/Shared/OpenObjects.Client/
COPY src/OpenPdc/OpenPdc.Adapter/OpenPdc.Adapter.csproj       src/OpenPdc/OpenPdc.Adapter/
COPY src/OpenPdc/OpenPdc.Worker/OpenPdc.Worker.csproj         src/OpenPdc/OpenPdc.Worker/
RUN dotnet restore src/OpenPdc/OpenPdc.Worker/OpenPdc.Worker.csproj

COPY src/ src/
RUN dotnet publish src/OpenPdc/OpenPdc.Worker/OpenPdc.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenPdc.Worker.dll"]
