FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenPdcAdapter.slnx .
COPY src/OpenPdc.Client/OpenPdc.Client.csproj           src/OpenPdc.Client/
COPY src/OpenObjects.Client/OpenObjects.Client.csproj   src/OpenObjects.Client/
COPY src/OpenPdc.Adapter/OpenPdc.Adapter.csproj         src/OpenPdc.Adapter/
COPY src/OpenPdc.Worker/OpenPdc.Worker.csproj           src/OpenPdc.Worker/
RUN dotnet restore src/OpenPdc.Worker/OpenPdc.Worker.csproj

COPY src/ src/
RUN dotnet publish src/OpenPdc.Worker/OpenPdc.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenPdc.Worker.dll"]
