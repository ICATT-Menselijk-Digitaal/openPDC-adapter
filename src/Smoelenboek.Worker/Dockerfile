FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenPdcAdapter.slnx .
COPY src/Entra.Client/Entra.Client.csproj               src/Entra.Client/
COPY src/OpenObjects.Client/OpenObjects.Client.csproj   src/OpenObjects.Client/
COPY src/Smoelenboek.Adapter/Smoelenboek.Adapter.csproj src/Smoelenboek.Adapter/
COPY src/Smoelenboek.Worker/Smoelenboek.Worker.csproj   src/Smoelenboek.Worker/
RUN dotnet restore src/Smoelenboek.Worker/Smoelenboek.Worker.csproj

COPY src/ src/
RUN dotnet publish src/Smoelenboek.Worker/Smoelenboek.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Smoelenboek.Worker.dll"]
