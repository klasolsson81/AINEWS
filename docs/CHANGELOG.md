# Changelog — NewsRoom AI

Alla viktiga andringar i projektet dokumenteras har.

Format: [YYYY-MM-DD] — Beskrivning

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
