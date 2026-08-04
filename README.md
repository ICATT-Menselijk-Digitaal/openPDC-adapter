# openPDC-adapter

This repository contains standalone adapters that sync external data sources into the Open Object register. They're developed for the municipality Rheden to make data directly available in [KISS](https://github.com/Klantinteractie-Servicesysteem), a Dutch local government open source project, as part of the [Association of Netherlands Municipalities](https://vng.nl/artikelen/about-the-vng) (VNG) [Common Ground framework](https://commonground.nl/).

## Adapters

| Adapter | Description |
|---|---|
| [OpenPdc adapter](src/OpenPdc.Worker/README.md) | Syncs a WordPress-based Products and Services catalog (Producten en Diensten Catalogus) into Open Objects as SDG Kennisartikelen |
| [Smoelenboek adapter](src/Smoelenboek.Worker/README.md) | Syncs employee ("medewerker") data from Microsoft Entra ID into Open Objects as Medewerker objects |

Each adapter has its own README covering how it works, prerequisites, configuration reference, and running instructions.

## Running Open Objects with Docker

Both adapters sync into the same [Open Objects API](https://github.com/maykinmedia/objects-api).

To run Open Objects via `docker-compose`,
1- Create a `docker/postgres.entrypoint-initdb.d/` directory **in the same directory as your `docker-compose.yml`** and populate it with the DB initialisation scripts from:

> https://github.com/maykinmedia/open-object/tree/master/docker/postgres.entrypoint-initdb.d

2- Create a `docker/setup_configuration/` directory **in the same directory as your `docker-compose.yml`** and populate it with the DB initialisation scripts from:

> https://github.com/maykinmedia/open-object/tree/master/docker/setup_configuration

3- Run docker compose: `docker compose up -d --no-build`

4- For loading demo data, run: `docker compose exec web src/manage.py loaddata demodata`

5- For creating user in admin portal, run: `docker compose exec web src/manage.py createsuperuser` and follow the steps
