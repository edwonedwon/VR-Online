//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//

using UnityEngine;
using System.Collections;

namespace EasyEditor
{
	public class InspectorStyle {

        public GUIStyle titleStyle { get ; private set; }
        public GUIStyle titleDescriptionStyle { get ; private set; }

	    private static InspectorStyle defaultStyle;

        public static InspectorStyle DefaultStyle
        {
		    get {
                if (defaultStyle == null)
                {
                    defaultStyle = new InspectorStyle();
			    }

                return defaultStyle;
		    }
	    }
	
	    public InspectorStyle() 
        {
		    titleStyle = new GUIStyle((GUIStyle) "OL title");
            titleDescriptionStyle = GetTitleDescriptionStyle();;
	    }

        static private GUIStyle GetTitleDescriptionStyle()
        {
            GUIStyle titleDescriptionStyle = new GUIStyle((GUIStyle) "GroupBox");
            titleDescriptionStyle.alignment = TextAnchor.UpperLeft;
            titleDescriptionStyle.padding = new RectOffset(3, 3, 3, 3);
            titleDescriptionStyle.margin = new RectOffset(0, 0, 3, 3);
            titleDescriptionStyle.fontSize = 10;
            titleDescriptionStyle.wordWrap = true;
            return titleDescriptionStyle;
        }
	}
}