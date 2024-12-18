
using System;
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [Serializable]
    public sealed class PackageMetaData
    {
        [Tooltip("You will receive update notifications when you open the Unity Editor.")]
        public bool UpdateNotify;
        [Tooltip("The URL should only be from Git. Please specify the URL in the format required by \"Package Manager > Add package from git URL...\"")]
        public string InstallUrl;
        [Tooltip("Branch name (Optional). Leave empty to use default.")]
        public string Branch;
    }
}