
using System.Threading;
using SmartGitUPM.Editor.Localization;
using UnityEditor;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    internal sealed class PackageCollectionWindow : EditorWindow
    {
        public const string PackagePath = "Packages/" + _packageName + "/";
        const string _gitURL = "https://github.com/IShix-g/SmartGitUPM";
        const string _gitInstallUrl = _gitURL + ".git?path=Packages/SmartGitUPM";
        const string _gitBranchName = "main";
        const string _packageName = "com.ishix.smartgitupm";
        const string _firstOpenKey = "SmartGitUPM_PackageCollectionWindow_IsFirstOpen";
        
        [MenuItem("Window/Smart Git UPM")]
        public static void Open() => Open(IsFirstOpen);
        
        public static void Open(bool superReload)
        {
            var window = GetWindow<PackageCollectionWindow>("Smart Git UPM");
            window.minSize = new Vector2(440, 450);
            window._superReload = superReload;
            window.Show();
        }
        
        public static bool IsFirstOpen
        {
            get => SessionState.GetBool(_firstOpenKey, true);
            set => SessionState.SetBool(_firstOpenKey, value);
        }
        
        readonly PackageVersionChecker _versionChecker = new (_gitInstallUrl, _gitBranchName, _packageName);
        bool _superReload;
        SGUPackageManager _manager;
        EditorViewSwitcher _viewSwitcher;
        PackageCollectionSettingView _settingView;
        LanguageManager _languageManager;
        PackageCollectionView _collectionView;
        CancellationTokenSource _tokenSource;
        Vector2 _scrollPos;
        GUIContent _settingIcon;
        GUIContent _refreshIcon;
        GUIContent _backIcon;
        Texture2D _logo;
        LocalizationEntry _installAllEntry;
        
        void OnEnable()
        {
            _settingIcon = EditorGUIUtility.IconContent("Settings");
            _refreshIcon = EditorGUIUtility.IconContent("Refresh");
            _backIcon = EditorGUIUtility.IconContent("back");
            _logo = GetLogo();
            
            _languageManager = LanguageManagerFactory.GetOrCreate();
            _installAllEntry = _languageManager.GetEntry("InstallAll");
            
            _manager = SGUPackageManagerFactory.Create();
            _settingView ??= new PackageCollectionSettingView(
                    _manager.Setting,
                    UniqueLocalizationSetting.GetOrCreate(),
                    _languageManager,
                    _manager.Collection.GetSupportProtocols(),
                    this
                );
            _collectionView ??= new PackageCollectionView(
                    _manager.Installer,
                    _manager.Setting,
                    _manager.Collection,
                    _languageManager,
                    OpenSettingAction,
                    this
                );

            _collectionView.OnInstall += FetchPackagesNextFrame;
            _collectionView.OnUnInstall += FetchPackagesNextFrame;
            
            if (_viewSwitcher == default)
            {
                _viewSwitcher = new EditorViewSwitcher(_collectionView.ViewID);
                _viewSwitcher.Add(_settingView, _collectionView);
                _viewSwitcher.ShowDefaultViewIfNeeded();
            }
            _versionChecker.Fetch().Handled();
            FetchPackages(_superReload);
            _superReload = false;
            IsFirstOpen = false;
        }

        void OnDestroy()
        {
            _collectionView.OnInstall -= FetchPackagesNextFrame;
            _collectionView.OnUnInstall -= FetchPackagesNextFrame;
            EditorApplication.delayCall -= FetchPackagesNextFrame;
            
            _languageManager?.Dispose();
            _manager?.Dispose();
            _versionChecker?.Dispose();
            _viewSwitcher?.Dispose();
            _tokenSource?.SafeCancelAndDispose();
            _settingView?.Dispose();
            _collectionView?.Dispose();
            _settingIcon = default;
            _refreshIcon = default;
            _backIcon = default;
            _logo = default;
        }
        
        void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(_manager.Installer.IsProcessing);
            GUILayout.BeginHorizontal(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });
            var width = GUILayout.Width(33);
            var height = GUILayout.Height(EditorGUIUtility.singleLineHeight + 5);
            var settingIcon = _viewSwitcher.IsOpen(_settingView) ? _backIcon : _settingIcon;
            var clickedOpenSetting = GUILayout.Button(settingIcon, width, height);
            var clickedStartReload = GUILayout.Button(_refreshIcon, width, height);
            var clickedOpenManager = GUILayout.Button("Package Manager", height);
            var clickedGitHub = GUILayout.Button("GitHub page", height);
            var clickedVersion = false;
            if (_versionChecker.IsLoaded)
            {
                clickedVersion = GUILayout.Button(_versionChecker.LocalInfo.VersionString, height);
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            if (clickedGitHub)
            {
                Application.OpenURL(_gitURL);
            }
            else if (clickedOpenSetting)
            {
                if (!_viewSwitcher.IsOpen(_settingView))
                {
                    _viewSwitcher.Show(_settingView);
                }
                else
                {
                    _viewSwitcher.Show(_collectionView);
                }
            }
            else if (clickedStartReload)
            {
                if (!_viewSwitcher.IsOpen(_collectionView))
                {
                    _viewSwitcher.Show(_collectionView);
                }
                FetchPackages(true);
            }
            else if (clickedOpenManager)
            {
                PackageInstaller.OpenPackageManager();
            }
            else if (clickedVersion)
            {
                _tokenSource = new CancellationTokenSource();
                _versionChecker.CheckVersion(_tokenSource.Token);
            }

            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 5, 5),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.Label(_logo, style, GUILayout.MinWidth(430), GUILayout.Height(75));
            }
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 5, 5),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.BeginHorizontal(style);
                GUILayout.Label(_viewSwitcher.CurrentViewDisplayName, style);
                if (_viewSwitcher.IsOpen(_collectionView)
                    && _manager.Collection.HasMetas)
                {
                    EditorGUI.BeginDisabledGroup(_manager.Installer.IsProcessing);
                    if (GUILayout.Button("Install All", GUILayout.Width(70))
                        && _manager.Collection.HasMetas)
                    {
                        var userAgreed = EditorUtility.DisplayDialog(
                            "Install All",
                            _installAllEntry.CurrentValue,
                            "Install All",
                            "Close"
                        );

                        if (userAgreed)
                        {
                            var packageUrls = _manager.Collection.GetInstallPackageUrls();
                            _tokenSource = new CancellationTokenSource();
                            _manager.Installer.Install(packageUrls, _tokenSource.Token)
                                .Handled(_ =>
                                {
                                    _tokenSource.Dispose();
                                    _tokenSource = default;
                                    EditorApplication.delayCall += FetchPackagesNextFrame;
                                });
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
            }

            if (_viewSwitcher.IsOpen())
            {
                _viewSwitcher.Update();
            }
        }
        
        void FetchPackagesNextFrame()
        {
            EditorApplication.delayCall -= FetchPackagesNextFrame;
            FetchPackages(false);
        }
        
        void FetchPackages(bool superReload)
        {
            var setting = UniquePackageCollectionSetting.GetOrCreate();
            _manager.Collection.Set(setting.Packages);
            if (!_manager.Collection.HasMetas)
            {
                return;
            }
            _tokenSource?.SafeCancelAndDispose();
            _tokenSource = new CancellationTokenSource();
            
            _manager.Collection.FetchPackages(superReload)
                .Handled(_ =>
                {
                    Repaint();
                    _tokenSource?.Dispose();
                    _tokenSource = default;
                });
        }
        
        void OpenSettingAction()
        {
            if (_viewSwitcher.IsOpen(_settingView))
            {
                return;
            }
            
            _viewSwitcher.Show(_settingView);
            _settingView.OnCloseView += OnClosedSettingView;
        }

        void OnClosedSettingView(EditorView view)
        {
            _settingView.OnCloseView -= OnClosedSettingView;
            FetchPackages(true);
        }

        internal static Texture2D GetLogo() => GetTexture("SmartGitUPMLogo");
        
        static Texture2D GetTexture(string textureName)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D " + textureName, new []{ PackagePath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return default;
        }
    }
}