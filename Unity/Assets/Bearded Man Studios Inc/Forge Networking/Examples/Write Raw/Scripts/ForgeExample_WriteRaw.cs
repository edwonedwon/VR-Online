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

public class ForgeExample_WriteRaw : MonoBehaviour
{
	private BMSByte dataCache = new BMSByte();

	private void Start()
	{
		Networking.PrimarySocket.rawDataRead += PrimarySocket_rawDataRead;
	}

	private void PrimarySocket_rawDataRead(NetworkingPlayer sender, BMSByte data)
	{
		// Print out the id and the convert the bytes to a string to print
		Debug.Log("Received message from " + sender.NetworkId + ", payload to string says: " + System.Text.Encoding.UTF8.GetString(data.byteArr, data.StartIndex(), data.Size));
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			// Clone will clear the dataCache and add the new data
			dataCache.Clone(System.Text.Encoding.UTF8.GetBytes("This is a raw message"));
			Networking.WriteRaw(Networking.PrimarySocket, dataCache, "EXAMPLE_Raw", true);
		}
	}
}