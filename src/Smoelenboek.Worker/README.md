# Smoelenboek adapter

Smoelenboek adapter for syncing employee ("medewerker") data from Microsoft Entra ID into the Open Object register.

This adapter is a generic standalone application: the Microsoft Graph user query is customer-configurable, so it can be deployed for different municipalities/tenants without code changes. It's developed as part of the [Association of Netherlands Municipalities](https://vng.nl/artikelen/about-the-vng) (VNG) [Common Ground framework](https://commonground.nl/).

The Smoelenboek adapter fetches users from Microsoft Entra ID (via Microsoft Graph API) and syncs them as Medewerker objects into Open Objects.

---

## How it works

1. **Read** — fetches users from Microsoft Graph (`/v1.0/users`) using client-credentials auth, applying a configurable `$filter` (e.g. restricted to a domain and requiring a department) and selecting only the name/contact/department fields the sync needs
2. **Map** — converts each user to a Medewerker object matching the [medewerker schema](https://github.com/open-objecten/objecttypes/blob/main/community-concepts/Medewerker/medewerker-schema.json), resolving each Entra.user's `department` to a `naam` by matching against Groep objects already present in Open Objects
3. **Insert, delete** — for each user, DELETEs the existing Medewerker with the same `identificatie` (if any) and INSERTs a fresh one, then DELETEs any Medewerker objects left over that no longer have a matching Entra user

Groep objects themselves are **not** synced by this adapter — they're expected to already exist in Open Objects (entered manually) and are only read to resolve the `identificatie` reference on each Medewerker.

## Prerequisites

| Requirement | Version |
|---|---|
| Microsoft Entra app registration | client-credentials flow, with the Microsoft Graph **application** permission `User.Read.All` granted with admin consent |
| [Open Objects API](https://github.com/maykinmedia/objects-api) | running and configured with the 'Medewerker' and 'Groep' object types |

See the main [README](README.md#running-open-objects-with-docker) for instructions on running Open Objects locally via `docker-compose`.

## Configuration reference

All values can be set as environment variables or in `appsettings.json`. Environment variables take precedence.

| Key | Description | Required |
|---|---|---|
| `Entra__TenantId` | Microsoft Entra tenant ID | **Yes** |
| `Entra__ClientId` | App registration (client) ID | **Yes** |
| `Entra__ClientSecret` | App registration client secret | **Yes** |
| `Entra__UsersFilter` | Microsoft Graph OData `$filter` expression applied to the `/users` query — customer-specific, e.g. `endsWith(userPrincipalName,'@example.nl') and not(department eq null)`. Graph treats an empty `$filter` as no filter, so leaving this unset fetches every user in the tenant, unfiltered. | No |
| `OpenObjects__BaseUrl` | Base URL of the Open Objects API | No (defaults to `http://localhost:8000`) |
| `OpenObjects__Token` | API token sent as `Authorization: Token <value>`. To obtain: in the Objects admin UI, create a **Permission** scoped to the Medewerker (and Groep, for read access) object types, then create a **Token Authorization** for that permission and copy the resulting token string here. | **Yes** |
| `OpenObjects__MedewerkerObjectTypeUrl` | URL of the Medewerker object type registered in the objecttypen API — e.g. `http://host/api/v2/objecttypes/<uuid>` | **Yes** |
| `OpenObjects__MedewerkerObjectTypeVersion` | Published version number of the Medewerker object type schema to validate against | No (defaults to `1`) |
| `OpenObjects__GroepObjectTypeUrl` | URL of the Groep object type — read-only, Groep objects are entered manually and never written by this adapter | **Yes** |
| `OpenObjects__GroepObjectTypeVersion` | Not currently used (Groep is only ever read, never written) | No |

## Running

```bash
dotnet run --project src/Smoelenboek.Worker/Smoelenboek.Worker.csproj
```
