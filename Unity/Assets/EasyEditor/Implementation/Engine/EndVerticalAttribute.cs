// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using System;
using System.Reflection;


namespace EasyEditor
{
	/// <summary>
	/// Stop rendering <c>InspecterItemRenderer</c> in a vertical layout.
	/// </summary>
	[System.Serializable]
	[AttributeUsageAttribute(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
	public class EndVerticalAttribute : System.Attribute {
	}
}