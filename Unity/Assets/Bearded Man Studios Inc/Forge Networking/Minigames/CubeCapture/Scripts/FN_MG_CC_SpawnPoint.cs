using UnityEngine;
using System.Collections;

public class FN_MG_CC_SpawnPoint : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(transform.position, 0.25f);
	}
}