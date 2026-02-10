# Changelog — NewsRoom AI

Alla viktiga andringar i projektet dokumenteras har.

Format: [YYYY-MM-DD] — Beskrivning

---

## [2026-02-10] — FAS 3: TTS + Workers + Stockmaterial

### Tillagt
- **ElevenLabsTtsProvider** — Riktig TTS via ElevenLabs API
  - Multilingual v2-modell for svensk rost
  - Ton-baserade rostinstallningar (serious, warm, light, transitional)
  - SHA256-baserad caching av genererat ljud
  - Konfigurerbar rost-ID och lagringssokvag
- **PexelsBRollProvider** — Royalty-free stockmaterial via Pexels API
  - Foto- och videosokning med landscape-orientering
  - Videoupplosningsval (narmare 1920px valjs)
  - Fotograf-attribution ("Foto: namn / Pexels")
  - Fallback fran video till foto vid tomma resultat
  - URL-baserad filcaching
- **RabbitMQ Worker Services** — 4 bakgrundsworkers
  - `TtsWorker` — Konsumerar tts-generation-kon
  - `AvatarWorker` — Konsumerar avatar-generation-kon
  - `BRollWorker` — Konsumerar broll-generation-kon
  - `CompositionWorker` — Konsumerar video-composition-kon
  - `RabbitMqConnection` — Singleton med automatisk ateranslutning
  - Alla workers: manuell ack/nack, scopad DI, strukturerad loggning
- **Workers Program.cs** — Komplett worker-host med Serilog
- **Provider-switching utokat**
  - TTS_PROVIDER=elevenlabs|mock
  - BROLL_VIDEO_PROVIDER=pexels|mock
- 31 nya enhetstester (52 totalt, alla grona)

---

## [2026-02-09] — FAS 2: Nyheter + Manus + Frontend

### Tillagt
- **Next.js Frontend** med vardagsrums-TV-koncept
  - TVScreen-komponent med 4 tillstånd (standby, generating, playing, error)
  - FilterPanel: tidsperiod, kategorier, antal nyheter, generera-knapp
  - StatusBar: steg-för-steg-progress med visuella pipeline-indikatorer
  - API-klient mot backend REST-endpoints
  - SignalR-klient för realtidsuppdateringar
  - Mörkt tema med TV-ambient-glow-effekter
- **RssNewsSource** — Hämtar nyheter från SVT, DN, Expressen, SR
  - RSS/Atom-parsing med System.ServiceModel.Syndication
  - Kategori-mappning baserad på URL och RSS-kategorier
  - Tidsfiltrering och deduplicering
- **OgImageExtractor** — Hämtar redaktionella bilder från artikelsidor
  - Fallback-kedja: og:image → twitter:image → första <img>
  - HtmlAgilityPack för HTML-parsing
- **OpenAiScriptGenerator** — GPT-4o manusgenerering
  - Svensk nyhetsredaktör-systemprompt med visuell beslutsmatris
  - JSON-response-format med robust parsing
  - Fullständigt manusschema med visual_content per segment
- **Provider-switching** via konfiguration
  - NEWS_PROVIDER=rss|mock
  - LLM_PROVIDER=openai|mock
  - EDITORIAL_IMAGE_ENABLED=true|false
- 8 nya enhetstester (21 totalt, alla gröna)

---

## [2026-02-09] — Projektstart

### Tillagt
- Initial projektstruktur med ASP.NET Core 8 solution
  - `NewsRoom.Api` — Web API med SignalR
  - `NewsRoom.Core` — Domanmodeller, interfaces, DTOs, enums
  - `NewsRoom.Infrastructure` — Mock-implementationer
  - `NewsRoom.Workers` — Background workers
  - `NewsRoom.Tests` — xUnit-tester
- Docker Compose med PostgreSQL 16 + RabbitMQ 3 (management)
- Komplett interface-arkitektur:
  - `INewsSource`, `IEditorialImageExtractor`, `IScriptGenerator`
  - `ITtsProvider`, `IAvatarGenerator`, `IBRollProvider`
  - `IBRollOrchestrator`, `IMapGenerator`, `IDataGraphicGenerator`
  - `IVideoComposer`, `IStorageProvider`, `IBroadcastOrchestrator`
  - `IBroadcastRepository`, `IMessagePublisher`
- Domanmodeller: `BroadcastJob`, `BroadcastScript`, `NewsArticle`, `GeneratedAsset`
- Enums: `NewsCategory`, `BroadcastStatus`, `VisualContentType`, `VisualStrategy`
- DTOs: `BroadcastRequestDto`, `BroadcastStatusDto`, `NewsArticleDto`
- Custom exceptions: `NewsRoomException` med specifika subklasser
- Queue-meddelanden: `TtsGenerationMessage`, `AvatarGenerationMessage`, etc.
- Projektdokumentation: README, ARCHITECTURE, API-GUIDE, CHANGELOG, DEV-DIARY
- `.env.example` med alla konfigurationsnycklar
- `.gitignore` for .NET, Node.js, och genererade filer
- Anslutning till GitHub: https://github.com/klasolsson81/AINEWS.git
