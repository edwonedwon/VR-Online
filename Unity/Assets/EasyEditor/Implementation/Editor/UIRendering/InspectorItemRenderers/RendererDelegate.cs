//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using System;
using System.Collections;

namespace EasyEditor
{
    /// <summary>
    /// Delegate which can draw a group of UI elements
    /// </summary>
    public interface RendererDelegate
    {
        /// <summary>
        /// Function to be called to render the UI related to the renderer delegate.
        /// </summary>
        void Render(Action preRender);
    }
}
