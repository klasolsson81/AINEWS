# CLAUDE.md — AI-Nyhetssändning (Codename: "NewsRoom AI")

> **Detta dokument är den enda sanningskällan för projektet.**
> Läs HELA detta dokument innan du skriver en enda rad kod.
> Referera även till `docs/ai-nyhetsrapport.docx` för teknisk research om API:er, kostnader och plattformsjämförelser.

---

## 📌 PROJEKT-OVERVIEW

### Vad vi bygger
En fullständigt automatiserad, on-demand AI-nyhetssändning på svenska — med samma format och upplevelse som TV4 Nyheterna eller SVT Rapport. Användaren väljer filter (tidsperiod, kategorier) och systemet genererar en komplett videobaserad nyhetssändning med AI-ankare, nyhetsklipp och professionell TV-produktion.

### Vem det är för
- Personligt bruk och portfolio-demonstration
- LinkedIn och LIA-ansökningar
- INTE kommersiell distribution

### Kärnupplevelse
1. Användaren öppnar appen och ser ett **fotorealistiskt vardagsrum med en stor TV**
2. Användaren väljer filter: tidsperiod (senaste 6h/12h/24h) och kategorier (Inrikes, Utrikes, Sport, Politik, Nöje, Ekonomi, Teknik)
3. Användaren klickar "Generera sändning"
4. Realtidsstatus visas medan sändningen genereras
5. Den färdiga videon spelas upp **inne i TV:n i vardagsrummet** — som att titta på riktiga nyheter

---

## 🎬 KRAV: VIDEOFORMAT & LÄNGD

### KRITISKT — Minimikrav på videolängd
```
REGEL: Varje komplett nyhetssändning MÅSTE vara minst 5 minuter lång.
MÅL: 7-10 minuter är idealt.
ALDRIG: Nyhetsklipp på under 20 sekunder. 8 sekunder fungerar INTE.
```

### Struktur för en sändning (mål: 7-10 min)
```
00:00 - 00:30  Intro (vinjett + ankarens välkomstfras)
00:30 - 02:00  Nyhet 1 — Huvudnyhet (ankare intro → klipp → ankare outro)
02:00 - 03:15  Nyhet 2 (ankare intro → klipp → ankare outro)
03:15 - 04:30  Nyhet 3 (ankare intro → klipp → ankare outro)
04:30 - 05:30  Nyhet 4 (ankare intro → klipp → ankare outro)
05:30 - 06:30  Nyhet 5 (ankare intro → klipp → ankare outro)
06:30 - 07:30  Nyhet 6 — Lättare nyhet/kultur/nöje
07:30 - 08:30  Nyhet 7 — Sport eller väder
08:30 - 09:30  Sammanfattning/avslut av ankare
09:30 - 10:00  Outro (vinjett + grafik)
```

### Regler för nyhetsklipp (B-roll-segment)
```
REGEL: Om en AI-videomodell max genererar 8-10 sekunder per klipp:
  → Generera FLERA klipp (3-5 st) per nyhet med olika bilder/vinklar
  → Kombinera dem till ett sammanhängande segment på 30-60 sekunder
  → Lägg ankarens voiceover ÖVER klippen (inte tystnad mellan klipp)
  → Använd mjuka övergångar (crossfade) mellan klippen

EXEMPEL för en nyhet om översvämning i Göteborg:
  Klipp 1 (8s): Översiktsbild av stad med regn
  Klipp 2 (8s): Översvämmad gata med bilar
  Klipp 3 (8s): Räddningstjänst i arbete
  Klipp 4 (8s): Ken Burns-zoom på AI-genererad bild av drabbat område
  = 32 sekunder B-roll med kontinuerlig voiceover
```

### Segment-timing per nyhet
```
Ankare intro:        10-15 sekunder (ankare på skärmen, presenterar nyheten)
B-roll med voiceover: 30-60 sekunder (klipp med ankarens röst över)
Ankare outro:         5-10 sekunder (ankare tillbaka, kort avslut/övergång)
TOTALT per nyhet:     45-85 sekunder
```

---

## 📝 MANUSET ÄR GRUNDEN FÖR ALLT

### Varför manuset är kritiskt
Manuset är inte bara "text som läses upp" — det är **styrdokumentet för hela produktionen**. Varje nedströms-komponent beror på manuset:

```
MANUS styr:
  → TTS: Vad som sägs, med vilken ton, pauser och betoning
  → Avatar: Hur lång ankarens video blir (baserat på ljudlängd)
  → B-roll: Vilka bilder/video som behövs (via visual_prompts)
  → Remotion: Hela tidslinjen, segmentordning, övergångar
  → Lower thirds: Källhänvisning, kategori, rubrik

Om manuset är fel → ALLT blir fel.
Om manuset är bra → resten är "bara" exekvering.
```

### Manusformat (strukturerad JSON)
Varje genererat manus MÅSTE följa detta exakta format. Detta är kontraktet som alla nedströms-tjänster förlitar sig på.

