# Utvecklingslogg — NewsRoom AI

Beslut, problem och lardomar dokumenteras har kronologiskt.

---

## 2026-02-09 — Dag 1: Projektuppstart

### Beslut

**Arkitektur: Modular monolit med kobaserade workers**
- Valde modular monolit framfor mikrotjanster — enklare att utveckla och deploya som ensam utvecklare
- RabbitMQ for asynkron kommunikation mellan orchestrator och workers
- Saga-monster i BroadcastOrchestrator for att hantera det komplexa flodet

**Net 8.0 som initialt target framework (senare uppgraderat till net10.0)**
- Startade med net8.0 enligt CLAUDE.md spec
- Uppgraderades till net10.0 da maskinen saknar ASP.NET Core 8.0 runtime
- Se dag 1 forts nedan

**Interface-first design**
- Alla 14 interfaces definierade innan implementation
- Mock-providers som default — mojliggor end-to-end-testning utan API-kostnader
- Provider-switching via en enda miljovariabel per tjanst

**5-niva visuell strategi**
- Kritiskt beslut: AI-genererade bilder av verkliga personer/platser blir missvisande
- Losning: OG-bilder fran artiklar (Niva 1) for verkliga nyheter, AI-bilder bara for abstrakta amnen
- Pexels som gratis stockmaterial-fallback

### Lardomar
- .NET 10 SDK pa maskinen skapar net10.0-projekt som default — maste explicit ange `--framework net8.0` eller manuellt andra csproj
- NuGet-paketversioner maste matcha target framework — Npgsql 8.0.x for net8.0

### Nasta steg
- ~~Implementera mock-providers for alla interfaces~~ KLART
- ~~Satta upp BroadcastOrchestrator med saga-logik~~ KLART
- ~~Bygga API-controllers och SignalR hub~~ KLART
- ~~Skapa grundlaggande frontend med Next.js~~ KLART

---

## 2026-02-09 — Dag 1 (forts): FAS 2 + Frontend parallellt

### Beslut

**Target framework: net10.0 (inte net8.0)**
- Maskinen har bara ASP.NET Core 10.0 runtime — inte 8.0
- Alla 5 projekt uppgraderade till net10.0 med matchande NuGet-paket
- Fungerar utmarkt — inga kompatibilitetsproblem

**RSS-kallor: SVT, SR, DN, Expressen**
- Fyra svenska RSS-floden ger bra tackning av nyhetslandskapet
- Kategori-mappning baserat pa bade RSS-kategorier och URL-monster
- Deduplicering via normaliserad titel (lowercased, trimmed)

**OpenAI GPT-4o for manusgenerering (skarpt fran start)**
- Foljde CLAUDE.md:s instruktion: "Anvand OpenAI API skarpt under dev"
- Kostnad ~$0.03-0.10 per manus — forsumbart
- System prompt som svensk nyhetsredaktor med visuell beslutsmatris
- JSON response_format for strukturerad output

**OG-bild-extraktion med HtmlAgilityPack**
- Fallback-kedja: og:image -> twitter:image -> forsta <img>
- Ger tillgang till redaktionella bilder utan upphovsrattsproblem
- Konfigureras via EDITORIAL_IMAGE_ENABLED

**Frontend: Next.js 14 med TV-koncept**
- Morkt tema med TV-ambient-glow-effekter
- 4 tillstand: standby, generating, playing, error
- Polling-baserad statusuppdatering (SignalR forberett men polling som fallback)
- FilterPanel med tidsperiod, kategorier, antal nyheter

**Provider-switching via IConfiguration**
- ServiceRegistration laser fran bade IConfiguration och Environment.GetEnvironmentVariable
- Fallback-kedja: appsettings.json -> .env -> hardkodad default ("mock")
- NEWS_PROVIDER=rss|mock, LLM_PROVIDER=openai|mock, EDITORIAL_IMAGE_ENABLED=true|false

### Lardomar
- FluentAssertions 8.x brot API: `HaveCountLessOrEqualTo` -> `HaveCountLessThanOrEqualTo`
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2 finns inte — senaste ar 10.0.0
- System.ServiceModel.Syndication fungerar bra for RSS/Atom-parsing i net10.0
- Serilog.AspNetCore 9.0.0 ar kompatibelt med net10.0

### Testresultat
- 21 tester totalt, alla grona
- Nya tester: InMemoryBroadcastRepository (5 st), MockTtsProvider (3 st)

### Nasta steg
- FAS 3: ElevenLabs TTS-integration
- FAS 4: Remotion-templates for nyhetssandning
- Worker Services implementation
- End-to-end-test med mock-video
