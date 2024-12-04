
using UnityEditor;

namespace SmartGitUPM.Editor
{
    public sealed class EditorStartupDetector
    {
        public static bool IsFirstInit
        {
            get => SessionState.GetBool("EditorStartupDetector_FirstInit", false);
            set => SessionState.SetBool("EditorStartupDetector_FirstInit", value);
        }
        
        [InitializeOnLoadMethod]
        static void DetectEditorStartup()
        {
            if (!IsFirstInit)
            {
                FirstInit();
                IsFirstInit = true;
            }
        }
        
        static void FirstInit()
        {
            var updater = new PackageUpdateChecker();
            updater.CheckUpdate()
                .Handled(_ => updater.Dispose());
        }
    }
}