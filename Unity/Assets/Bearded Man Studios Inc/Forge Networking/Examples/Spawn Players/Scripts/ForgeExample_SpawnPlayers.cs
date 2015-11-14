/*-----------------------------+------------------------------\
|                                                             |
|                        !!!NOTICE!!!                         |
|                                                             |
|  These libraries are under heavy development so they are    |
|  subject to make many changes as development continues.     |
|  For this reason, the libraries may not be well commented.  |
|  THANK YOU for supporting forge with all your feedback      |
|  suggestions, bug reports and comments!                     |
|                                                             |
|                               - The Forge Team              |
|                                 Bearded Man Studios, Inc.   |
|                                                             |
|  This source code, project files, and associated files are  |
|  copyrighted by Bearded Man Studios, Inc. (2012-2015) and   |
|  may not be redistributed without written permission.       |
|                                                             |
\------------------------------+-----------------------------*/



using UnityEngine;

using BeardedManStudios.Network;

public class ForgeExample_SpawnPlayers : MonoBehaviour
{
	public GameObject objectToSpawn = null;

	private void Start()
	{
		// This will buffer spawn the cube across the network for each client
		if (Networking.PrimarySocket.Connected)
			Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered);
		else
		{
			Networking.PrimarySocket.connected += delegate()
			{
				Networking.Instantiate(objectToSpawn, NetworkReceivers.AllBuffered);
			};
		}
	}
}