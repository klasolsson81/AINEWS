'use client';

interface StatusBarProps {
  status: string;
  message: string | null;
  progress: number;
}

const STATUS_STEPS = [
  { key: 'FetchingNews', label: 'Hamtar nyheter' },
  { key: 'GeneratingScript', label: 'Skriver manus' },
  { key: 'GeneratingAudio', label: 'Genererar tal' },
  { key: 'GeneratingAvatars', label: 'Skapar ankare' },
  { key: 'GeneratingBRoll', label: 'Hamtar bilder' },
  { key: 'Composing', label: 'Monterar video' },
];

export default function StatusBar({ status, message, progress }: StatusBarProps) {
  if (status === 'Pending' || status === 'Completed' || status === 'Failed') return null;

  const currentIndex = STATUS_STEPS.findIndex((s) => s.key === status);

  return (
    <div className="bg-gray-900/60 backdrop-blur-sm rounded-xl p-4 border border-gray-700/30">
      <div className="flex items-center gap-4 mb-3">
        <div className="flex-1">
          <div className="h-2 bg-gray-800 rounded-full overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-blue-600 to-blue-400 rounded-full transition-all duration-700 ease-out"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
        <span className="text-sm font-mono text-blue-400 w-12 text-right">{progress}%</span>
      </div>

      <div className="flex justify-between gap-1">
        {STATUS_STEPS.map((step, i) => (
          <div
            key={step.key}
            className={`flex flex-col items-center gap-1 flex-1 ${
              i < currentIndex
                ? 'text-green-400'
                : i === currentIndex
                ? 'text-blue-400'
                : 'text-gray-600'
            }`}
          >
            <span className="text-lg">
              {i < currentIndex ? (
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-green-400">
                  <circle cx="12" cy="12" r="10" />
                  <path d="M9 12l2 2 4-4" />
                </svg>
              ) : i === currentIndex ? (
                <svg className="animate-spin h-[18px] w-[18px] text-blue-400" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
              ) : (
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-gray-600">
                  <circle cx="12" cy="12" r="10" />
                </svg>
              )}
            </span>
            <span className="text-[10px] text-center leading-tight">{step.label}</span>
          </div>
        ))}
      </div>

      {message && (
        <p className="text-center text-sm text-gray-400 mt-3">{message}</p>
      )}
    </div>
  );
}
