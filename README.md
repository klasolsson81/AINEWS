# NewsRoom AI — AI-Genererad Svensk Nyhetssandning

> En fullstandigt automatiserad, on-demand AI-nyhetssandning pa svenska — med samma format och upplevelse som TV4 Nyheterna eller SVT Rapport.

## Koncept

Anvandaren oppnar appen och ser ett fotorealistiskt vardagsrum med en stor TV. Man valjer filter (tidsperiod, kategorier) och systemet genererar en komplett videobaserad nyhetssandning med AI-ankare, nyhetsklipp och professionell TV-produktion.

## Tech Stack

| Komponent | Teknologi |
|-----------|-----------|
| **Frontend** | Next.js 14+ (App Router) + TypeScript + Tailwind CSS |
| **Video** | Remotion (React-baserad videokomposition) |
| **Backend** | ASP.NET Core 8 Web API |
| **Realtid** | SignalR (jobbstatus till frontend) |
| **Kosystem** | RabbitMQ (Docker) |
| **Databas** | PostgreSQL (Docker) |
| **TTS** | ElevenLabs / Azure TTS |
| **Avatar** | HeyGen / D-ID |
| **LLM** | OpenAI GPT-4o (manusgenerering) |
| **B-roll** | Pexels (stock) + Flux 2 Pro (AI-bilder) |

## Arkitektur

```
Frontend (Next.js + Remotion)
         |
         | REST API + SignalR
         v
Backend (ASP.NET Core 8)
  +-- BroadcastOrchestrator (saga pattern)
  +-- NewsService (RSS + GNews)
  +-- ScriptGenerator (GPT-4o)
  |
  +-- TTS Worker -------- ElevenLabs
  +-- Avatar Worker ----- HeyGen/D-ID
  +-- B-Roll Worker ----- Pexels/Flux
  +-- Composition Worker - Remotion CLI
```

## Snabbstart

### Forutsattningar

- .NET 8 SDK
- Node.js 20+
- Docker Desktop
- ffmpeg (for Remotion)

### Starta utvecklingsmiljo

```bash
# 1. Klona repot
git clone https://github.com/klasolsson81/AINEWS.git
cd AINEWS

# 2. Kopiera miljovariabler
cp .env.example .env
# Redigera .env med dina API-nycklar

# 3. Starta infrastruktur
docker-compose up -d

# 4. Backend
cd src/backend/NewsRoom.Api
dotnet run

# 5. Frontend (ny terminal)
cd src/frontend/newsroom-web
npm install
npm run dev
```

### Endpoints

- **Frontend:** http://localhost:3000
- **Backend API:** https://localhost:5001
- **RabbitMQ Management:** http://localhost:15672 (newsroom/newsroom_dev_2026)

## Projektstruktur

```
AINEWS/
+-- CLAUDE.md                    # AI-instruktioner & arkitekturdok
+-- docker-compose.yml           # PostgreSQL + RabbitMQ
+-- .env.example                 # Miljovariabel-mall
+-- docs/
|   +-- ai-nyhetsrapport.docx    # Teknisk feasibility-rapport
|   +-- ARCHITECTURE.md          # Arkitekturbeslut
|   +-- API-GUIDE.md             # API-dokumentation
|   +-- CHANGELOG.md             # Andringslogg
|   +-- DEV-DIARY.md             # Utvecklingslogg
+-- src/
|   +-- backend/
|   |   +-- NewsRoom.slnx
|   |       +-- NewsRoom.Api/            # Web API
|   |       +-- NewsRoom.Core/           # Domanmodeller & interfaces
|   |       +-- NewsRoom.Infrastructure/ # Implementationer
|   |       +-- NewsRoom.Workers/        # Background workers
|   |       +-- NewsRoom.Tests/          # Tester
|   +-- frontend/
|       +-- newsroom-web/                # Next.js app
+-- mock-data/                           # Testdata
```

## Utvecklingsfaser

- [x] **FAS 1:** Grundarkitektur + Mocks
- [ ] **FAS 2:** Nyheter + Manus (GPT-4o)
- [ ] **FAS 3:** TTS (ElevenLabs)
- [ ] **FAS 4:** Remotion-templates
- [ ] **FAS 5:** Avatar-integration
- [ ] **FAS 6:** Visuell pipeline (5-niva strategi)
- [ ] **FAS 7:** UI-finish (Vardagsrums-TV)
- [ ] **FAS 8:** Polish & Deploy

## Licens

Personligt portfolio-projekt. Ej for kommersiell distribution.

---

*AI-genererad nyhetssandning — byggt med passion och moderna AI-verktyg.*
