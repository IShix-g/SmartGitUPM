
using UnityEditor;

namespace SmartGitUPM.Editor
{
    internal sealed class EditorInitializationExecuter
    {
        const string _key = "SmartGitUPM_EditorInitializationExecuter_FirstInit";
        
        public static bool IsFirstInit
        {
            get => SessionState.GetBool(_key, false);
            set => SessionState.SetBool(_key, value);
        }
        
        [InitializeOnLoadMethod]
        static void DetectEditorStartup()
        {
            if (!IsFirstInit)
            {
                FirstInit();
                IsFirstInit = true;
            }

            OnProjectLoadedInEditor();
        }

        static void FirstInit()
        {
            var updater = new PackageUpdateChecker();
            updater.CheckUpdate()
                .Handled(_ => updater.Dispose());
        }
        
        static void OnProjectLoadedInEditor()
            => PackageCacheManager.Initialize();
    }
}