```json
{
  "broadcast_id": "uuid",
  "generated_at": "2026-02-08T19:00:00Z",
  "language": "sv-SE",
  "total_segments": 7,
  "estimated_duration_seconds": 480,
  "intro": {
    "anchor_text": "God kväll och välkommen till Nyhetskollen. Jag heter Anna Lindström. Ikväll tar vi en titt på de senaste händelserna.",
    "tone": "warm, professional, welcoming"
  },
  "segments": [
    {
      "segment_number": 1,
      "category": "inrikes",
      "headline": "Översvämningar i Västsverige",
      "source": "SVT Nyheter",
      "source_url": "https://svt.se/...",
      "priority": "top_story",

      "anchor_intro": {
        "text": "Vi inleder med väderkaoset i västra Sverige. Under det senaste dygnet har kraftiga skyfall orsakat stora översvämningar i Göteborgsregionen.",
        "tone": "serious, concerned",
        "estimated_seconds": 12
      },

      "broll_voiceover": {
        "text": "Räddningstjänsten har under natten fått in över tvåhundra larm om översvämningar. Flera vägar har stängts av och boende i låglänta områden har uppmanats att hålla sig inomhus. Enligt SMHI väntas regnet fortsätta under morgondagen med ytterligare femtio millimeter.",
        "tone": "serious, informative",
        "estimated_seconds": 35
      },

      "visual_content": {
        "type": "real_event",
        "visual_strategy": "editorial_images_with_maps",
        "scenes": [
          {
            "description": "Karta över Västsverige med markering av drabbade områden",
            "type": "generated_map",
            "duration_seconds": 8,
            "source_hint": "Generera karta med Göteborg markerat"
          },
          {
            "description": "Översvämmad gata i stadsmiljö",
            "type": "stock_footage",
            "search_terms": ["flooding urban street sweden", "översvämning stad"],
            "duration_seconds": 8
          },
          {
            "description": "Räddningstjänst i arbete vid översvämning",
            "type": "stock_footage",
            "search_terms": ["rescue team flooding", "räddningstjänst vatten"],
            "duration_seconds": 8
          },
          {
            "description": "Regnigt väder med mörka moln över stad",
            "type": "ai_generated_image",
            "prompt": "Heavy rain over Scandinavian city, dark storm clouds, wet streets, photojournalistic style, editorial photography",
            "duration_seconds": 8
          },
          {
            "description": "SMHI väderprognos-grafik",
            "type": "generated_graphic",
            "data_hint": "Regnmängder per dag, prognos 48h",
            "duration_seconds": 6
          }
        ]
      },

      "anchor_outro": {
        "text": "Vi följer utvecklingen i morgon.",
        "tone": "transitional",
        "estimated_seconds": 4
      },

      "lower_third": {
        "title": "ÖVERSVÄMNINGAR I VÄSTSVERIGE",
        "subtitle": "Över 200 larm till räddningstjänsten"
      }
    }
  ],
  "outro": {
    "anchor_text": "Det var allt för ikväll. Tack för att ni tittade. God natt.",
    "tone": "warm, closing"
  }
}
```

### LLM-prompten för manusgenerering
Manusprompen är projektets VIKTIGASTE prompt. Den måste producera:

```
KRAV PÅ LLM-PROMPT:
1. Korrekt JSON-format som matchar schemat ovan EXAKT
2. Naturlig svensk nyhetssvenska (inte översatt engelska)
3. Rätt ton per segment (allvarlig för krig, lättsam för kultur)
4. visual_content med RÄTT strategi per nyhetstyp (se visuell strategi nedan)
5. Realistiska tidsuppskattningar per segment
6. Totalt 5-10 segment som ger MINST 5 minuters sändning
7. Källhänvisning till ursprunglig nyhetsartikel
8. ALDRIG fabricerade fakta — bara omskrivning av verkliga nyheter
```

### Manusgenerering körs SKARPT under dev
```
REGEL: Använd OpenAI API (GPT-4o) för manusgenerering redan från FAS 1.
KOSTNAD: ~$0.03-0.10 per manus — försumbart.
ANLEDNING: Vi MÅSTE se att manus-kvaliteten är rätt tidigt.
           Allt annat bygger på att manuset är korrekt.

OpenAI API-nyckel finns redan tillgänglig.
LLM_PROVIDER ska sättas till "openai" som default under dev.
Mock-provider finns som fallback om API:et är nere.
```

---

## 🎥 VISUELL INNEHÅLLSSTRATEGI — DET KRITISKA BESLUTET

### Problemet
AI-genererade bilder av verkliga händelser och personer blir MISSVISANDE:
- En AI-genererad "Trump" ser inte ut som Trump
- En AI-genererad "riksdagsbyggnad" är inte Sveriges riksdag
- En AI-genererad "olycksplats" visar fel plats, fel fordon, fel detaljer
- Tittaren förlorar ALL trovärdighet om bilderna inte stämmer

### Kan vi använda riktiga nyhetsklipp?
```
SVAR: NEJ — inte direkt.
- SVT, TV4, DN etc. äger upphovsrätt till sina videoklipp
- Att kopiera och återanvända dem kräver licensavtal
- Även "fair use" (citaträtt) är extremt begränsad för video i Sverige

MEN: Det finns lagliga sätt att visa "riktigt" visuellt material.
```

### Lösning: 5-nivå visuell strategi
Manuset bestämmer visuell strategi PER NYHET baserat på nyhetstyp. Rätt strategi väljs automatiskt.

