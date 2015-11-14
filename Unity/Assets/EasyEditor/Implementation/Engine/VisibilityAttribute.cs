//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using System;

namespace EasyEditor
{
	/// <summary>
	/// Visibility attribute allows to define when a <c>InspectorItemRenderer</c> should be visible or not based on some conditions.
	/// </summary>
	[Serializable]
	[AttributeUsageAttribute(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
	public class VisibilityAttribute : Attribute
	{
		/// <summary>
		/// The id of the <c>InspectorItemRenderer</c> the visibility is based on.
		/// </summary>
		public string id = "";

		/// <summary>
		/// The value that the renderer with id <c>id</c> should have for the <c>InspectorItemRenderer</c> to be visible.
		/// </summary>
		public object value = null;

		public VisibilityAttribute(string id, object value)
		{
			this.id = id;
			this.value = value;
		}
	}
}
