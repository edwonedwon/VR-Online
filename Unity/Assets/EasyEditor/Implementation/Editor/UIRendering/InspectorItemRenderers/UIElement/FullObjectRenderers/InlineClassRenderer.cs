//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EasyEditor
{
    /// <summary>
    /// Renders a custom class directly into the monobehaviour/scriptableobject inspector.
    /// </summary>
    public class InlineClassRenderer : FullObjecRenderer 
    {
        private object subtarget;
        private string foldoutTitle = "";
        private bool foldout = true;

        protected override void InitializeRenderersList()
        {
            base.InitializeRenderersList();

            subtarget = FieldInfoHelper.GetObjectFromPath(entityInfo.propertyPath, _serializedObject.targetObject);
            List<InspectorItemRenderer> fieldsRenderers = RendererFinder.GetListOfFields(subtarget, entityInfo.propertyPath);
            List<InspectorItemRenderer> methodsRenderers = RendererFinder.GetListOfMethods(subtarget);
            
            renderers = new List<InspectorItemRenderer>();
            renderers.AddRange(fieldsRenderers);
            renderers.AddRange(methodsRenderers);
            
            InspectorItemRendererOrderComparer comparer = new InspectorItemRendererOrderComparer(groups, renderers);
            renderers.Sort(comparer);
            
            foreach (InspectorItemRenderer renderer in renderers)
            {
                renderer.serializedObject = _serializedObject;
            }
        }

        protected override void InitializeGroups()
        {
            GroupsAttribute groupAttribute = AttributeHelper.GetAttribute<GroupsAttribute>(entityInfo.fieldInfo.FieldType);
            if (groupAttribute != null)
            {
                groups = new Groups(groupAttribute.groups);
            }
            else
            {
                groups = new Groups(new string[]{""});
            }
        }

        public override void Render(Action preRender = null)
        {
            if (string.IsNullOrEmpty(foldoutTitle))
            {
                foldoutTitle = ObjectNames.NicifyVariableName(entityInfo.fieldInfo.Name);
            }

            foldout = EditorGUILayout.Foldout(foldout, foldoutTitle);
            if (foldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);

                base.Render(preRender);

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}