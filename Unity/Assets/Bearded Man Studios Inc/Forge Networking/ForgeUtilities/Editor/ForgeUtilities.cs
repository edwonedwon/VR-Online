using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class ForgeUtilities
{
    [MenuItem("Tools/Forge Networking/Chat")]
    private static void NewMenuOption()
    {
		GameObject.Instantiate(Resources.Load("FN_ChatWindow"));

		if (GameObject.FindObjectOfType<EventSystem>() == null)
		{
			GameObject evt = new GameObject("Event System");
			evt.AddComponent<EventSystem>();
			evt.AddComponent<StandaloneInputModule>();
			evt.AddComponent<TouchInputModule>();
		}
    }
}