```
NIVÅ 1 — REDAKTIONELLA ARTIKELBILDER (bäst för namngivna personer/händelser)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Källa: Open Graph-bilder / thumbnail-bilder från nyhetsartiklarna
Hur:   RSS-flöden och nyhetsartiklar inkluderar nästan ALLTID en og:image
       eller thumbnail-URL som är den faktiska nyhetsbilden.
       Hämta bilden, visa med Ken Burns-effekt + voiceover.

Lagligt: Ja — bilden visas som del av rapportering om nyheten, med
         tydlig källhänvisning i lower third. Jämförbart med hur
         nyhetssajter visar bilder från andra källor med attribution.

Exempel: Nyhet om Trump → Hämta artikelns og:image (ett riktigt foto)
         → Visa bilden med långsam zoom + voiceover
         → Lower third: "Foto: Reuters via DN"

PERFEKT FÖR: Politik, internationella ledare, namngivna personer,
             specifika byggnader/platser, sportevenemang

IMPLEMENTATION:
  - Parsa <meta property="og:image"> från artikelns URL
  - Fallback: Parsa <img> från RSS <description> eller <enclosure>
  - Spara bild lokalt, använd i Remotion med Ken Burns


NIVÅ 2 — KARTOR & DATAGRAFIK (bäst för händelser med platsdata)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Källa: Programmatiskt genererade kartor och grafer
Hur:   Remotion React-komponenter som renderar kartor (Leaflet/Mapbox)
       och datavisualiseringar (Recharts/D3) direkt i videokomposition.

Lagligt: 100% — vi genererar allt själva.

Exempel: Översvämning i Göteborg → Karta med Göteborg markerat i rött
         Börsutveckling → Linjediagram med OMX Stockholm 30
         Valresultat → Stapeldiagram per parti

PERFEKT FÖR: Väder, naturkatastrofer, ekonomi/börs, val, geopolitik,
             brottstatistik, pandemidata, trafikolyckor

IMPLEMENTATION:
  - React-komponenter i Remotion som tar data-props
  - Animerade kartor med zoom till rätt region
  - Grafer med animerad uppritning
  - Kan kombineras med voiceover-timing


NIVÅ 3 — STOCKMATERIAL (bäst för generiska/tematiska bilder)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Källa: Pexels API, Pixabay (gratis, royalty-free)
Hur:   Manuset genererar söktermer, systemet hämtar relevanta
       stockbilder/videos.

Lagligt: 100% — Pexels/Pixabay-licens tillåter fri användning.

Exempel: Nyhet om sjukvård → Stockvideo av sjukhuskorridor
         Nyhet om utbildning → Stockbild av klassrum
         Nyhet om trafik → Stockvideo av motorväg

PERFEKT FÖR: Sjukvård, utbildning, miljö, energi, infrastruktur,
             generella samhällsnyheter utan specifika platser/personer

IMPLEMENTATION:
  - Pexels API-sökning med engelska nyckelord (bäst resultat)
  - Filtrera på video vs foto baserat på behov
  - Ken Burns-effekt på stillbilder, direkt uppspelning av video
  - Visa "Illustrationsbild" i lower third vid behov


NIVÅ 4 — AI-GENERERADE BILDER (bäst för abstrakta/konceptuella ämnen)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Källa: Flux 2 Pro, DALL-E, etc.
Hur:   Manuset inkluderar specifika bild-prompts som genererar
       illustrativa (INTE fotorealistiska nyhetsbilder) bilder.

Lagligt: 100% — vi skapar allt själva.

⚠️  KRITISK REGEL: AI-genererade bilder ska ALDRIG föreställa:
    - Namngivna verkliga personer
    - Specifika verkliga byggnader/platser
    - Specifika verkliga händelser (olyckor, bränder, etc.)

✅  AI-genererade bilder SKA användas för:
    - Konceptuella illustrationer (cybersäkerhet, AI, klimat)
    - Symboliska bilder (rättsklubba för juridik, jordglob för internationellt)
    - Bakgrunder och atmosfärbilder
    - "Mood shots" som sätter ton utan att påstå specifik plats

Exempel: Nyhet om cybersäkerhet → AI-bild av abstrakt digital säkerhet
         Nyhet om AI-utveckling → AI-bild av futuristisk teknologi
         Nyhet om klimat → AI-bild av smältande glaciär (generiskt)

IMPLEMENTATION:
  - Prompt-stil: "editorial illustration, conceptual, magazine style"
  - ALDRIG: "photo of [verklig person]" eller "photo of [verklig plats]"
  - Alla AI-bilder märks med "AI-genererad illustration" i metadata


NIVÅ 5 — KOMBINERAT (de flesta nyheter använder flera nivåer)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
De flesta nyheter bygger 30-60 sekunder B-roll av en MIX:

Exempel — "Översvämningar i Göteborg":
  0-8s:   NIVÅ 2 — Animerad karta med Göteborg markerat
  8-16s:  NIVÅ 1 — Redaktionell bild från SVT-artikeln (riktigt foto)
  16-24s: NIVÅ 3 — Stockvideo: räddningstjänst vid översvämning
  24-32s: NIVÅ 4 — AI-illustration: dramatiskt regnväder över stad
  32-38s: NIVÅ 2 — SMHI-prognos som animerad grafik

Exempel — "Trump aviserar nya tullar":
  0-8s:   NIVÅ 1 — Redaktionell bild av Trump (från artikelns og:image)
  8-16s:  NIVÅ 2 — Karta: USA ↔ handelspartners med pilar
  16-24s: NIVÅ 1 — Redaktionell bild #2 (om artikeln har flera)
  24-32s: NIVÅ 3 — Stockvideo: containerhamn, lastfartyg
  32-38s: NIVÅ 2 — Datavisualisering: tullnivåer i stapeldiagram

Exempel — "Nya regler för AI i EU":
  0-8s:   NIVÅ 4 — AI-illustration: abstrakt teknologi/datanätverk
  8-16s:  NIVÅ 1 — Bild av EU-parlamentet (från artikeln)
  16-24s: NIVÅ 3 — Stockvideo: person vid dator
  24-32s: NIVÅ 2 — Grafik: tidslinje för AI Acts implementering
  32-38s: NIVÅ 4 — AI-illustration: sköld/lås som symboliserar reglering
```

### Beslutsmatris för visuell strategi
```
Manuset MÅSTE tagga varje nyhet med rätt visual_strategy.
LLM:en får denna matris i sin system prompt:

┌─────────────────────────┬───────────────────────────────────────┐
│ NYHETSTYP               │ PRIMÄR VISUELL STRATEGI               │
├─────────────────────────┼───────────────────────────────────────┤
│ Namngiven person        │ NIVÅ 1 — Redaktionell bild            │
│ Specifik plats/byggnad  │ NIVÅ 1 — Redaktionell bild            │
│ Val / politik           │ NIVÅ 1 + NIVÅ 2 (bild + datavisualis.)│
│ Ekonomi / börs          │ NIVÅ 2 — Grafer och diagram           │
│ Väder / naturkatastrof  │ NIVÅ 2 + NIVÅ 3 (karta + stock)      │
│ Sport (resultat)        │ NIVÅ 1 + NIVÅ 2 (bild + resultattab.)│
│ Sjukvård / samhälle     │ NIVÅ 3 — Stockmaterial                │
│ Teknik / AI / cyber     │ NIVÅ 4 — AI-genererade illustrationer │
│ Kultur / nöje           │ NIVÅ 1 + NIVÅ 3 (bild + stock)       │
│ Brott (specifik plats)  │ NIVÅ 1 + NIVÅ 2 (bild + karta)       │
│ Abstrakt / opinion      │ NIVÅ 4 — AI-genererade illustrationer │
└─────────────────────────┴───────────────────────────────────────┘
```

