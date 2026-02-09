# API-Guide — NewsRoom AI

## Backend API (ASP.NET Core 8)

### Base URL
```
Development: https://localhost:5001/api
```

### Endpoints

#### POST /api/broadcast
Startar en ny nyhetssandning.

**Request Body:**
```json
{
  "timePeriodHours": 24,
  "categories": ["Inrikes", "Utrikes", "Sport"],
  "maxArticles": 7
}
```

**Response:**
```json
{
  "jobId": "uuid",
  "status": "Pending",
  "statusMessage": "Sandning skapad",
  "progressPercent": 0
}
```

#### GET /api/broadcast/{jobId}
Hamtar status for ett sandningsjobb.

**Response:**
```json
{
  "jobId": "uuid",
  "status": "GeneratingScript",
  "statusMessage": "Genererar nyhetsmanus...",
  "progressPercent": 25,
  "videoUrl": null,
  "errorMessage": null
}
```

#### GET /api/broadcast/recent
Hamtar senaste sandningsjobben.

### SignalR Hub

**URL:** `https://localhost:5001/hubs/broadcast`

**Events:**
- `BroadcastStatusUpdate` — Realtidsstatus for ett jobb
- `BroadcastCompleted` — Sandning fardig med video-URL
- `BroadcastFailed` — Sandning misslyckades med felmeddelande

---

## Externa API:er

### OpenAI GPT-4o (Manusgenerering)
- **Endpoint:** `https://api.openai.com/v1/chat/completions`
- **Modell:** `gpt-4o`
- **Kostnad:** ~$0.005/1K input tokens, ~$0.015/1K output tokens
- **Rate limit:** 10,000 RPM (Tier 2+)
- **Env:** `LLM_OPENAI_API_KEY`

### ElevenLabs (TTS)
- **Endpoint:** `https://api.elevenlabs.io/v1/text-to-speech/{voice_id}`
- **Modell:** `eleven_multilingual_v2`
- **Kostnad:** Starter $5/man (~30 min), Scale $22/man (~100 min)
- **Rate limit:** 2 concurrent requests (Starter)
- **Env:** `TTS_ELEVENLABS_API_KEY`

### D-ID (Avatar — Development)
- **Endpoint:** `https://api.d-id.com/talks`
- **Kostnad:** $18/man (16 min video)
- **Rate limit:** 10 concurrent
- **Env:** `AVATAR_DID_API_KEY`

### HeyGen (Avatar — Production)
- **Endpoint:** `https://api.heygen.com/v2/video/generate`
- **Kostnad:** $99/man (100 credits)
- **Env:** `AVATAR_HEYGEN_API_KEY`

### Pexels (Stock footage)
- **Endpoint:** `https://api.pexels.com/v1/search`, `https://api.pexels.com/videos/search`
- **Kostnad:** Gratis
- **Rate limit:** 200 requests/timme
- **Env:** `BROLL_PEXELS_API_KEY`

### GNews (Nyhetsdata)
- **Endpoint:** `https://gnews.io/api/v4/search`
- **Kostnad:** 49.99 EUR/man (1000 req/dag)
- **Env:** `NEWS_GNEWS_API_KEY`

### Flux 2 Pro (AI-bilder)
- **Endpoint:** Via Replicate eller BFL API
- **Kostnad:** ~$0.03-0.055/bild
- **Env:** `BROLL_FLUX_API_KEY`

---

## RSS-floden (Gratis)

| Kalla | URL | Kategori |
|-------|-----|----------|
| SVT Nyheter | `https://www.svt.se/nyheter/rss.xml` | Blandat |
| DN Sverige | `https://www.dn.se/rss/senaste-nytt/` | Inrikes |
| Expressen | `https://feeds.expressen.se/nyheter/` | Blandat |
| SR Ekot | `https://api.sr.se/api/rss/program/83` | Inrikes |
