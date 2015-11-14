//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using UnityEditor;
using UEObject = UnityEngine.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EasyEditor
{
    /// <summary>
    /// Render a list of renderers based on the group they belong to and the specified order. Allows as well to hide/show groups/fields.
    /// </summary>
    abstract public class FullObjecRenderer : InspectorItemRenderer
    {
        protected List<InspectorItemRenderer> renderers;
        protected Groups groups;

        private bool currentLayoutIsHorizontal = false;

        public void HideGroup(string group)
        {
            groups.HideGroup(group);
        }
        
        public void ShowGroup(string group)
        {
            groups.ShowGroup(group);
        }
        
        public void HideRenderer(string id)
        {
            InspectorItemRenderer renderer = LookForRenderer(id);
            if (renderer != null)
            {
                renderer.hidden = true;
            }
        }
        
        public void ShowRenderer(string id)
        {
            InspectorItemRenderer renderer = LookForRenderer(id);
            if (renderer != null)
            {
                renderer.hidden = false;
            }
        }

        protected virtual void InitializeGroups()
        {
            this.groups = new Groups(new string[]{""});
        }
        
        /// <summary>
        /// Initializes the renderers list and order it. Should be called after <c>InitializeGroups</c> to avoir null error exception when 
        /// ordering the list based on groups.
        /// </summary>
        protected virtual void InitializeRenderersList()
        {
            if (groups == null)
            {
                Debug.LogError("InitializeGroups should be called before InitializeRenderersList since renderers order depends" +
                    " on the groups specified.");
            }
        }

        bool groupDescriptionInitialized = false;
        private void InitializeGroupDescription()
        {
            groupDescriptionInitialized = true;

            foreach (InspectorItemRenderer renderer in renderers)
            {
                if(!string.IsNullOrEmpty(renderer.inspectorAttribute.groupDescription))
                {
                    groups.SetGroupDescription(renderer.inspectorAttribute.group, renderer.inspectorAttribute.groupDescription);
                }
            }
        }
        
        public override void Render (Action preRender = null)
        {
            base.Render (preRender);

            if (groups == null)
            {
                InitializeGroups();

                if(groups == null)
                {
                    Debug.LogError("You should ensure your implementation of FullObjectRenderer set Groups groups in InitializeGroups() before to call Render on FullObjectRenderer. If there is not groups to set," +
                                   " initialize it with new Groups(string[]{''})");
                }
            }

            if (renderers == null)
            {
                InitializeRenderersList();
            }

            if (!groupDescriptionInitialized)
            {
                InitializeGroupDescription();
            }
            
            SetVisibility ();
            
            EditorGUILayout.BeginVertical();
            
            Group nextGroup = groups[0];
            
            foreach (InspectorItemRenderer renderer in renderers)
            {
                if (nextGroup != null)
                {
                    if (renderer.inspectorAttribute.group == nextGroup.name)
                    {
                        if (nextGroup.hidden == false)
                        {
                            DrawGroupHeader(nextGroup);
                        }
                        
                        int currentGroupIndex = groups.GetGroupIndex(nextGroup.name);
                        nextGroup = groups[currentGroupIndex + 1];
                    }
                }
                
                if (GroupDoesNotExist(renderer.inspectorAttribute.group, groups) || GroupExistsAndNotHidden(renderer.inspectorAttribute.group, groups))
                {
                    SetBeginLayout(renderer);
                    
                    if (renderer.hidden == false)
                    {
                        renderer.serializedObject = serializedObject;
                        
                        EditorGUILayout.BeginVertical();

                        Action optimizeLabelWidth = null;
                        Action cancelOptimizeLabelWidth = null;
                        float oldLabelWidth = EditorGUIUtility.labelWidth;

                        if(currentLayoutIsHorizontal)
                        {
                            if(!string.IsNullOrEmpty(renderer.GetLabel()))
                            {
                                optimizeLabelWidth = () => 
                                {
                                    var textDimensions = GUI.skin.label.CalcSize(new GUIContent(renderer.GetLabel()));
                                    EditorGUIUtility.labelWidth = textDimensions.x + 20f;
                                };

                                cancelOptimizeLabelWidth = () => 
                                {
                                    EditorGUIUtility.labelWidth = oldLabelWidth;
                                };
                            }
                        }

                        renderer.Render(optimizeLabelWidth);
                        renderer.PostRender(cancelOptimizeLabelWidth);
                        
                        EditorGUILayout.EndVertical();
                    }
                    
                    SetEndLayout(renderer);
                }
                
                GUILayout.Space(1f);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGroupHeader(Group group)
        {
            if (!string.IsNullOrEmpty(group.name))
            {
                GUILayout.Space(15f);
                GUILayout.Label(group.name, InspectorStyle.DefaultStyle.titleStyle);

                if(!string.IsNullOrEmpty(group.description))
                {
                    GUILayout.Label(group.description, InspectorStyle.DefaultStyle.titleDescriptionStyle);
                }

                GUILayout.Space(15f);
            }
        }
        
        private bool GroupDoesNotExist(string groupName, Groups group)
        {
            return !groups.Exist(groupName);
        }
        
        private bool GroupExistsAndNotHidden(string groupName, Groups group)
        {
            return groups.Exist(groupName) && !groups.IsHidden(groupName);
        }
        
        private void SetBeginLayout(InspectorItemRenderer renderer)
        {
            if(renderer.horizontalLayout == HorizontalLayout.BeginHorizontal)
            {
                EditorGUILayout.BeginHorizontal();
                currentLayoutIsHorizontal = true;
            }
            
            if(renderer.verticalLayout == VerticalLayout.BeginVertical)
            {
                EditorGUILayout.BeginVertical();
            }
        }
        
        private void SetEndLayout(InspectorItemRenderer renderer)
        {
            if(renderer.horizontalLayout == HorizontalLayout.EndHorizontal)
            {
                EditorGUILayout.EndHorizontal();
                currentLayoutIsHorizontal = false;
            }
            
            if(renderer.verticalLayout == VerticalLayout.EndVertical)
            {
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Sets the visibility of a renderer based on the attribute [Visibility(string id, object value)]. If the object with the id 'id' 
        /// has the value 'value', the the renderer holding the attribute is visible, otherwise it is not display in the inspector.
        /// </summary>
        private void SetVisibility()
        {
            foreach(InspectorItemRenderer renderer in renderers)
            {
                FieldInfo fieldInfo = null;
                
                if(renderer.entityInfo.isField)
                {
                    fieldInfo = renderer.entityInfo.fieldInfo;
                }
                
                if(fieldInfo != null)
                {
                    VisibilityAttribute visibilityAttribute = AttributeHelper.GetAttribute<VisibilityAttribute>(fieldInfo);
                    if(visibilityAttribute != null)
                    {
                        InspectorItemRenderer conditionalRenderer = LookForRenderer(visibilityAttribute.id);
                        if(conditionalRenderer != null && conditionalRenderer.entityInfo.isField)
                        {
                            if(visibilityAttribute.value.Equals(conditionalRenderer.entityInfo.fieldInfo.GetValue(_serializedObject.targetObject)))
                            {
                                ShowRenderer(renderer.GetIdentifier());
                            }
                            else
                            {
                                HideRenderer(renderer.GetIdentifier());
                            }
                        }
                        else
                        {
                            Debug.LogWarning("The identifier " + visibilityAttribute.id + " was not found in the list of renderers, or this renderer " +
                                             "was not initialized from a field. Ensure that the id parameter of the attribute Visibility refers to the id of a field " +
                                             "(name of the field if you did not specify explicitly the id of the field in [Inspector(id = \"...\").");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Looks for renderer in the renderers list based on an id. The default id is the field or the method name of a renderer.
        /// But this id can be modified with Inspector attribute.
        /// </summary>
        /// <returns>The for renderer.</returns>
        /// <param name="rendererId">Renderer identifier.</param>
        private InspectorItemRenderer LookForRenderer(string rendererId)
        {
            InspectorItemRenderer result = null;
            foreach (InspectorItemRenderer renderer in renderers)
            {
                if (renderer.GetIdentifier() == rendererId)
                {
                    result = renderer;
                    break;
                }
            }
            
            return result;
        }
    }
}