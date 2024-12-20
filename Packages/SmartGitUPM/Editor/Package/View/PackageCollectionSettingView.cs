
using System;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    public sealed class PackageCollectionSettingView : EditorView
    {
        Vector2 _scrollPos;
        SerializedObject _serializedObject;
        PackageCollectionSetting _setting;
        readonly string _supportProtocols;
        
        public PackageCollectionSettingView(
                PackageCollectionSetting setting,
                string[] supportProtocols,
                EditorWindow window
            )
            : base(window)
        {
            _setting = setting;
            _supportProtocols = String.Join(", ", supportProtocols);;
        }

        public override string ViewID => "setting-view";
        
        public override string ViewDisplayName => "Setting";

        protected override void OnOpen()
            => _serializedObject = new SerializedObject(_setting);

        protected override void OnClose()
            => _serializedObject = default;
        
        protected override void OnUpdate()
        {
            _serializedObject.Update();
            
            var position = Window.position;
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width));
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });
            EditorGUILayout.HelpBox("UpdateNotify:\nYou will receive update notifications when you open the Unity Editor.", MessageType.Info);
            EditorGUILayout.HelpBox("InstallUrl:\nThe URL should only be from Git. Please specify the URL in the format required by \"Package Manager > Add package from git URL...\".", MessageType.Info);
            EditorGUILayout.HelpBox("Branch:\nBranch name (Optional). Leave empty to use default.", MessageType.Info);
            EditorGUILayout.HelpBox("Supported Protocols:\n" + _supportProtocols, MessageType.Info);
            GUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            var property = _serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                EditorGUILayout.PropertyField(property, true);
            }
            GUILayout.EndScrollView();
            
            _serializedObject.ApplyModifiedProperties();
        }
        
        protected override void OnDestroy()
        {
            _setting = default;
            _serializedObject = default;
        }
    }
}