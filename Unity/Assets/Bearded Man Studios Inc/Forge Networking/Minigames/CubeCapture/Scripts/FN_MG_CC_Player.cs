using UnityEngine;
using System.Collections;

using BeardedManStudios.Network;

public class FN_MG_CC_Player : NetworkedMonoBehavior
{
	private void OnTriggerEnter(Collider c)
	{
		if (c.name == "Cube")
			FN_MG_CC_SpawnManager.Instance.CollectCube();
	}
}