### Implementation: OG-bild-extraktion
```
PIPELINE FÖR REDAKTIONELLA BILDER:

1. RSS ger oss artikel-URL:er
2. Fetcha artikelsidan (eller bara <head>)
3. Parsa og:image meta-tag:
   <meta property="og:image" content="https://svt.se/image/wide/992/123.jpg" />
4. Ladda ner bilden, spara lokalt
5. Använd i Remotion med Ken Burns-effekt

FALLBACK-KEDJA:
  1. og:image meta tag
  2. twitter:image meta tag
  3. RSS <enclosure> tag (ofta thumbnail)
  4. RSS <media:content> tag
  5. Första <img> i artikelns HTML
  6. Om inget hittas → NIVÅ 3 (stockmaterial) som fallback

VIKTIGT:
  - Attributera ALLTID källa i lower third: "Foto: [källa]"
  - Spara original-URL för spårbarhet
  - Respektera robots.txt (hämta inte från sajter som blockerar)
```

---

## 🏗️ ARKITEKTUR

### Tech Stack
```
Frontend:     Next.js 14+ (App Router) + TypeScript + Tailwind CSS
Video:        Remotion (videokomposition som React-komponenter)
Backend:      ASP.NET Core 8 Web API
Realtid:      SignalR (jobb-status till frontend)
Kö:           RabbitMQ (Docker) → Azure Service Bus i produktion
Databas:      PostgreSQL (Docker lokalt)
Storage:      Lokal filsystem under dev → Azure Blob Storage i produktion
```

### Systemoversikt
```
┌─────────────────────────────────────────────────────────┐
│  FRONTEND (Next.js + TypeScript + Remotion)             │
│                                                         │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ Vardagsrums- │  │ Filter &     │  │ Remotion      │  │
│  │ TV-vy        │  │ Konfiguration│  │ Player        │  │
│  └──────┬──────┘  └──────┬───────┘  └───────┬───────┘  │
│         └────────────────┼──────────────────┘           │
└─────────────────────────┬───────────────────────────────┘
                          │ REST API + SignalR
┌─────────────────────────▼───────────────────────────────┐
│  BACKEND (ASP.NET Core 8)                               │
│                                                         │
│  ┌────────────────┐  ┌────────────────┐                 │
│  │ BroadcastOrch- │  │ NewsService    │                 │
│  │ estrator       │  │ (RSS + GNews)  │                 │
│  │ (Saga pattern) │  │                │                 │
│  └───────┬────────┘  └────────────────┘                 │
│          │                                              │
│  ┌───────▼─────────────────────────────────┐            │
│  │ Message Queue (RabbitMQ)                │            │
│  └───────┬─────────┬──────────┬────────────┘            │
│          │         │          │                         │
│  ┌───────▼──┐ ┌────▼───┐ ┌───▼────────┐                │
│  │TTS Worker│ │Avatar  │ │B-Roll      │                │
│  │          │ │Worker  │ │Worker      │                │
│  └──────────┘ └────────┘ └────────────┘                │
│                                                         │
│  ┌─────────────────────────────────────────┐            │
│  │ Composition Worker (Remotion CLI)       │            │
│  └─────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│  STORAGE                                                │
│  /storage/audio/       — TTS-ljudfiler                  │
│  /storage/avatars/     — Ankare-videoklipp              │
│  /storage/broll/       — B-roll bilder och video        │
│  /storage/broadcasts/  — Färdiga sändningar             │
│  /storage/cache/       — Cachade mallar, intro, outro   │
└─────────────────────────────────────────────────────────┘
```

### Interface-arkitektur (KRITISKT)
Varje extern tjänst MÅSTE gömmas bakom ett interface. Detta möjliggör mocking under dev och provider-byte utan kodändringar.

```
Interfaces att definiera FÖRST:
├── INewsSource              — Hämtar nyheter (RSS, GNews, mock)
├── IEditorialImageExtractor — Hämtar og:image från artikelsidor
├── IScriptGenerator         — Genererar nyhetsmanus (GPT-4o, Claude API, mock)
├── ITtsProvider             — Text-till-tal (ElevenLabs, Azure, mock)
├── IAvatarGenerator         — Ankare-video (HeyGen, D-ID, mock)
├── IBRollProvider           — EN bildkälla: stock, AI, editorial (strategi-agnostisk)
├── IBRollOrchestrator       — Koordinerar visuell strategi per nyhet (Nivå 1-5)
├── IMapGenerator            — Genererar kartbilder/animationer
├── IDataGraphicGenerator    — Genererar diagram och grafer
├── IVideoComposer           — Slutlig videomontering (Remotion, mock)
├── IStorageProvider         — Fillagring (lokal, Azure Blob, mock)
└── IBroadcastOrchestrator   — Orkestreringslogik (hela sändningen)
```

---

## 📁 PROJEKTSTRUKTUR

```
newsroom-ai/
├── CLAUDE.md                          ← DENNA FIL (läs alltid först)
├── README.md                          ← Projektdokumentation
├── docs/
│   ├── ai-nyhetsrapport.docx          ← Teknisk feasibility-rapport
│   ├── ARCHITECTURE.md                ← Arkitekturbeslut och diagram
│   ├── API-GUIDE.md                   ← API-nycklar, endpoints, limits
│   ├── CHANGELOG.md                   ← Alla ändringar per datum
│   └── DEV-DIARY.md                   ← Utvecklingslogg, beslut, lärdomar
│
├── src/
│   ├── backend/
│   │   └── NewsRoom.Api/              ← ASP.NET Core 8 solution
│   │       ├── NewsRoom.Api/          ← Web API-projekt
│   │       ├── NewsRoom.Core/         ← Domänmodeller, interfaces, DTOs
│   │       ├── NewsRoom.Infrastructure/ ← Implementationer av interfaces
│   │       ├── NewsRoom.Workers/      ← Background workers
│   │       └── NewsRoom.Tests/        ← Enhetstester + integrationstester
│   │
│   └── frontend/
│       └── newsroom-web/              ← Next.js 14 App Router
│           ├── app/                   ← Routes och pages
│           ├── components/            ← React-komponenter
│           │   ├── tv/                ← TV-vardagsrums-UI
│           │   ├── broadcast/         ← Remotion-kompositioner
│           │   ├── controls/          ← Filter och konfiguration
│           │   └── status/            ← Jobbstatus-komponenter
│           ├── lib/                   ← API-klient, typer, utils
│           └── public/
│               ├── images/            ← Vardagsrumsbild, TV-frame
│               └── templates/         ← Intro/outro-assets
│
├── mock-data/                         ← Testdata för utveckling
│   ├── news-articles.json             ← Hårdkodade nyhetsartiklar
│   ├── scripts/                       ← Exempelmanus
│   ├── audio/                         ← Placeholder TTS-ljud
│   ├── videos/                        ← Placeholder ankare-video
│   └── broll/                         ← Placeholder B-roll
│
├── docker-compose.yml                 ← PostgreSQL + RabbitMQ
├── .env.example                       ← Mall för miljövariabler
└── .gitignore
```

