
using System;

namespace SmartGitUPM.Editor
{
    internal static class EditorViewSwitcherExtension
    {
        public static bool IsOpen(this EditorViewSwitcher @this, string viewID)
            => @this.IsOpen(@this.Get(viewID));
        
        public static void SetDefaultView(this EditorViewSwitcher @this, EditorView view)
            => @this.SetDefaultView(view.ViewID);
        
        public static bool Has(this EditorViewSwitcher @this, string viewID)
            => @this.Views.Exists(view => view.ViewID == viewID);
        
        public static void Show(this EditorViewSwitcher @this, string viewID)
        {
            if (!@this.Has(viewID))
            {
                throw new InvalidOperationException($"View {viewID} is not found.");
            }
            
            var view = @this.Get(viewID);
            if (!view.IsOpen)
            {
                @this.Show(view);
            }
        }
    }
}