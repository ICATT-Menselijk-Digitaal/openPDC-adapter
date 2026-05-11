FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenPdcAdapter.slnx .
COPY src/OpenPdc.Client/OpenPdc.Client.csproj           src/OpenPdc.Client/
COPY src/OpenObjects.Client/OpenObjects.Client.csproj   src/OpenObjects.Client/
COPY src/OpenPdc.Adapter/OpenPdc.Adapter.csproj         src/OpenPdc.Adapter/
COPY src/OpenPdc.Sample/OpenPdc.Sample.csproj           src/OpenPdc.Sample/
RUN dotnet restore src/OpenPdc.Sample/OpenPdc.Sample.csproj

COPY src/ src/
RUN dotnet publish src/OpenPdc.Sample/OpenPdc.Sample.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenPdc.Sample.dll"]