---

## 🔨 UTVECKLINGSFASER — BYGG I DENNA ORDNING

### FAS 1: Grundarkitektur + Mocks (Kostnad: $0)
```
PRIORITET: Bygg HELA pipelinen med fake data först.
TESTA: Att hela flödet fungerar end-to-end med mockar.

Uppgifter:
□ Skapa solution-struktur (backend + frontend)
□ Docker Compose med PostgreSQL + RabbitMQ
□ Definiera ALLA interfaces i NewsRoom.Core
□ Implementera mock-versioner av alla interfaces
□ Bygg BroadcastOrchestrator med saga-mönster
□ Bygg Worker Services med kö-konsumtion
□ SignalR hub för realtidsstatus
□ Frontend: Grundläggande sida med filter-val
□ Frontend: Status-vy som visar orkestreringsflöde
□ Remotion: Grundläggande nyhetssändning-template med placeholder-data
□ End-to-end test: Klicka "Generera" → se status → se placeholder-video
```

### FAS 2: Nyheter + Manus — KÖRS SKARPT FRÅN START (Kostnad: ~$1-5 totalt)
```
PRIORITET: HÖGSTA. Manuset är grunden för ALLT.
           Använd OpenAI API (GPT-4o) SKARPT — kostar ~$0.05/manus.
           Vi MÅSTE validera manus-kvalitet tidigt.

Uppgifter:
□ RSS-parser för SVT, DN, Expressen, SR
□ OG-bild-extraktion från artikelsidor (meta og:image)
□ Kategori-mappning (Inrikes/Utrikes/Sport/etc)
□ Tidsfiltrering (senaste 6h/12h/24h)
□ ScriptGenerator med GPT-4o (OpenAI API-nyckel finns redan)
□ LLM system prompt med:
  - Exakt JSON-schema för manusformat (se "Manusformat" ovan)
  - Visuell beslutsmatris (se "Visuell innehållsstrategi" ovan)
  - Regler för svensk nyhetssvenska
  - Krav på minst 5 min total estimerad tid
□ Manus-validering: verifiera JSON-schema, tidsuppskattningar, visual_content
□ visual_content.scenes med rätt type per nyhet:
  - "editorial_image" → OG-bild från artikeln
  - "generated_map" → Kartdata för Remotion
  - "stock_footage" → Söktermer för Pexels
  - "ai_generated_image" → Prompt för Flux/DALL-E
  - "generated_graphic" → Data för graf/diagram
□ Spara genererade manus i databasen
□ MANUELL GRANSKNING: Skriv ut manus i terminalen/UI för att
  verifiera kvalitet, ton, längd och visuella val
□ Testa: Riktiga nyheter → riktigt manus → granska output → mock-video
```

### FAS 3: TTS — Svensk röst (Kostnad: $5/månad)
```
PRIORITET: Naturligt klingande svensk röst.

Uppgifter:
□ ElevenLabs-integration (ITtsProvider)
□ Röstval och konfiguration för nyhetsankare-stil
□ Generera ljud per segment (intro, voiceover, outro)
□ Cacha genererat ljud (content hash → fil)
□ Timing-metadata: hur lång är varje ljudfil?
□ Testa: Riktiga nyheter → riktigt manus → riktigt ljud → mock-video
```

### FAS 4: Remotion-templates (Kostnad: $0)
```
PRIORITET: Professionella nyhetskompositioner.

Uppgifter:
□ Intro-vinjett (animerad grafik, logotyp, musik)
□ Ankare-vy (studio-bakgrund, namnplatta, lower third)
□ B-roll-vy (bild/video med Ken Burns-effekt, voiceover-ljud)
□ Nyhetsticker (scrollande text nedtill)
□ Kategori-grafik (ikon + text: "INRIKES", "SPORT", etc.)
□ Övergångar mellan segment (crossfade, wipe)
□ Outro-sekvens
□ TESTA: Fullständig 5+ minuters rendering med mock-video + riktigt ljud
```

### FAS 5: Avatar-integration (Kostnad: $18/månad D-ID)
```
PRIORITET: Riktigt AI-ankare.

Uppgifter:
□ D-ID-integration (IAvatarGenerator) — budget-testning
□ Skicka TTS-ljud → få tillbaka lip-sync video
□ Testa svensk lip-sync kvalitet
□ Hantera segmentering (ett API-anrop per nyhetssegment)
□ Parallell generering av ankare-segment
□ Cachelagring av genererade avatarklipp
□ BYTA TILL HeyGen när pipeline är stabil (för portfolio)
```

