//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using System;
using System.Reflection;


namespace EasyEditor
{
	/// <summary>
	/// Start rendering <c>InspecterItemRenderer</c> in a vertical layout.
	/// </summary>
	[System.Serializable]
	[AttributeUsageAttribute(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
	public class BeginVerticalAttribute : System.Attribute {
	}
}