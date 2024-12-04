
using System;
using System.Collections.Generic;

namespace SmartGitUPM.Editor
{
    public sealed class EditorViewSwitcher : IDisposable
    {
        public List<EditorView> Views { get; private set; } = new ();
        public string CurrentViewID { get; private set; }
        public string PrevViewID { get; private set; }
        public string CurrentViewDisplayName { get; private set; }
        
        bool _isDisposed;
        string _defaultViewID;

        public EditorViewSwitcher(string defaultViewID = default)
            => _defaultViewID = defaultViewID;
        
        public bool IsOpen() => !string.IsNullOrEmpty(CurrentViewID);
        
        public bool IsOpen(EditorView view) => view.IsOpen && CurrentViewID == view.ViewID;
        
        public bool Has(EditorView view) => Views.Contains(view);
        
        public void SetDefaultView(string viewID) => _defaultViewID = viewID;
        
        public EditorView Get(string viewID) => Views.Find(view => view.ViewID == viewID);
        
        public void Show(EditorView view)
        {
            if (view.IsOpen)
            {
                return;
            }
            
            PrevViewID = CurrentViewID;
            if (!string.IsNullOrEmpty(CurrentViewID))
            {
                Hide(CurrentViewID);
            }
            view.Open();
            CurrentViewID = view.ViewID;
            CurrentViewDisplayName = view.ViewDisplayName;
        }

        public void Update()
        {
            foreach (var view in Views)
            {
                if (view.IsOpen)
                {
                    view.Update();
                }
            }
        }
        
        bool Hide(string viewID)
        {
            var view = Get(viewID);
            if (!view.IsOpen)
            {
                return false;
            }
            view.Close();
            CurrentViewID = string.Empty;
            CurrentViewDisplayName = string.Empty;
            return true;
        }
        
        public void Add(params EditorView[] views)
        {
            foreach (var view in views)
            {
                if (!Has(view))
                {
                    Views.Add(view);
                }
            }
        }
        
        public void Remove(params string[] viewIDs)
        {
            foreach (var viewID in viewIDs)
            {
                var index = Views.FindIndex(view => view.ViewID == viewID);
                if (index >= 0)
                {
                    Views.RemoveAt(index);
                }
            }
        }

        public void Remove(params EditorView[] views)
        {
            foreach (var view in views)
            {
                var isRemoved = Views.Remove(view);
                if (isRemoved
                    && view.IsOpen)
                {
                    view.Close();
                }
            }
            ShowDefaultViewIfNeeded();
        }

        public void ShowDefaultViewIfNeeded()
        {
            if (!IsOpen())
            {
                ShowDefaultView();
            }
        }
        
        void ShowDefaultView()
        {
            var next = Get(_defaultViewID);
            if (next == default
                && Views.Count > 0)
            {
                next = Views[0];
            }
            if (next is {IsOpen: false})
            {
                Show(next);
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Views.Clear();
            Views = default;
        }
    }
}