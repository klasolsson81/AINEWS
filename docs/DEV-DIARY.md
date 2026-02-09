# Utvecklingslogg — NewsRoom AI

Beslut, problem och lardomar dokumenteras har kronologiskt.

---

## 2026-02-09 — Dag 1: Projektuppstart

### Beslut

**Arkitektur: Modular monolit med kobaserade workers**
- Valde modular monolit framfor mikrotjanster — enklare att utveckla och deploya som ensam utvecklare
- RabbitMQ for asynkron kommunikation mellan orchestrator och workers
- Saga-monster i BroadcastOrchestrator for att hantera det komplexa flodet

**Net 8.0 som target framework**
- Stabilt LTS-release
- Alla NuGet-paket har bra stod
- Produktionsredo

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
- Implementera mock-providers for alla interfaces
- Satta upp BroadcastOrchestrator med saga-logik
- Bygga API-controllers och SignalR hub
- Skapa grundlaggande frontend med Next.js
