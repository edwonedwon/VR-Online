using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PoweredByForge : MonoBehaviour
{
	public bool LightTheme = false;

	public GameObject[] Disabled;
	public Image LogoImage;

	public Sprite LightThemeLogo;

	public string SceneToLoad = "ForgeQuickStartMenu";

	// Use this for initialization
	IEnumerator Start()
	{
		if (LightTheme)
		{
			Camera.main.backgroundColor = Color.black;
			LogoImage.sprite = LightThemeLogo;
		}

		foreach (GameObject go in Disabled)
			go.SetActive(true);

		yield return new WaitForSeconds(1.5f);

		Application.LoadLevel(SceneToLoad);
	}
}
