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

namespace BeardedManStudios.Forge.Examples
{
	public class ForgeExample_TimeoutDisconnect : MonoBehaviour
	{
		private void Start()
		{
			// You must set a time in milliseconds for the timeout to happen
			Networking.PrimarySocket.ReadTimeout = 5000;

			Networking.PrimarySocket.timeoutDisconnected += TimeoutDisconnect;
		}

		private void TimeoutDisconnect()
		{
			Debug.Log("The server connection has timed out you are now " + (Networking.PrimarySocket.Connected ? "connected to" : "disconnected from") + " the server");
		}
	}
}