// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using System;
using System.Reflection;

namespace EasyEditor
{
    /// <summary>
    /// Entity info is a wrapper for fieldInfo and methodInfo.
    /// </summary>
    public class EntityInfo  {

    	public readonly FieldInfo fieldInfo;
        public readonly string propertyPath;

    	public readonly MethodInfo methodInfo;
    	public readonly object caller;

    	public readonly bool isField;
    	public readonly bool isMethod;

    	public EntityInfo(FieldInfo fieldInfo, string propertyPath = "")
    	{
            isField = true;
            isMethod = false;
            this.fieldInfo = fieldInfo;
            this.propertyPath = propertyPath;
    	}

        public EntityInfo(MethodInfo methodInfo, object caller)
        {
            isField = false;
            isMethod = true;
            this.methodInfo = methodInfo;
            this.caller = caller;
        }

    	public string GetName()
    	{
    		if (isField) 
    		{
    			return fieldInfo.Name;
    		} 
    		else 
    		{
    			return methodInfo.Name;
    		}
    	}
    }
}
