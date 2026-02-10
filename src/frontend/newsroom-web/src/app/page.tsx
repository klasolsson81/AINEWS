'use client';

import { useState, useCallback } from 'react';
import TVScreen from '@/components/tv/TVScreen';
import FilterPanel from '@/components/controls/FilterPanel';
import StatusBar from '@/components/status/StatusBar';
import { createBroadcast, getBroadcastStatus, type BroadcastStatusDto } from '@/lib/api';

type TVState = 'standby' | 'generating' | 'playing' | 'error';

export default function Home() {
  const [tvState, setTvState] = useState<TVState>('standby');
  const [currentJob, setCurrentJob] = useState<BroadcastStatusDto | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);

  const pollStatus = useCallback(async (jobId: string) => {
    try {
      const status = await getBroadcastStatus(jobId);
      setCurrentJob(status);

      if (status.status === 'Completed') {
        setTvState('playing');
        setIsGenerating(false);
      } else if (status.status === 'Failed') {
        setTvState('error');
        setIsGenerating(false);
      } else {
        // Continue polling
        setTimeout(() => pollStatus(jobId), 1000);
      }
    } catch {
      setTvState('error');
      setIsGenerating(false);
      setCurrentJob((prev) => prev ? { ...prev, errorMessage: 'Kunde inte kontakta servern.' } : null);
    }
  }, []);

  const handleGenerate = async (config: {
    timePeriodHours: number;
    categories: string[];
    maxArticles: number;
  }) => {
    try {
      setIsGenerating(true);
      setTvState('generating');
      setCurrentJob(null);

      const result = await createBroadcast(config);
      setCurrentJob(result);

      // Start polling
      pollStatus(result.jobId);
    } catch {
      setTvState('error');
      setIsGenerating(false);
      setCurrentJob({
        jobId: '',
        status: 'Failed',
        statusMessage: null,
        progressPercent: 0,
        videoUrl: null,
        errorMessage: 'Kunde inte starta sandning. Kontrollera att backend kors.',
      });
    }
  };

  const handleReset = () => {
    setTvState('standby');
    setCurrentJob(null);
    setIsGenerating(false);
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-950 via-gray-900 to-gray-950">
      {/* Header */}
      <header className="border-b border-gray-800/50 bg-gray-950/80 backdrop-blur-sm sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-red-600 rounded-lg flex items-center justify-center text-white font-bold text-sm">
              NR
            </div>
            <div>
              <h1 className="text-white font-bold text-lg leading-none">NewsRoom AI</h1>
              <p className="text-gray-500 text-xs">AI-genererad nyhetssandning</p>
            </div>
          </div>
          {tvState !== 'standby' && (
            <button
              onClick={handleReset}
              className="text-sm text-gray-400 hover:text-white transition-colors px-3 py-1.5 rounded-lg hover:bg-gray-800"
            >
              Ny sandning
            </button>
          )}
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 py-8 space-y-6">
        {/* TV */}
        <TVScreen
          state={tvState}
          progress={currentJob?.progressPercent || 0}
          statusMessage={currentJob?.statusMessage || undefined}
          videoUrl={currentJob?.videoUrl}
          errorMessage={currentJob?.errorMessage}
        />

        {/* Status bar (visible during generation) */}
        {currentJob && tvState === 'generating' && (
          <StatusBar
            status={currentJob.status}
            message={currentJob.statusMessage}
            progress={currentJob.progressPercent}
          />
        )}

        {/* Controls */}
        <FilterPanel onGenerate={handleGenerate} isGenerating={isGenerating} />
      </main>

      {/* Footer */}
      <footer className="border-t border-gray-800/30 mt-auto">
        <div className="max-w-7xl mx-auto px-4 py-4 text-center text-xs text-gray-600">
          AI-genererad nyhetssandning -- Ej riktiga nyheter -- Portfolio-projekt
        </div>
      </footer>
    </div>
  );
}
