const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'https://localhost:5001';

export interface BroadcastRequestDto {
  timePeriodHours: number;
  categories: string[];
  maxArticles: number;
}

export interface BroadcastStatusDto {
  jobId: string;
  status: string;
  statusMessage: string | null;
  progressPercent: number;
  videoUrl: string | null;
  errorMessage: string | null;
}

export interface NewsArticleDto {
  id: string;
  title: string;
  summary: string;
  sourceName: string;
  sourceUrl: string;
  imageUrl: string | null;
  category: string;
  publishedAt: string;
}

export async function createBroadcast(request: BroadcastRequestDto): Promise<BroadcastStatusDto> {
  const res = await fetch(`${API_BASE}/api/broadcast`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error(`Failed to create broadcast: ${res.statusText}`);
  return res.json();
}

export async function getBroadcastStatus(jobId: string): Promise<BroadcastStatusDto> {
  const res = await fetch(`${API_BASE}/api/broadcast/${jobId}`);
  if (!res.ok) throw new Error(`Failed to get status: ${res.statusText}`);
  return res.json();
}

export async function getRecentBroadcasts(): Promise<BroadcastStatusDto[]> {
  const res = await fetch(`${API_BASE}/api/broadcast/recent`);
  if (!res.ok) throw new Error(`Failed to get recent: ${res.statusText}`);
  return res.json();
}

export async function fetchNews(
  timePeriodHours: number,
  categories: string[],
  maxArticles: number
): Promise<NewsArticleDto[]> {
  const params = new URLSearchParams({
    timePeriodHours: timePeriodHours.toString(),
    categories: categories.join(','),
    maxArticles: maxArticles.toString(),
  });
  const res = await fetch(`${API_BASE}/api/news?${params}`);
  if (!res.ok) throw new Error(`Failed to fetch news: ${res.statusText}`);
  return res.json();
}

export async function getCategories(): Promise<string[]> {
  const res = await fetch(`${API_BASE}/api/news/categories`);
  if (!res.ok) throw new Error(`Failed to get categories: ${res.statusText}`);
  return res.json();
}
