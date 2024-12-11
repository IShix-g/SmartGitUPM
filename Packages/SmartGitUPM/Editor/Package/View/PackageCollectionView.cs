
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    public sealed class PackageCollectionView : EditorView
    {
        readonly PackageInstaller _installer;
        readonly PackageCollection _collection;
        Action _openSettingAction;
        Vector2 _scrollPos;
        readonly GUIContent _installedIcon;
        readonly GUIContent _updateIcon;
        
        CancellationTokenSource _tokenSource;
        bool _hasFixed;
        bool _hasUpdate;
        
        public PackageCollectionView(
            PackageInstaller installer,
            PackageCollection collection,
            Action openSettingAction,
            EditorWindow window)
            : base(window)
        {
            _installer = installer;
            _collection = collection;
            _openSettingAction = openSettingAction;
            _installedIcon = EditorGUIUtility.IconContent("d_Progress");
            _updateIcon = EditorGUIUtility.IconContent("Update-Available");
        }
        
        public override string ViewID => "collection-view";

        public override string ViewDisplayName => "Collection";

        protected override void OnOpen() {}

        protected override void OnClose() {}

        protected override void OnUpdate()
        {
            if (_collection.Details.Count == 0)
            {
                GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5), alignment = TextAnchor.MiddleCenter});

                if (_installer.IsProcessing)
                {
                    GUILayout.Label("Now Loading...");
                }
                else if (GUILayout.Button("Configure the settings"))
                {
                    _openSettingAction?.Invoke();
                }

                GUILayout.EndVertical();
                return;
            }
            
            var position = Window.position;
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width));
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });
            
            foreach (var detail in _collection.Details)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                var versionText = ToLocalVersionString(detail);
                if (detail.HasUpdate
                    && !detail.IsFixedVersion)
                {
                    if (detail.IsInstalled)
                    {
                        versionText += " \u2192 ";
                    }
                    versionText += ToServerVersionString(detail);
                }
                
                if (string.IsNullOrEmpty(versionText))
                {
                    versionText = detail.IsFixedVersion
                        ? ToFixedVersion(detail)
                        : " v---";
                }
                
                if (detail.IsFixedVersion)
                {
                    versionText += " (Fixed)";
                    _hasFixed = true;
                }
                
                var displayName = detail.IsLoaded ? detail.Remote.displayName : "Unknown";
                GUILayout.Label(displayName, GUILayout.Width(130));
                
                var color = GUI.color;
                if (detail.IsInstalled
                    && detail.HasUpdate
                    && !detail.IsFixedVersion)
                {
                    GUI.color = Color.yellow;
                    _hasUpdate = true;
                }
                GUILayout.Label(versionText);
                GUI.color = color;
                
                if (detail.IsInstalled)
                {
                    var width = GUILayout.Width(22);
                    var height = GUILayout.Height(EditorGUIUtility.singleLineHeight);
                    var icon = detail.HasUpdate && !detail.IsFixedVersion ? _updateIcon : _installedIcon;
                    GUILayout.Label(icon, width, height);
                }
                
                var buttonText = GetButtonText(detail);
                EditorGUI.BeginDisabledGroup(!detail.IsLoaded || _installer.IsProcessing);
                if (GUILayout.Button(buttonText, GUILayout.Width(70)))
                {
                    if (detail.HasUpdate
                        && !detail.IsFixedVersion
                        || !detail.IsInstalled)
                    {
                        _tokenSource?.SafeCancelAndDispose();
                        _tokenSource = new CancellationTokenSource();
                        var task = default(Task);
                        if (detail.HasUpdate
                            && detail.IsInstalled)
                        {
                            task = _installer.Install(new []{ detail.PackageInstallUrl }, _tokenSource.Token);
                        }
                        else
                        {
                            task = _installer.Install(detail.PackageInstallUrl, _tokenSource.Token);
                        }
                        task.Handled(_ =>
                        {
                            _tokenSource?.Dispose();
                            _tokenSource = default;
                        });
                    }
                    else if(detail.IsInstalled)
                    {
                        _tokenSource?.SafeCancelAndDispose();
                        _tokenSource = new CancellationTokenSource();
                        _installer.UnInstall(detail.Local.name, _tokenSource.Token)
                            .Handled(_ =>
                            {
                                _tokenSource?.Dispose();
                                _tokenSource = default;
                            });
                    }
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
            }
            
            if (_hasFixed)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("(Fixed) : Version is locked. Remove version number after '#' in the URL if necessary.", MessageType.Info);
            }

            if (_hasUpdate)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("\"v Installed \u2192 v Latest\" : Update available. Please update if necessary.", MessageType.Info);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        protected override void OnDestroy()
        {
            _openSettingAction = default;
            _tokenSource?.SafeCancelAndDispose();
        }
        
        string GetButtonText(PackageInfoDetails details)
        {
            if (!details.IsInstalled)
            {
                return "Install";
            }
            if (details.HasUpdate
                && !details.IsFixedVersion)
            {
                return "Update";
            }
            return "Remove";
        }

        string ToLocalVersionString(PackageInfoDetails details)
            => details.IsInstalled ? "v" + details.Local.version : string.Empty;

        string ToServerVersionString(PackageInfoDetails details)
            => details.IsLoaded ? "v" + details.Remote.version : string.Empty;

        string ToFixedVersion(PackageInfoDetails details)
        {
            if (details.IsInstalled)
            {
                return "v" + details.Local.version;
            }
            else
            {
                return "v" + details.GetVersionParam();
            }
        }
    }
}