### FAS 6: Visuell pipeline — 5-nivå strategi (Kostnad: ~$5 totalt)
```
PRIORITET: Rätt bilder för rätt nyheter. Följ visuell strategi strikt.

Uppgifter:
□ NIVÅ 1 — Redaktionella bilder:
  □ OG-bild-extraktor (parsa meta og:image, twitter:image, enclosure)
  □ Fallback-kedja: og:image → twitter:image → RSS enclosure → första <img>
  □ Bildnedladdning med caching (URL-hash → lokal fil)
  □ Ken Burns-animation i Remotion
  □ Lower third: "Foto: [källa]"

□ NIVÅ 2 — Kartor & datagrafik:
  □ Remotion-komponent: AnimatedMap (Leaflet/SVG-baserad)
  □ Remotion-komponent: DataChart (stapel, linje, cirkel)
  □ Remotion-komponent: DataTable (valresultat, sportresultat)
  □ Konfigurerbar via props från manuset

□ NIVÅ 3 — Stockmaterial:
  □ Pexels API-integration (gratis stockvideo/bilder)
  □ Nyckelords-extraktion från manusets search_terms
  □ Relevansfiltrering (undvik uppenbart fel material)

□ NIVÅ 4 — AI-genererade illustrationer:
  □ Flux 2 Pro-integration för konceptuella bilder
  □ Prompt-prefix: "editorial illustration, conceptual, magazine style"
  □ HÅRDKODAD REGEL: Aldrig prompta med verkliga personers namn
  □ HÅRDKODAD REGEL: Aldrig prompta med specifika verkliga byggnader

□ NIVÅ 5 — Kombinationslogik:
  □ BRollOrchestrator läser manusets visual_content.scenes[]
  □ Dispatchar varje scene till rätt provider baserat på "type"
  □ Parallell generering av alla scenes
  □ Assemblerar till ett sammanhängande segment med crossfade
  □ Voiceover-ljud synkat över alla klipp

□ MULTI-KLIPP-LOGIK (KRITISKT):
  □ Varje nyhet får 3-6 visuella scener (från manuset)
  □ Varje scen varar 6-10 sekunder
  □ Totalt 30-60 sekunder B-roll per nyhet
  □ Crossfade-övergångar (0.5s) mellan scener
  □ Voiceover körs OAVBRUTET över alla scener
```

### FAS 7: UI-finish — Vardagsrums-TV (Kostnad: $0)
```
PRIORITET: Wow-faktor för portfolio.

Uppgifter:
□ Fotorealistisk vardagsrumsbild (AI-genererad eller stock)
□ TV-skärm identifierad med exakta koordinater
□ Video spelas upp INNE I TV:n med perspective transform
□ Ambient belysning — TV:ns ljus reflekteras i rummet (CSS/canvas)
□ Responsiv design (fungerar på mobil, surfplatta, desktop)
□ Subtila animationer: TV-slå-på-effekt när sändning startar
□ Ljud via webbläsarens mediaspelare med volymkontroll
□ Fullskärmsläge (klicka på TV:n för att expandera)
```

### FAS 8: Polish & Deploy (Kostnad: varierar)
```
PRIORITET: Produktionsklar demo.

Uppgifter:
□ Error handling för alla API-anrop
□ Retry-logik med exponential backoff
□ Graceful degradation (om en tjänst är nere)
□ Loading states och skeleton screens
□ SEO och meta tags för LinkedIn-delning
□ Docker-compose för enkel lokal körning
□ README med screenshots och demo-video
□ Deploy till Azure (valfritt)
```

---

## ⚙️ UTVECKLINGSREGLER

### SOLID-principer — ALLTID
```
S — Single Responsibility
    Varje klass har ETT ansvar. En Worker hanterar EN typ av jobb.
    BroadcastOrchestrator koordinerar, men delegerar allt arbete.

O — Open/Closed
    Ny provider? Implementera interfacet. Ändra INTE befintlig kod.
    Ny nyhetskategori? Lägg till i enum, ändra inte switch-satser.

L — Liskov Substitution
    MockAvatarGenerator och HeyGenAvatarGenerator är utbytbara.
    Alla INewsSource-implementationer returnerar samma NewsArticle-modell.

I — Interface Segregation
    ITtsProvider har BARA TTS-metoder. Inte bildgenerering.
    IStorageProvider har BARA lagring. Inte rendering.

D — Dependency Inversion
    ALLA workers och services tar emot interfaces via constructor injection.
    ALDRIG `new HeyGenClient()` direkt i en service.
    Registrera i DI-containern, injicera interfacet.
```

### Kodstandard
```
SPRÅK:
  - Kod, variabelnamn, metoder, kommentarer: ENGELSKA
  - UI-texter, nyhetsmanus, användarmeddelanden: SVENSKA
  - Dokumentation (CLAUDE.md, README): SVENSKA

NAMNGIVNING:
  - Interfaces: IServiceName (C#-konvention)
  - Implementationer: ProviderNameService (t.ex. ElevenLabsTtsProvider)
  - Mock: MockServiceName (t.ex. MockAvatarGenerator)
  - DTOs: EntityNameDto
  - API-controllers: EntityNameController

FORMATERING:
  - C#: Följ .editorconfig (skapas i Fas 1)
  - TypeScript: ESLint + Prettier med strict mode
  - Tabs vs spaces: Spaces (2 för TS/JSON, 4 för C#)

ASYNC/AWAIT:
  - ALLA I/O-operationer ska vara async
  - Suffix med Async: GenerateAsync(), FetchAsync()
  - Använd CancellationToken genomgående

ERROR HANDLING:
  - Custom exceptions: NewsSourceException, TtsGenerationException, etc.
  - ALDRIG svälj exceptions tyst (catch utan logging)
  - Structured logging med Serilog
  - Correlation ID genom hela pipeline (spåra ett broadcastjobb)
```

