
using SmartGitUPM.Editor.Localization;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    internal sealed class PackageCollectionSettingView : EditorView
    {
        Vector2 _scrollPos;
        SerializedObject _packageSettingSerializedObject;
        SerializedObject _localizationSettingSerializedObject;
        PackageCollectionSetting _packageSetting;
        LocalizationSetting _localizationSetting;
        readonly string _supportProtocols;

        LocalizationEntry _updateEntry;
        LocalizationEntry _installUrlEntry;
        LocalizationEntry _branchEntry;
        LocalizationEntry _languageEntry;

        public PackageCollectionSettingView(
                PackageCollectionSetting packageSetting,
                LocalizationSetting localizationSetting,
                LanguageManager languageManager,
                string[] supportProtocols,
                EditorWindow window
            )
            : base(window)
        {
            _packageSetting = packageSetting;
            _localizationSetting = localizationSetting;
            _supportProtocols = string.Join(", ", supportProtocols);
            _updateEntry = languageManager.GetEntry("Setting/Update");
            _installUrlEntry = languageManager.GetEntry("Setting/Install");
            _branchEntry = languageManager.GetEntry("Setting/Branch");
            _languageEntry = languageManager.GetEntry("Setting/Language");
        }

        public override string ViewID => "setting-view";

        public override string ViewDisplayName => "Setting";

        protected override void OnOpen()
        {
            _packageSettingSerializedObject = new SerializedObject(_packageSetting);
            _localizationSettingSerializedObject = new SerializedObject(_localizationSetting);
        }

        protected override void OnClose()
        {
            _packageSettingSerializedObject = default;
            _localizationSettingSerializedObject = default;
        }

        protected override void OnUpdate()
        {
            var position = Window.position;
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width));

            {
                GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });
                EditorGUILayout.HelpBox("UpdateNotify: " + _updateEntry.CurrentValue, MessageType.Info);
                EditorGUILayout.HelpBox("InstallUrl: " + _installUrlEntry.CurrentValue, MessageType.Info);
                EditorGUILayout.HelpBox("Branch: " + _branchEntry.CurrentValue, MessageType.Info);
                EditorGUILayout.HelpBox("Supported Protocols: " + _supportProtocols, MessageType.Info);
                GUILayout.EndVertical();

                _packageSettingSerializedObject.Update();
                var property = _packageSettingSerializedObject.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
                _packageSettingSerializedObject.ApplyModifiedProperties();
            }

            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 10, 5),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.BeginHorizontal(style);
                GUILayout.Label("Language Setting", style);
                GUILayout.EndHorizontal();
                EditorGUILayout.HelpBox(_languageEntry.CurrentValue, MessageType.Info);
                GUILayout.Space(10);

                _localizationSettingSerializedObject.Update();
                var property = _localizationSettingSerializedObject.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
                _localizationSettingSerializedObject.ApplyModifiedProperties();

                GUILayout.Space(20);
            }
            GUILayout.EndScrollView();
        }

        protected override void OnDestroy()
        {
            _packageSetting = default;
            _localizationSetting = default;
            _packageSettingSerializedObject = default;
            _localizationSettingSerializedObject = default;
            _updateEntry = default;
            _installUrlEntry = default;
            _branchEntry = default;
            _languageEntry = default;
        }
    }
}
