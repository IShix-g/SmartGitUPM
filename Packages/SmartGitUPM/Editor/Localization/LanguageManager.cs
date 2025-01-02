
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartGitUPM.Editor.Localization
{
    internal sealed class LanguageManager : IDisposable
    {
        public event Action<SystemLanguage> OnLanguageChanged = delegate {};
        
        public bool IsDisposed { get; private set; }
        public IReadOnlyList<SystemLanguage> SupportedLanguages => _data.SupportedLanguages;
        public SystemLanguage CurrentLanguage { get; private set; }
        
        LocalizationData _data;

        public LanguageManager(LocalizationData data, SystemLanguage language)
        {
            ResetData(data);
            SetLanguage(language);
        }

        public void SetLanguage(SystemLanguage language)
        {
            language = _data.ToSupportedLanguage(language);
            if (CurrentLanguage == language)
            {
                return;
            }
            CurrentLanguage = language;
            OnLanguageChanged(language);
        }
        
        public void ResetData(LocalizationData data)
        {
            _data = data;
            
            if (data.IsEmpty)
            {
                Debug.LogWarning("data is empty.");
            }
        }

        public LocalizationEntry GetEntry(string key)
        {
            if (_data.TryGetEntry(key, out var entry))
            {
                return entry;
            }
            throw new ArgumentNullException($"Missing entry for {key}");
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            _data = default;
        }
    }
}