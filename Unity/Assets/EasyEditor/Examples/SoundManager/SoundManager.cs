using UnityEngine;
using System.Collections;
using EasyEditor;

namespace EasyEditor
{
    public class SoundManager : MonoBehaviour {

    	[Comment("Ensure you input a new sound name before to click on Add Sound Handler")]
    	public string newSoundName = "";

    	[Inspector]
    	private void AddSoundHandler()
    	{
    		GameObject gameObject = new GameObject (newSoundName, typeof(SoundHandler));
    		gameObject.transform.position = this.transform.position;
    		gameObject.transform.parent = this.transform;
            gameObject.GetComponent<SoundHandler>().ID = newSoundName;
    	}
    }
}
