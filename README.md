# property-service

> Mikrostoritev za upravljanje nepremiènin (property) in izpostavitev API-ja prek REST in **GraphQL (HotChocolate)**.

---

## Odgovornosti

`property-service` izpostavlja **GraphQL API** za upravljanje nepremiènin ter podpira iskanje/filtriranje podatkov.  
Podatke hrani v **PostgreSQL (Supabase)** in uporablja **Supabase Storage** za delo z datotekami (npr. slike).

Glavne odgovornosti zajemajo:
- Upravljanje nepremiènin (CRUD)
- Iskanje, filtriranje in paginacija rezultatov
- Upravljanje povezanih entitet (npr. slike, oprema)
- Integracija s Supabase Storage (upload/serve datotek, po potrebi)

---

## Tehnološki sklad

- **.NET / ASP.NET Core**
- **GraphQL** (HotChocolate)
- **Entity Framework Core** + **Npgsql**
- **PostgreSQL** (Supabase)
- **Supabase Storage**
- **Serilog** (JSON logi) *(èe je v projektu vkljuèen)*
- **HealthChecks** (liveness/readiness) *(èe je v projektu vkljuèen)*
- **Prometheus** (prometheus-net) *(èe je v projektu vkljuèen)*

---

## API

### GraphQL
Dostopno na (`/graphql`), prav tkao tudi UI

### Swagger
- Swagger UI: `/swagger`
- OpenAPI JSON: `/swagger/v1/swagger.json`

### Model napak
- Kjer je smiselno, servis uporablja standardni ASP.NET Core `ProblemDetails`.

---

## Konfiguracija

Servis uporablja **Options pattern** (`IOptions<T>`) in lahko ob zagonu validira nastavitve (`ValidateOnStart`, data annotations, dodatne validacije). Èe je validacija omogoèena, se servis ob napaèni konfiguraciji **ne zažene**.

### Nastavitve (appsettings)

> Pri env var se `:` zamenja z `__` (npr. `SupabaseStorage__Url`).

#### ConnectionStrings
- `ConnectionStrings:Supabase` — connection string do PostgreSQL baze (Supabase).

#### SupabaseStorage
- `SupabaseStorage:Url` — URL do Supabase projekta.
- `SupabaseStorage:ServiceRoleKey` — Service Role za storage v Supabase.
- `SupabaseStorage:StorageBucket` — ime bucket-a v Supabase storage (npr. `property-images`).

#### Logging
- `Logging:LogLevel:Default` — privzeti nivo logiranja.
- `Logging:LogLevel:Microsoft.AspNetCore` — nivo logiranja za ASP.NET Core.

#### SwaggerPrefix
- `SwaggerPrefix` — javna predpona (npr. /property) za pravilne Swagger URL-je. 

#### Hosting
- `AllowedHosts` — dovoljeni hosti (pogosto `*`).

---
