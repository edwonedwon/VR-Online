using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;

public class NetworkPlayerManager
	: MonoBehaviour
{
    public GameObject PlayerPrefab;

    public void Start()
    {
        Debug.Assert(PlayerPrefab != null);

        Networking.Instantiate(PlayerPrefab, NetworkReceivers.AllBuffered, OnInstantiatePlayer);
    }

    public void Update()
    {
    }

    private void OnInstantiatePlayer(GameObject player)
    {
        Debug.LogFormat("NetworkPlayerManager.OnInstantiatePlayer(): {0}", player.name);

        var id = player.GetInstanceID();
        var script = player.GetComponent<NetworkPlayerInstance>();

        if (script)
        {
            script.OnConnect();
        }
    }
}
