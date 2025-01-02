
using System;

namespace SmartGitUPM.Editor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class UniqueScriptableObjectPathAttribute : Attribute
    {
        public string Path { get; }

        public UniqueScriptableObjectPathAttribute(string path) => Path = path;
    }
}