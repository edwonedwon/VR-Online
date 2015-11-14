using UnityEngine;
using System.Collections;

using BeardedManStudios.Network;

public class FN_MG_CC_SpawnManager : SimpleNetworkedMonoBehavior
{
	public GameObject cube = null;
	private FN_MG_CC_SpawnPoint[] spawnPoints = null;

	public static FN_MG_CC_SpawnManager Instance { get; private set; }

	protected override void Awake()
	{
		base.Awake();

		spawnPoints = new FN_MG_CC_SpawnPoint[transform.childCount];

		for (int i = 0; i < transform.childCount; i++)
			spawnPoints[i] = transform.GetChild(i).GetComponent<FN_MG_CC_SpawnPoint>();

		Instance = this;
	}

	protected override void NetworkStart()
	{
		base.NetworkStart();

		if (OwningNetWorker.IsServer)
			CollectCube();
	}

	[BRPC]
	private void GetCubePosition()
	{

	}

	[BRPC]
	private void MoveCube(Vector3 target)
	{
		cube.transform.position = target;
	}

	public void CollectCube()
	{
		if (!OwningNetWorker.IsServer)
			return;

		RPC("MoveCube", NetworkReceivers.Others, spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position);
	}
}