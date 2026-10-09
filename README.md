# openPDC-adapter

> [!WARNING]
> **This repository is no longer used or maintained.** The openPDC and Smoelenboek adapters have moved to [Rheden-Adapters](https://github.com/ICATT-Menselijk-Digitaal/Rheden-Adapters), together with the Rx.Enterprise adapter. Open new issues and pull requests there.
>
> | Adapter | New location |
> | --- | --- |
> | openPDC | [`openPDC/`](https://github.com/ICATT-Menselijk-Digitaal/Rheden-Adapters/tree/main/openPDC) |
> | Smoelenboek | [`Smoelenboek/`](https://github.com/ICATT-Menselijk-Digitaal/Rheden-Adapters/tree/main/Smoelenboek) |
>
> The image and Helm chart addresses (`ghcr.io/icatt-menselijk-digitaal/openpdc-adapter`, `ghcr.io/icatt-menselijk-digitaal/smoelenboek-adapter` and the charts under `ghcr.io/icatt-menselijk-digitaal/charts/`) stay the same. Versions `1.7.0` and later are built from Rheden-Adapters; `1.6.0` was the last release from this repository.
>

This repository contains standalone adapters that sync external data sources into the Open Object register. They're developed for the municipality Rheden to make data directly available in [KISS](https://github.com/Klantinteractie-Servicesysteem), a Dutch local government open source project, as part of the [Association of Netherlands Municipalities](https://vng.nl/artikelen/about-the-vng) (VNG) [Common Ground framework](https://commonground.nl/).

## Adapters

| Adapter | Description |
|---|---|
| [OpenPdc adapter](src/OpenPdc/OpenPdc.Worker/README.md) | Syncs a WordPress-based Products and Services catalog (Producten en Diensten Catalogus) into Open Objects as SDG Kennisartikelen |
| [Smoelenboek adapter](src/Smoelenboek/Smoelenboek.Worker/README.md) | Syncs employee ("medewerker") data from Microsoft Entra ID into Open Objects as Medewerker objects |

Each adapter has its own README covering how it works, prerequisites, configuration reference, and running instructions.

## Running Open Objects with Docker

Both adapters sync into the same [Open Objects API](https://github.com/maykinmedia/objects-api).
Download the docker-compose.yaml, but before running the installationscripts do the following steps:

1- Create a `docker/postgres.entrypoint-initdb.d/` directory **in the same directory as your `docker-compose.yml`** and populate it with the DB initialisation scripts from:

> https://github.com/maykinmedia/open-object/tree/master/docker/postgres.entrypoint-initdb.d

2- Create a `docker/setup_configuration/` directory **in the same directory as your `docker-compose.yml`** and populate it with the DB initialisation scripts from:

> https://github.com/maykinmedia/open-object/tree/master/docker/setup_configuration

Then to run Open Objects via `docker-compose`,
3- Run docker compose: `docker compose up -d --no-build`

4- For loading demo data, run: `docker compose exec web src/manage.py loaddata demodata`

5- For creating user in admin portal, run: `docker compose exec web src/manage.py createsuperuser` and follow the steps
