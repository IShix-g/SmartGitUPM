
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace SmartGitUPM.Editor
{
    [CustomPropertyDrawer(typeof(DynamicDropdownAttribute))]
    public class DynamicDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dropdownAttribute = (DynamicDropdownAttribute) attribute;
            var targetObject = property.serializedObject.targetObject;

            var methodInfo = targetObject.GetType().GetMethod(
                dropdownAttribute.MethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (methodInfo == default)
            {
               Debug.LogError($"Method '{dropdownAttribute.MethodName}' not found.");
                return;
            }
            
            if (methodInfo.ReturnType != typeof(object[]))
            {
                Debug.LogError($"Method '{dropdownAttribute.MethodName}' must return object[].");
                return;
            }

            var options = (object[]) methodInfo.Invoke(targetObject, default);
            if (options == default
                || options.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No options available.");
                return;
            }
            
            var selectedIndex = Array.IndexOf(options, GetValue(property));
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
            }

            var displayOptions = Array.ConvertAll(options, item => item.ToString());
            var newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);
            
            if (newIndex != selectedIndex)
            {
                SetValue(property, options[newIndex]);
            }
        }

        object GetValue(SerializedProperty property)
            => fieldInfo.GetValue(property.serializedObject.targetObject);

        void SetValue(SerializedProperty property, object value)
        {
            fieldInfo.SetValue(property.serializedObject.targetObject, value);
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}