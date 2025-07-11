
using System;
using System.Threading;
using System.Threading.Tasks;
using SmartGitUPM.Editor.Localization;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    internal sealed class PackageCollectionView : EditorView
    {
        public event Action OnInstall = delegate { };
        public event Action OnUnInstall = delegate { };

        PackageInstaller _installer;
        PackageCollection _collection;
        PackageCollectionSetting _setting;
        Action _openSettingAction;
        Vector2 _scrollPos;
        GUIContent _installedIcon;
        GUIContent _updateIcon;
        GUIContent _helpIcon;
        CancellationTokenSource _tokenSource;
        bool _hasFixed;
        bool _hasUpdate;
        LocalizationEntry _configureButtonEntry;
        LocalizationEntry _fixedEntry;
        LocalizationEntry _updateEntry;

        public PackageCollectionView(
            PackageInstaller installer,
            PackageCollectionSetting setting,
            PackageCollection collection,
            LanguageManager languageManager,
            Action openSettingAction,
            EditorWindow window)
            : base(window)
        {
            _installer = installer;
            _setting = setting;
            _collection = collection;
            _openSettingAction = openSettingAction;
            _installedIcon = EditorGUIUtility.IconContent("Progress");
            _updateIcon = EditorGUIUtility.IconContent("Update-Available");
            _helpIcon = EditorGUIUtility.IconContent("_Help");
            _configureButtonEntry = languageManager.GetEntry("Collection/ConfigureButton");
            _fixedEntry = languageManager.GetEntry("Collection/Fixed");
            _updateEntry = languageManager.GetEntry("Collection/Update");
        }

        public override string ViewID => "collection-view";

        public override string ViewDisplayName => "Collection";

        protected override void OnOpen() {}

        protected override void OnClose() {}

        protected override bool OnUpdate()
        {
            if (_collection.Details.Count == 0)
            {
                GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5), alignment = TextAnchor.MiddleCenter});

                if (_installer.IsProcessing
                    || (_setting.Length > 0
                        && _collection.IsFetching))
                {
                    GUILayout.Label("Now Loading...");
                }
                else if (GUILayout.Button(_configureButtonEntry.CurrentValue))
                {
                    _openSettingAction?.Invoke();
                }

                GUILayout.EndVertical();
                return false;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(Window.position.width));
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });

            foreach (var detail in _collection.Details)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);

                var setting = _setting.GetPackage(detail.PackageInstallUrl);
                detail.UpdatePackageUrl(setting.InstallUrl);

                var versionText = ToLocalVersionString(detail);
                if (detail.HasUpdate)
                {
                    if (detail.IsInstalled)
                    {
                        versionText += " \u2192 ";
                    }
                    versionText += detail.IsFixedVersion
                        ? detail.FixedVersion
                        : ToServerVersionString(detail);
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

                if (string.IsNullOrEmpty(setting.HelpPageUrl))
                {
                    GUILayout.Label(displayName, GUILayout.Width(150));
                }
                else
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(150));
                    var linkStyle = new GUIStyle(GUI.skin.label)
                    {
                        stretchWidth = false,
                        fixedHeight = EditorGUIUtility.singleLineHeight,
                    };
                    var clickedHelp = GUILayout.Button(displayName, linkStyle);
                    var iconStyle = new GUIStyle(GUI.skin.label)
                    {
                        fixedWidth = 25,
                        fixedHeight = EditorGUIUtility.singleLineHeight,
                    };
                    clickedHelp |= GUILayout.Button(_helpIcon, iconStyle);
                    GUILayout.EndHorizontal();

                    var lastRect = GUILayoutUtility.GetLastRect();
                    EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
                    if (clickedHelp)
                    {
                        Application.OpenURL(setting.HelpPageUrl);
                    }
                }

                var color = GUI.color;
                if (detail.IsInstalled
                    && detail.HasUpdate)
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
                    var icon = detail.HasUpdate ? _updateIcon : _installedIcon;
                    GUILayout.Label(icon, width, height);
                }

                var buttonText = GetButtonText(detail);
                EditorGUI.BeginDisabledGroup(!detail.IsLoaded || _installer.IsProcessing);
                if (GUILayout.Button(buttonText, GUILayout.Width(70)))
                {
                    if (detail.HasUpdate
                        || !detail.IsInstalled)
                    {
                        _tokenSource?.SafeCancelAndDispose();
                        _tokenSource = new CancellationTokenSource();
                        var op = default(Task);
                        if (detail.HasUpdate
                            && detail.IsInstalled)
                        {
                            op = _installer.Install(new []{ detail.PackageInstallUrl }, _tokenSource.Token);
                        }
                        else
                        {
                            op = _installer.Install(detail.PackageInstallUrl, _tokenSource.Token);
                        }
                        op.Handled(task =>
                        {
                            _tokenSource?.Dispose();
                            _tokenSource = default;
                            if (task.IsCompletedSuccessfully)
                            {
                                OnInstall();
                            }
                        });
                    }
                    else if(detail.IsInstalled)
                    {
                        _tokenSource?.SafeCancelAndDispose();
                        _tokenSource = new CancellationTokenSource();
                        _installer.UnInstall(detail.Local.name, _tokenSource.Token)
                            .Handled(task =>
                            {
                                _tokenSource?.Dispose();
                                _tokenSource = default;
                                if (task.IsCompletedSuccessfully)
                                {
                                    OnUnInstall();
                                }
                            });
                    }
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
            }

            if (_hasFixed)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(_fixedEntry.CurrentValue, MessageType.Info);
            }

            if (_hasUpdate)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(_updateEntry.CurrentValue, MessageType.Info);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            return false;
        }

        protected override void OnDestroy()
        {
            _setting = default;
            _installer = default;
            _collection = default;
            _openSettingAction = default;
            _tokenSource?.SafeCancelAndDispose();

            _installedIcon = default;
            _updateIcon = default;
            _helpIcon = default;

            _configureButtonEntry = default;
            _fixedEntry = default;
            _updateEntry = default;
        }

        string GetButtonText(PackageInfoDetails details)
        {
            if (!details.IsInstalled)
            {
                return "Install";
            }
            if (details.HasUpdate)
            {
                return "Update";
            }
            return "Remove";
        }

        string ToLocalVersionString(PackageInfoDetails details)
            => details.IsInstalled
                ? !details.Local.version.StartsWith("v")
                    ? "v" + details.Local.version
                    : details.Local.version
                : string.Empty;

        string ToServerVersionString(PackageInfoDetails details)
            => details.IsLoaded
                ? !details.Remote.version.StartsWith("v")
                    ? "v" + details.Remote.version
                    : details.Remote.version
                : string.Empty;

        string ToFixedVersion(PackageInfoDetails details)
        {
            if (details.IsInstalled)
            {
                return !details.Local.version.StartsWith("v")
                    ? "v" + details.Local.version
                    : details.Local.version;
            }
            var version = details.GetVersionParam();
            return !version.StartsWith("v")
                ? "v" + version
                : version;
        }
    }
}
