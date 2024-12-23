
using System;
using UnityEditor;

namespace SmartGitUPM.Editor
{
    internal abstract class EditorView : IDisposable
    {
        public event Action<EditorView> OnOpenView = delegate { };
        public event Action<EditorView> OnCloseView = delegate { };
        public bool IsOpen { get; private set; }
        
        bool _isDisposed;
        protected EditorWindow Window { get; private set; }

        protected EditorView(EditorWindow window) => Window = window;

        public abstract string ViewID { get; }
        public abstract string ViewDisplayName { get; }
        protected abstract void OnOpen();
        protected abstract void OnClose();
        protected abstract void OnUpdate();
        protected abstract void OnDestroy();
        
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }
            IsOpen = true;
            OnOpen();
            OnOpenView(this);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            IsOpen = false;
            OnClose();
            OnCloseView(this);
        }

        public void Update()
        {
            if (IsOpen)
            {
                OnUpdate();
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            OnDestroy();
            OnOpenView = default;
            OnCloseView = default;
            Window = default;
        }
    }
}