using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

namespace EasyEditor
{
    /// <summary>
    /// Custom class property drawer. WIP.
    /// </summary>
    public class CustomClassPropertyDrawer : PropertyDrawer
    {
        private InlineClassRenderer inlineClassRenderer;

        private SerializedFieldRenderer fieldRenderer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (fieldRenderer == null)
            {
                fieldRenderer = InspectorItemRenderer.CreateInstance<SerializedFieldRenderer>();
                fieldRenderer.InitializeFromEntityInfo(new EntityInfo(this.fieldInfo, property.propertyPath));
                fieldRenderer.serializedObject = property.serializedObject;
            }
//
//            Debug.Log("current Event " + Event.current.type);
//            Debug.Log("property " + property.propertyPath);
//            Debug.Log("position " + position);
//            Debug.Log("label : " + label.text);

            EditorGUI.BeginProperty(position, label, property);
            //GUILayout.BeginArea(position);
            GUILayout.BeginVertical();
            GUILayout.Label(label);
            GUILayout.Button("hey");
            //fieldRenderer.Render();
            GUILayout.EndVertical();
            //GUILayout.EndArea();
            //fieldRenderer.PostRender();

            EditorGUI.EndProperty();
        }
    }
}