
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    public sealed class CustomDialog : EditorWindow
    {
        CustomDialogContents _contents;
        Vector2 _scrollPos;
        Texture2D _logo;
        
        public static void Open(CustomDialogContents contents, string dialogTitle = default)
        {
            var window = GetWindow<CustomDialog>(dialogTitle);
            window.minSize = new Vector2(440, 300);
            window.maxSize = new Vector2(440, 300);
            window._contents = contents;
            window.ShowUtility();
        }

        void OnEnable()
        {
            _logo = PackageCollectionWindow.GetLogo();
        }

        void OnDestroy()
        {
            _logo = default;
            _contents?.Dispose();
        }
        
        void OnGUI()
        {
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 5, 0),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.Label(_logo, style, GUILayout.MinWidth(430), GUILayout.Height(75));
            }
            
            var wrapStyle = new GUIStyle
            {
                padding = new RectOffset(20, 20, 20, 20)
            };
            GUILayout.BeginVertical(wrapStyle);
            
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label(_contents.Title, titleStyle, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.Space(10);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(400));
            GUILayout.Label(_contents.Message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var width = GUILayout.Width(140);
            var height = GUILayout.Height(EditorGUIUtility.singleLineHeight + 10);
            if (GUILayout.Button(_contents.YesButtonText, width, height))
            {
                _contents.OnClickYes?.Invoke();
                _contents.OnClose?.Invoke(true);
                Close();
            }
            if (GUILayout.Button(_contents.NoButtonText, width, height))
            {
                _contents.OnClose?.Invoke(false);
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
    }
}