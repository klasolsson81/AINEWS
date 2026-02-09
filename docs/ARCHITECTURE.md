# Arkitekturdokumentation — NewsRoom AI

## Oversikt

NewsRoom AI foljer en modular monolitarkitektur med kobaserade bakgrundsarbetare. Systemet ar designat for utbytbarhet — varje extern tjanst ligger bakom ett interface som kan bytas utan kodandringar.

## Arkitekturprinciper

### SOLID
- **Single Responsibility:** Varje worker hanterar en typ av jobb
- **Open/Closed:** Ny provider = nytt interface-implementation, ingen befintlig kod andras
- **Liskov Substitution:** MockTtsProvider och ElevenLabsTtsProvider ar utbytbara
- **Interface Segregation:** ITtsProvider har bara TTS-metoder
- **Dependency Inversion:** Alla services tar emot interfaces via constructor injection

### Provider-monster
Varje extern tjanst kan bytas genom att andra EN miljovariabel:
```
TTS_PROVIDER=mock         -> MockTtsProvider
TTS_PROVIDER=elevenlabs   -> ElevenLabsTtsProvider
TTS_PROVIDER=azure        -> AzureTtsProvider
```

## Systemflode

### Orkestreringsflode (Saga Pattern)

```
1. Anvandaren skickar BroadcastRequest
2. BroadcastOrchestrator skapar BroadcastJob
3. NewsService hamtar artiklar (RSS + GNews)
4. ScriptGenerator skapar manus (GPT-4o)
5. Parallellt:
   a. TTS Worker genererar ljud per segment
   b. B-Roll Worker hamtar/genererar visuellt material
6. Avatar Worker genererar ankarvideo (kraver TTS forst)
7. Composition Worker monterar slutlig video (Remotion)
8. SignalR pushar status till frontend
```

### Meddelandeflode (RabbitMQ)

```
Koer:
+-- tts-generation      -> TTS Worker
+-- avatar-generation   -> Avatar Worker
+-- broll-generation    -> B-Roll Worker
+-- video-composition   -> Composition Worker
+-- broadcast-status    -> Status Hub
```

## Interface-arkitektur

```
INewsSource              -> Hamtar nyheter (RSS, GNews, mock)
IEditorialImageExtractor -> Hamtar OG-bilder fran artikelsidor
IScriptGenerator         -> Genererar nyhetsmanus (GPT-4o, Claude, mock)
ITtsProvider             -> Text-till-tal (ElevenLabs, Azure, mock)
IAvatarGenerator         -> Ankarvideo (HeyGen, D-ID, mock)
IBRollProvider           -> Bildkalla per typ
IBRollOrchestrator       -> Koordinerar visuell strategi per nyhet
IMapGenerator            -> Kartbilder/animationer
IDataGraphicGenerator    -> Diagram och grafer
IVideoComposer           -> Slutlig videomontering (Remotion, mock)
IStorageProvider         -> Fillagring (lokal, Azure Blob)
IBroadcastOrchestrator   -> Orkestreringslogik (saga)
IBroadcastRepository     -> Databasatkomst
IMessagePublisher        -> Meddelandekopublicering
```

## Visuell strategi (5 nivaer)

| Niva | Kalla | Anvandning |
|------|-------|------------|
| 1 | Redaktionella artikelbilder (OG-image) | Namngivna personer, specifika platser |
| 2 | Kartor & datagrafik (programmatiskt) | Vader, ekonomi, val, geopolitik |
| 3 | Stockmaterial (Pexels) | Sjukvard, utbildning, generella samhallsnyheter |
| 4 | AI-genererade illustrationer | Teknik, AI, cyber, abstrakta amnen |
| 5 | Kombinerat (mix av ovan) | De flesta nyheter |

## Datamodell

```
BroadcastJob
+-- Id, Status, ProgressPercent
+-- BroadcastRequest (filter)
+-- BroadcastScript (genererat manus)
|   +-- Intro
|   +-- Segments[] (5-10 nyheter)
|   |   +-- AnchorIntro
|   |   +-- BRollVoiceover
|   |   +-- VisualContent -> Scenes[]
|   |   +-- AnchorOutro
|   |   +-- LowerThird
|   +-- Outro
+-- GeneratedAssets[]
+-- OutputVideoPath
```

## Cachningsstrategi

| Data | TTL | Nyckel |
|------|-----|--------|
| RSS-svar | 15 min | URL-hash |
| Genererade manus | Permanent | Content hash |
| TTS-ljud | Permanent | Text hash |
| Avatar-video | Permanent | Audio hash |
| B-roll | Permanent | Sokterm hash |
| Remotion renders | Permanent | Composition hash |

## Tekniska beslut

| Beslut | Val | Motivation |
|--------|-----|-----------|
| Backend-sprak | C# / .NET 8 | Befintlig kompetens, stark typing |
| Frontend | Next.js + TypeScript | SSR, App Router, Remotion-integration |
| Kosystem | RabbitMQ | Gratis, Docker-vanlig, mogen |
| Databas | PostgreSQL | Gratis, palitlig, JSON-stod |
| Videokomposition | Remotion | React-baserat, gratis for individer |
| Avatarer (dev) | D-ID | Billigast ($18/man) |
| Avatarer (prod) | HeyGen | Basta kvalitet/pris-balans |
| TTS | ElevenLabs | Basta svenska rostkvalitet |
| LLM | OpenAI GPT-4o | Billigt (~$0.05/manus), hog kvalitet |
