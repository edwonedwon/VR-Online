using System;
using System.Collections;
using System.Collections.Generic;

namespace EasyEditor
{
	/// <summary>
	/// List element comparer to order <c>InspectorItemRenderer</c> in the inspector based on their group and order.
	/// </summary>
	public class InspectorItemRendererOrderComparer : IComparer<InspectorItemRenderer>
	{
		Groups orderedGroupList;
		InspectorItemRenderer[] rendererArray;
		
		public InspectorItemRendererOrderComparer(Groups groups, List<InspectorItemRenderer> list)
		{
			orderedGroupList = groups;
			
			rendererArray = new InspectorItemRenderer[list.Count];
			list.CopyTo(rendererArray);
		}
		
		public int Compare(InspectorItemRenderer a, InspectorItemRenderer b)
		{
			int aIndex = orderedGroupList.GetGroupIndex(a.inspectorAttribute.group);
			int bIndex = orderedGroupList.GetGroupIndex(b.inspectorAttribute.group);
			
			int result = 1;
			
			if(string.IsNullOrEmpty(a.inspectorAttribute.group) || aIndex == -1 || 
			   string.IsNullOrEmpty(b.inspectorAttribute.group) || bIndex == -1)
			{
				if(Array.IndexOf(rendererArray,a) < Array.IndexOf(rendererArray,b))
				{
					result = -1;
				}
			}
			else
			{
				if (aIndex < bIndex)
				{
					result = -1;
				}
				else if (aIndex == bIndex)
				{
					if (a.inspectorAttribute.order < b.inspectorAttribute.order)
					{
						result = -1;
					}
					else if (a.inspectorAttribute.order == b.inspectorAttribute.order && Array.IndexOf(rendererArray,a) < Array.IndexOf(rendererArray,b))
					{
						result = -1;
					}
				}
			}
			
			return result;
		}
	}
}