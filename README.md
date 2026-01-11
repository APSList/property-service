# property-service

> Mikrostoritev za upravljanje nepremičnin (property) in izpostavitev API-ja prek REST in **GraphQL (HotChocolate)**.
Storitev je dostopona na https://hostflow.software/property/swagger/index.html
---

## Odgovornosti

`property-service` izpostavlja **GraphQL API** za upravljanje nepremičnin ter podpira iskanje/filtriranje podatkov.  
Podatke hrani v **PostgreSQL (Supabase)** in uporablja **Supabase Storage** za delo z datotekami (npr. slike).

Glavne odgovornosti zajemajo:
- Upravljanje nepremičnin (CRUD)
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
- **Serilog** (JSON logi) *(če je v projektu vključen)*
- **HealthChecks** (liveness/readiness) *(če je v projektu vključen)*
- **Prometheus** (prometheus-net) *(če je v projektu vključen)*

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

Servis uporablja **Options pattern** (`IOptions<T>`) in lahko ob zagonu validira nastavitve (`ValidateOnStart`, data annotations, dodatne validacije). Če je validacija omogočena, se servis ob napačni konfiguraciji **ne zažene**.

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

## CI/CD in pravila razvoja

### Pregled
CI/CD je sestavljen iz dveh delov:
1. **Service repo (ta repo)**: build/test + izdelava in push Docker image-a.
2. **Deployment repo (npr. `APSList/Hostflow`)**: Helm chart + `values*` kot “source of truth” za deploy v Kubernetes.

---

### GitHub Actions workflowi

#### PR validacija (`pr.yaml`)
- **Trigger**: PR → `main`
- **Koraki**: restore → build → test
- **Pravila**: naslov PR mora slediti “conventional” prefiksom:
  - `feat:`, `fix:`, `chore:`, `docs:`, `style:`, `refactor:`, `perf:`, `test:`, `ci:`

#### DEV CI/CD (`dev.yaml`)
- **Trigger**: `push` → `dev`
- **Koraki**:
  1) restore/build/test  
  2) build Docker image  
  3) push image v registry z tagom **kratkega SHA** (`${GITHUB_SHA::7}`)  
  4) checkout deployment repota (`APSList/Hostflow`, veja `dev`)  
  5) `helm upgrade --install` za **DEV** okolje (nastavi `image.tag` na kratek SHA)

#### Release PR (`release-please.yaml`)
- **Trigger**: `push` → `main`
- **Namen**: `release-please` pripravi/posodobi **release PR** (changelog + bump verzije) na podlagi conventional sprememb.

#### PROD release (`release.yaml`)
- **Trigger**: `git tag vX.Y.Z` (npr. `v1.2.3`)
- **Koraki**:
  1) restore/build/test  
  2) build + push Docker image z tagom **verzije** (`vX.Y.Z`)  
  3) checkout deployment repota (`APSList/Hostflow`, privzeta veja)  
  4) `helm upgrade --install` za **PROD** okolje (nastavi `image.tag` na `vX.Y.Z`)

---

### Deploy model (service repo → deployment repo)

1. **Ta repo** zgradi artefakt:
   - Docker image se zgradi iz trenutnega commita.
   - Image se pushne v registry (DockerHub/registry).

2. **Deployment repo** definira, *kako* in *kam* se deploya:
   - Helm chart + `values.yaml` (in pogosto `values-dev.yaml`/`values-prod.yaml`) so v deployment repotu.
   - Deployment repo je “source of truth” za:
     - namespace, ingress, replicas, resources
     - env var/secret reference (DB, storage, itd.)
     - health probes, autoscaling, service/ports

3. **Helm deploy**:
   - Pipeline naredi `helm upgrade --install` in ob tem nastavi vsaj:
     - `image.repository`
     - `image.tag` (DEV = kratek SHA, PROD = verzija)

---