### Testning
```
REGEL: Skriv tester INNAN eller SAMTIDIGT som implementation.
REGEL: Aldrig pusha kod utan att befintliga tester passerar.

TESTSTRUKTUR:
  NewsRoom.Tests/
  ├── Unit/
  │   ├── Orchestrator/     — Saga-logik, statemachine
  │   ├── Services/         — Individuella services
  │   ├── ScriptGeneration/ — Manus-formatering och validering
  │   └── VideoComposition/ — Timing, segmentordning
  ├── Integration/
  │   ├── NewsSource/       — RSS-parsing med riktiga feeds (kan vara flaky)
  │   ├── Queue/            — RabbitMQ-meddelanden
  │   └── Api/              — Controller-endpoints
  └── E2E/
      └── BroadcastFlow/    — Hela flödet med mockar

TESTPRIORITET:
  1. BroadcastOrchestrator — saga-logik, felhantering, parallellisering
  2. ScriptGenerator — manus har rätt format, längd, segment
  3. VideoComposer — timing, segmentordning, minimikrav på längd
  4. API-endpoints — korrekt validering, felkoder
  5. Worker-logik — retry, timeout, felhantering

MOCK-REGLER:
  - Unit-tester: Mocka ALLT externt (interfaces)
  - Integration: Mocka externa API:er, testa egen infrastruktur
  - E2E: Kör med mock-providers, testa hela flödet

ASSERTION:
  - Testa att en genererad sändning ALLTID ≥ 5 minuter
  - Testa att inget segment är kortare än 20 sekunder
  - Testa att voiceover-timing matchar B-roll-längd
  - Testa att alla nyheter har korrekt kategori-tagg
```

### Git-workflow
```
BRANCH-STRATEGI:
  main          — Alltid körbar, alltid testad
  develop       — Integrationsgren
  feature/xxx   — En feature per branch
  fix/xxx       — Bugfixar

COMMIT-REGLER:
  ✅ Commita OFTA — efter varje meningsfullt steg
  ✅ Konventionella commit-meddelanden:
     feat: add ElevenLabs TTS integration
     fix: correct segment timing in Remotion composition
     refactor: extract news parsing into separate service
     test: add orchestrator saga unit tests
     docs: update API-GUIDE with HeyGen endpoints
     chore: update docker-compose with RabbitMQ config

  ❌ ALDRIG commita:
     - Brutna tester
     - API-nycklar eller hemligheter
     - node_modules, bin/, obj/
     - Genererade videofiler (lägg i .gitignore)

COMMIT-FREKVENS:
  - Efter varje ny funktion som fungerar
  - Efter varje ny test som passerar
  - Efter varje buggfix
  - Efter dokumentationsuppdateringar
  - MINST en commit per arbetspass
```

### Dokumentation
```
REGEL: Dokumentation uppdateras SAMTIDIGT som kod.
REGEL: Aldrig lämna en fil odokumenterad.

CHANGELOG.md:
  - Uppdatera vid VARJE commit som ändrar funktionalitet
  - Format: datum, ändring, ev. breaking changes

DEV-DIARY.md:
  - Logga beslut: "Valde D-ID över HeyGen för dev pga kostnad"
  - Logga problem: "ElevenLabs Swedish accent issue — workaround: ..."
  - Logga lärdomar: "Remotion Lambda kräver specifik ffmpeg-version"

API-GUIDE.md:
  - Varje extern API: endpoint, auth, rate limits, kostnad
  - Exempel-requests och responses
  - Kända begränsningar

README.md:
  - Setup-instruktioner (ska fungera med en enda `docker-compose up`)
  - Screenshots
  - Demo-video (när projektet är klart)
  - Tech stack med motiveringar
```

---

## 🖥️ UI-DESIGN: VARDAGSRUMS-TV

### Koncept
Appen ska kännas som att man sitter i sitt vardagsrum och tittar på nyheter. Inte som en vanlig webbapp — utan en upplevelse.

### Layout (Desktop)
```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                    VARDAGSRUMSBILD                            │
│                    (fotorealistisk)                           │
│                                                              │
│              ┌──────────────────────┐                        │
│              │                      │                        │
│              │    TV-SKÄRM          │                        │
│              │                      │                        │
│              │  (här spelas videon) │                        │
│              │                      │                        │
│              │                      │                        │
│              └──────────────────────┘                        │
│                    ┌──┐                                      │
│                    │  │ (TV-fot)                              │
│              ══════╧══╧══════  (TV-bänk)                     │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  KONTROLLPANEL (under eller vid sidan av TV:n)         │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌────────────┐   │  │
│  │  │Tidsperiod│ │Kategorier│ │ Antal   │ │ GENERERA  │   │  │
│  │  │ 6h/12/24│ │☑Inrikes │ │ nyheter │ │ SÄNDNING  │   │  │
│  │  │         │ │☑Utrikes │ │  5-10   │ │    ▶      │   │  │
│  │  │         │ │☑Sport   │ │         │ │           │   │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └────────────┘   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### TV-interaktion
```
TILLSTÅND 1 — Standby:
  TV:n visar en subtil "TV-brus" animation eller mörk skärm med klocka
  Text: "Välj dina nyheter och tryck Generera"

TILLSTÅND 2 — Genererar:
  TV:n visar en snygg laddningsskärm med:
  - Progressbar med procent
  - Steg-för-steg-status: "Hämtar nyheter... Skriver manus... Genererar röst..."
  - Estimerad tid kvar

TILLSTÅND 3 — Klar:
  TV:n "slår på sig" med en kort animeringseffekt
  Nyhetssändningen spelas upp inne i TV-skärmen
  Kontroller: Play/Pause, Volym, Fullskärm, Hoppa till nästa nyhet

TILLSTÅND 4 — Fullskärm:
  Klicka på TV:n eller fullskärmsknapp → videon expanderar till hela fönstret
  ESC eller klick → tillbaka till vardagsrum
```

### Teknisk implementation av TV-vy
```
APPROACH: CSS perspective transform + clip-path

1. Vardagsrumsbild som bakgrund (CSS background-image, 100vw/100vh)
2. <video> element positionerat med position: absolute
3. CSS clip-path eller mask för att matcha TV-skärmens form
4. perspective + rotateY för lätt 3D-effekt om bilden är vinklad
5. Box-shadow med dynamisk färg baserad på videoinnehåll (ambient light)
6. Eventuellt: CSS filter för att simulera skärmreflektion

ALTERNATIVT (enklare):
1. TV-frame som PNG med transparent skärmyta
2. Video renderas bakom TV-frame
3. z-index: video bakom, TV-frame framför
```

### Responsivitet
```
DESKTOP (>1200px):  Fullständig vardagsrumsvy med kontrollpanel under
TABLET (768-1200px): TV:n tar upp mer plats, kontrollpanel under
MOBIL (<768px):     TV:n full bredd, kontrollpanel som drawer/sheet
                    I fullskärm: vanlig videospelare
