'use client';

import { useState } from 'react';

const ALL_CATEGORIES = [
  { id: 'Inrikes', label: 'Inrikes' },
  { id: 'Utrikes', label: 'Utrikes' },
  { id: 'Sport', label: 'Sport' },
  { id: 'Politik', label: 'Politik' },
  { id: 'Ekonomi', label: 'Ekonomi' },
  { id: 'Teknik', label: 'Teknik' },
  { id: 'Kultur', label: 'Kultur' },
  { id: 'Noje', label: 'Noje' },
  { id: 'Vader', label: 'Vader' },
];

const TIME_PERIODS = [
  { value: 6, label: '6 timmar' },
  { value: 12, label: '12 timmar' },
  { value: 24, label: '24 timmar' },
  { value: 48, label: '48 timmar' },
];

interface FilterPanelProps {
  onGenerate: (config: {
    timePeriodHours: number;
    categories: string[];
    maxArticles: number;
  }) => void;
  isGenerating: boolean;
}

export default function FilterPanel({ onGenerate, isGenerating }: FilterPanelProps) {
  const [timePeriod, setTimePeriod] = useState(24);
  const [selectedCategories, setSelectedCategories] = useState<string[]>([
    'Inrikes', 'Utrikes', 'Sport', 'Politik', 'Ekonomi',
  ]);
  const [maxArticles, setMaxArticles] = useState(7);

  const toggleCategory = (id: string) => {
    setSelectedCategories((prev) =>
      prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id]
    );
  };

  const handleGenerate = () => {
    if (selectedCategories.length === 0) return;
    onGenerate({
      timePeriodHours: timePeriod,
      categories: selectedCategories,
      maxArticles,
    });
  };

  return (
    <div className="bg-gray-900/80 backdrop-blur-sm rounded-2xl p-6 border border-gray-700/50">
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {/* Time Period */}
        <div>
          <label className="block text-sm font-medium text-gray-300 mb-3">
            Tidsperiod
          </label>
          <div className="flex flex-wrap gap-2">
            {TIME_PERIODS.map((tp) => (
              <button
                key={tp.value}
                onClick={() => setTimePeriod(tp.value)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-all ${
                  timePeriod === tp.value
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                }`}
              >
                {tp.label}
              </button>
            ))}
          </div>
        </div>

        {/* Categories */}
        <div className="md:col-span-2">
          <label className="block text-sm font-medium text-gray-300 mb-3">
            Kategorier
          </label>
          <div className="flex flex-wrap gap-2">
            {ALL_CATEGORIES.map((cat) => (
              <button
                key={cat.id}
                onClick={() => toggleCategory(cat.id)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-all ${
                  selectedCategories.includes(cat.id)
                    ? 'bg-blue-600/80 text-white'
                    : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                }`}
              >
                {cat.label}
              </button>
            ))}
          </div>
        </div>

        {/* Articles count + Generate button */}
        <div className="flex flex-col justify-between">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-3">
              Antal nyheter: {maxArticles}
            </label>
            <input
              type="range"
              min={3}
              max={10}
              value={maxArticles}
              onChange={(e) => setMaxArticles(Number(e.target.value))}
              className="w-full accent-blue-500"
            />
            <div className="flex justify-between text-xs text-gray-500 mt-1">
              <span>3</span>
              <span>10</span>
            </div>
          </div>

          <button
            onClick={handleGenerate}
            disabled={isGenerating || selectedCategories.length === 0}
            className={`mt-4 w-full py-3 rounded-xl font-bold text-lg transition-all ${
              isGenerating || selectedCategories.length === 0
                ? 'bg-gray-700 text-gray-500 cursor-not-allowed'
                : 'bg-red-600 hover:bg-red-500 text-white shadow-lg shadow-red-600/25 hover:shadow-red-500/40'
            }`}
          >
            {isGenerating ? (
              <span className="flex items-center justify-center gap-2">
                <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Genererar...
              </span>
            ) : (
              'GENERERA SANDNING'
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
