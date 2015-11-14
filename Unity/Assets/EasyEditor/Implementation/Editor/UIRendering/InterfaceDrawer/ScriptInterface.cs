//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using UEObject = UnityEngine.Object;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EasyEditor
{
    /// <summary>
    /// Draw the interface of a monobehaviour/scriptableobject by rendering <c>InspectorItemRender</c> objects.
    /// </summary>
    public class ScriptInterface : ScriptableObject
    {
        public MonoScript renderedScript;
        public List<InspectorItemRenderer> itemsToRender = new List<InspectorItemRenderer>();
    }
        
}