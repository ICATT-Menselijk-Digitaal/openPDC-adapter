# openPDC-adapter
openPDC-adapter for retrieving items from a "Products and Services catalog" (Producten en Diensten Catalogus) and syncing them into the Open Object register.

This adapter is a generic standalone application, developed for the municiplaity Rheden to make their Products and Services catalog directly avalibale in KISS (https://www.kiss-klantcontact.nl/).
The openPDC-adapter is developed and tested with a openPDC WorkdPress plugin
<img width="800px"   alt="KISS Rheden Context Diagram" src="https://github.com/user-attachments/assets/3e18edb7-d584-4a8e-88b1-9a87255e104a" />
 
---

## How it works

1. **Read** — streams all PDC items from the openPDC WordPress REST API (handles pagination automatically)
2. **Map** — converts each item to an SDG Kennisartikel object matching the [kennisartikel schema](https://github.com/open-objecten/objecttypes/blob/main/community-concepts/PDC%20-%20kennisartikel/kennisartikel-schema.json)
3. **Delete** - DELETEs the existing Kennisartikel objects in OpenObjects 
4. **Write** — POSTs each mapped object to the Open Objects API

## Prerequisites

| Requirement | Version |
|---|---|
| openPDC WordPress instance | accessible over HTTP |
| [Open Objects API](https://github.com/maykinmedia/objects-api) | running and configured with the 'Kennisartikel' object type |

### Configuration reference

All values can also be set as real environment variables or in `appsettings.json`. Environment variables take precedence.

| Key | Description | Required |
|---|---|---|
| `OpenPdc__BaseUrl` | Base URL of the openPDC WordPress REST API | **Yes** |
| `OpenPdc__ItemBaseUrl` | Base URL used to build per-item URLs in the mapped object, without trailing slash | **Yes** |
| `OpenObjects__Token` | API token for `Authorization: Token <value>` | **Yes** |
| `OpenObjects__ObjectTypeUrl` | URL of the registered object type — e.g. `http://host/api/v2/objecttypes/<uuid>` | **Yes** |
| `OpenObjects__ObjectTypeVersion` | Version number of the object type — e.g. `1` | **Yes** |
| `OpenObjects__OwmsUrl` | URL of the responsible organisation | **Yes** |
| `OpenObjects__OwmsIdentifier` | OWMS identifier URI of the organisation | **Yes** |
| `OpenObjects__OwmsEndDate` | OWMS end date (ISO 8601) | No (defaults to `2099-12-31`) |

### Constant field values

The following fields in the mapped OopenObjects Kennisartikelen type are hardcoded. The issue is addressed: https://github.com/ICATT-Menselijk-Digitaal/openPDC-adapter/issues/12

| Field | Value | Notes |
|---|---|---|
| `uuid` | `00000000-0000-0000-0000-000000000000` | Assigned by the API on creation; a zero UUID is sent as placeholder |
| `upnUri` | `"unknown"` | Not available in the openPDC source data |
| `productAanwezig` | `true` | Defaults to true |
| `doelgroep` | `"eu-burger"` | Fixed target audience |

## Running

```bash
dotnet run --project src/OpenPdc.Worker/OpenPdc.Worker.csproj
```
