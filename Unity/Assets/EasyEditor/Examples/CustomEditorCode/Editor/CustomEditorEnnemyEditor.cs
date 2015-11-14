using UnityEditor;
using UnityEngine;
using System.Collections;

namespace EasyEditor
{
    [Groups("Game Designer Settings", "Basic Settings", "Advanced Settings")]
    [CustomEditor(typeof(CustomEditorEnnemy))]
    public class CustomEditorEnnemyEditor : EasyEditorBase
    {
        [Inspector(group = "Advanced Settings", rendererType = "CustomRenderer", order = 1)]
        [Comment("When the Armor reach zero, the Life drop twice faster.")]
        private void RenderProgressBars()
        {
    		RenderLifeBar ();

            GUILayout.Space(5);

    		RenderArmorBar ();
        }

    	private void RenderLifeBar()
    	{
    		Rect r = EditorGUILayout.BeginVertical();
    		EditorGUI.ProgressBar(r, 0.8f, "Life");
    		GUILayout.Space(18);
    		EditorGUILayout.EndVertical();
    	}

    	private void RenderArmorBar()
    	{
    		Rect r = EditorGUILayout.BeginVertical();
    		EditorGUI.ProgressBar(r, 0.4f, "Armor");
    		GUILayout.Space(18);
    		EditorGUILayout.EndVertical();
    	}
    }
}