
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class MenuTest : MonoBehaviour {

	[MenuItem ("File/Load Scene [Additive]")]
	static void Apply()
    {
		Debug.Log ("Doing Something...");

        var path = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
        if (path == null)
        {
            EditorUtility.DisplayDialog("Select Scene", "Select a scene", "Ok");
            return;
        }

        Debug.LogFormat("Opening scene {0}...", path);

        EditorApplication.OpenSceneAdditive(path);
	}
}

#endif
