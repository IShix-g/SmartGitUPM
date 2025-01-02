
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace SmartGitUPM.Editor
{
    [CustomPropertyDrawer(typeof(OnValueChangedAttribute))]
    internal class OnValueChangedDrawer : PropertyDrawer
    {
        readonly Dictionary<string, MethodInfo> _methodCache = new();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var onValueChanged = (OnValueChangedAttribute) attribute;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label, true);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }
            
            property.serializedObject.ApplyModifiedProperties();
            
            foreach (var target in property.serializedObject.targetObjects)
            {
                var method = GetCachedMethod(target, onValueChanged.MethodName);
                if (method == default)
                {
                    Debug.LogError($"Method '{onValueChanged.MethodName}' not found on '{target.GetType().Name}'. " +
                                   "Make sure the method exists, is marked as public or private, and takes no parameters.");
                    continue;
                }
                method.Invoke(target, default);
            }
        }
        
        MethodInfo GetCachedMethod(object targetObject, string methodName)
        {
            var key = $"{targetObject.GetType().FullName}.{methodName}";
            if (_methodCache.TryGetValue(key, out var cachedMethod))
            {
                return cachedMethod;
            }

            var method = targetObject.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);

            if (method != default)
            {
                _methodCache[key] = method;
            }
            return method;
        }
    }
}