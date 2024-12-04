
using UnityEngine;

namespace SmartGitUPM.Editor
{
    [UniqScriptableObjectPath("Assets/Editor/SmartGitUPM_PackageCollectionSetting.asset")]
    public sealed class PackageCollectionSetting : ScriptableObject
    {
        [SerializeField] PackageMetaData[] _packages;
        public PackageMetaData[] Packages => _packages;
    }
}