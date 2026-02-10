'use client';

import { useState } from 'react';

interface TVScreenProps {
  state: 'standby' | 'generating' | 'playing' | 'error';
  progress?: number;
  statusMessage?: string;
  videoUrl?: string | null;
  errorMessage?: string | null;
  onFullscreen?: () => void;
}

export default function TVScreen({
  state,
  progress = 0,
  statusMessage,
  videoUrl,
  errorMessage,
  onFullscreen,
}: TVScreenProps) {
  const [isFullscreen, setIsFullscreen] = useState(false);

  const handleFullscreen = () => {
    setIsFullscreen(!isFullscreen);
    onFullscreen?.();
  };

  return (
    <div className={`relative ${isFullscreen ? 'fixed inset-0 z-50 bg-black' : ''}`}>
      {/* TV Frame */}
      <div className={`relative mx-auto ${isFullscreen ? 'w-full h-full' : 'max-w-4xl'}`}>
        {/* TV Body */}
        {!isFullscreen && (
          <div className="bg-gradient-to-b from-gray-800 to-gray-900 rounded-2xl p-3 shadow-2xl">
            {/* Screen bezel */}
            <div className="bg-black rounded-lg overflow-hidden aspect-video relative">
              {/* Screen content */}
              <TVContent
                state={state}
                progress={progress}
                statusMessage={statusMessage}
                videoUrl={videoUrl}
                errorMessage={errorMessage}
              />

              {/* Fullscreen button */}
              {state === 'playing' && (
                <button
                  onClick={handleFullscreen}
                  className="absolute bottom-4 right-4 bg-black/60 hover:bg-black/80 text-white p-2 rounded-lg transition-colors"
                  title="Fullskarm"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3" />
                  </svg>
                </button>
              )}
            </div>

            {/* TV stand */}
            <div className="flex justify-center mt-2">
              <div className="w-24 h-2 bg-gray-700 rounded-full" />
            </div>
          </div>
        )}

        {/* Fullscreen mode */}
        {isFullscreen && (
          <div className="w-full h-full flex items-center justify-center">
            <TVContent
              state={state}
              progress={progress}
              statusMessage={statusMessage}
              videoUrl={videoUrl}
              errorMessage={errorMessage}
            />
            <button
              onClick={handleFullscreen}
              className="absolute top-4 right-4 bg-black/60 hover:bg-black/80 text-white p-3 rounded-lg transition-colors z-10"
            >
              ESC
            </button>
          </div>
        )}
      </div>

      {/* TV ambient glow */}
      {!isFullscreen && state === 'playing' && (
        <div className="absolute -inset-8 bg-blue-500/10 rounded-3xl blur-3xl -z-10 animate-pulse" />
      )}
    </div>
  );
}

function TVContent({
  state,
  progress,
  statusMessage,
  videoUrl,
  errorMessage,
}: Omit<TVScreenProps, 'onFullscreen'>) {
  switch (state) {
    case 'standby':
      return (
        <div className="w-full h-full flex flex-col items-center justify-center bg-gradient-to-br from-gray-900 to-gray-950 text-gray-400">
          <div className="text-6xl mb-4 opacity-30">
            <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="opacity-40">
              <rect x="2" y="7" width="20" height="15" rx="2" ry="2" />
              <polyline points="17 2 12 7 7 2" />
            </svg>
          </div>
          <p className="text-lg font-light">Valj dina nyheter och tryck Generera</p>
          <p className="text-sm mt-2 opacity-50">NewsRoom AI</p>
        </div>
      );

    case 'generating':
      return (
        <div className="w-full h-full flex flex-col items-center justify-center bg-gradient-to-br from-gray-900 to-blue-950">
          <div className="text-center mb-8">
            <div className="text-2xl font-semibold text-white mb-2">Genererar sandning</div>
            <p className="text-blue-300 text-sm">{statusMessage || 'Forbereder...'}</p>
          </div>

          {/* Progress bar */}
          <div className="w-64 bg-gray-800 rounded-full h-3 mb-4">
            <div
              className="bg-blue-500 h-3 rounded-full transition-all duration-500 ease-out"
              style={{ width: `${progress}%` }}
            />
          </div>
          <p className="text-white text-lg font-mono">{progress}%</p>

          {/* Animated dots */}
          <div className="flex gap-2 mt-6">
            {[0, 1, 2].map((i) => (
              <div
                key={i}
                className="w-2 h-2 bg-blue-400 rounded-full animate-bounce"
                style={{ animationDelay: `${i * 0.2}s` }}
              />
            ))}
          </div>
        </div>
      );

    case 'playing':
      return (
        <div className="w-full h-full bg-black flex items-center justify-center">
          {videoUrl ? (
            <video
              src={videoUrl}
              controls
              autoPlay
              className="w-full h-full object-contain"
            />
          ) : (
            <div className="text-center text-white">
              <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="mx-auto mb-4 text-green-400">
                <circle cx="12" cy="12" r="10" />
                <path d="M9 12l2 2 4-4" />
              </svg>
              <p className="text-lg">Sandning klar!</p>
              <p className="text-sm text-gray-400 mt-2">Video genererad (mock-lage)</p>
            </div>
          )}
        </div>
      );

    case 'error':
      return (
        <div className="w-full h-full flex flex-col items-center justify-center bg-gradient-to-br from-gray-900 to-red-950">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="text-red-400 mb-4">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
            <line x1="12" y1="9" x2="12" y2="13" />
            <line x1="12" y1="17" x2="12.01" y2="17" />
          </svg>
          <p className="text-red-400 text-lg font-semibold">Nagot gick fel</p>
          <p className="text-red-300/70 text-sm mt-2 max-w-md text-center">
            {errorMessage || 'Ett ovantat fel uppstod. Forsok igen.'}
          </p>
        </div>
      );
  }
}