```

---

## 💰 KOSTNADSHANTERING

### API-nycklar (i .env, ALDRIG i kod)
```
# .env.example
NEWS_GNEWS_API_KEY=
LLM_PROVIDER=openai                # openai | claude | mock (OBS: openai som default!)
LLM_OPENAI_API_KEY=
LLM_CLAUDE_API_KEY=
TTS_PROVIDER=mock                  # mock | elevenlabs | azure
TTS_ELEVENLABS_API_KEY=
TTS_AZURE_KEY=
AVATAR_PROVIDER=mock               # mock | did | heygen
AVATAR_DID_API_KEY=
AVATAR_HEYGEN_API_KEY=
BROLL_IMAGE_PROVIDER=mock          # mock | flux | openai
BROLL_VIDEO_PROVIDER=mock          # mock | runway | pexels
BROLL_FLUX_API_KEY=
BROLL_RUNWAY_API_KEY=
BROLL_PEXELS_API_KEY=
EDITORIAL_IMAGE_ENABLED=true       # Hämta og:image från artiklar (gratis, alltid på)
```

### Provider-switching via config
```
REGEL: Byt provider genom att ändra EN miljövariabel.
REGEL: ALLA providers registreras i DI baserat på config.
REGEL: mock-provider är ALLTID default.

Exempel:
  TTS_PROVIDER=mock         → MockTtsProvider injiceras
  TTS_PROVIDER=elevenlabs   → ElevenLabsTtsProvider injiceras
  TTS_PROVIDER=azure        → AzureTtsProvider injiceras
```

### Cachning (KRITISKT för kostnadskontroll)
```
REGEL: Cacha ALLT som kan cachas.

Vad som cachas:
  - RSS-svar: 15 minuter TTL
  - Genererade manus: Content hash → fil (samma nyheter = samma manus)
  - TTS-ljud: Content hash → .mp3 (samma text = samma ljud)
  - Avatar-video: Audio hash → .mp4 (samma ljud = samma video)
  - B-roll bilder: Sökterm hash → bild (samma sökning = samma bild)
  - Remotion renders: Composition hash → .mp4

Cache-strategi:
  1. Beräkna hash av input
  2. Kolla om cache-fil finns
  3. Om ja → returnera cachad fil, skippa API-anrop
  4. Om nej → gör API-anrop, spara till cache, returnera

ALDRIG radera cache automatiskt under dev.
```

---

## 🧪 LOKAL UTVECKLING

### Förutsättningar
```
- .NET 8 SDK
- Node.js 20+
- Docker Desktop (för PostgreSQL + RabbitMQ)
- ffmpeg (för Remotion-rendering)
```

### Starta utvecklingsmiljö
```bash
# 1. Starta infrastruktur
docker-compose up -d

# 2. Backend
cd src/backend/NewsRoom.Api
dotnet run

# 3. Frontend
cd src/frontend/newsroom-web
npm install
npm run dev
```

### REGEL: Localhost tills allt fungerar
```
ALDRIG deploya halvfärdigt.
ALLTID testa lokalt först:
  - docker-compose up startar PostgreSQL + RabbitMQ
  - Backend på https://localhost:5001
  - Frontend på http://localhost:3000
  - Alla API-anrop mot localhost
  - Alla genererade filer sparas lokalt i /storage/
```

---

## 🔒 SÄKERHET & JURIDIK

### API-nycklar
```
ALDRIG i kod eller Git.
ALLTID i .env (som finns i .gitignore).
.env.example committad med tomma värden.
```

### EU AI Act compliance
```
ALLTID inkludera synlig text: "AI-genererad nyhetssändning"
ALLTID inkludera i video-metadata: AI-generated content tag
ALDRIG presentera som riktiga nyheter utan AI-markering
Nyhetsankaret ska INTE vara baserat på en riktig person
```

### Upphovsrätt
```
RSS-nyheter: Konsumera fakta, generera EGNA manus via LLM
ALLTID attributera källa visuellt (lower third: "Källa: SVT Nyheter")
ALDRIG kopiera artikeltext direkt
Stockmaterial: Använd BARA CC0 eller royalty-free
```

---

## 📋 DEFINITION OF DONE

En feature är klar när:
```
□ Koden kompilerar utan varningar
□ Alla befintliga tester passerar
□ Nya tester skrivna för ny funktionalitet
□ Dokumentation uppdaterad (CHANGELOG, ev. API-GUIDE, README)
□ Committat med konventionellt meddelande
□ Fungerar lokalt via localhost
□ Ingen hårdkodad konfiguration (allt via .env)
□ Interfaces används för alla externa beroenden
□ Error handling implementerad
□ Logging finns för felsökning
```

---

## 🚫 GÖR ALDRIG

```
❌ Starta med betalda API:er — börja ALLTID med mocks
❌ Bygga utan interfaces — SOLID bryts aldrig
❌ Committa API-nycklar
❌ Ignorera tester — de ska alltid vara gröna
❌ Skriva "TODO" utan att skapa ett GitHub issue
❌ Hårdkoda värden som borde vara config
❌ Generera video utan att validera minst 5 minuters längd
❌ Skapa nyhetsklipp kortare än 20 sekunder
❌ Deploaya innan allt fungerar på localhost
❌ Ändra interface-kontrakt utan att uppdatera alla implementationer
❌ Svälj exceptions utan logging
❌ Lämna odokumenterad kod
```

---

## ✅ GÖR ALLTID

```
✅ Läs denna fil före varje arbetspass
✅ Kör tester innan och efter ändringar
✅ Commita ofta med bra meddelanden
✅ Uppdatera CHANGELOG.md vid funktionsändringar
✅ Logga beslut i DEV-DIARY.md
✅ Använd mock-providers som default
✅ Cacha alla API-svar till disk
✅ Verifiera videolängd ≥ 5 minuter i tester
✅ Testa på localhost innan deploy
✅ Skriv tester för edge cases (tom nyhetslista, API timeout, etc.)
```
