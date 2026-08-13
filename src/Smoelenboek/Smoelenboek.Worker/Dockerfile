FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenPdcAdapter.slnx .
COPY src/Smoelenboek/Entra.Client/Entra.Client.csproj             src/Smoelenboek/Entra.Client/
COPY src/Shared/OpenObjects.Client/OpenObjects.Client.csproj       src/Shared/OpenObjects.Client/
COPY src/Smoelenboek/Smoelenboek.Adapter/Smoelenboek.Adapter.csproj src/Smoelenboek/Smoelenboek.Adapter/
COPY src/Smoelenboek/Smoelenboek.Worker/Smoelenboek.Worker.csproj   src/Smoelenboek/Smoelenboek.Worker/
RUN dotnet restore src/Smoelenboek/Smoelenboek.Worker/Smoelenboek.Worker.csproj

COPY src/ src/
RUN dotnet publish src/Smoelenboek/Smoelenboek.Worker/Smoelenboek.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Smoelenboek.Worker.dll"]
