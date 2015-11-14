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



using System;
using System.Globalization;

namespace BeardedManStudios.Network
{
#if NETFX_CORE
	public class IPEndPointWinRT
	{
		public string ipAddress = string.Empty;
		public string Address { get { return ipAddress; } }
		public int port = 0;
		public int Port { get { return port; } }
	}
#endif

	public class NetworkingPlayer : NetworkingSerialized
	{
		/// <summary>
		/// The socket to the Networking player
		/// </summary>
		public object SocketEndpoint { get; private set; }
		
		/// <summary>
		/// The NetworkID the NetworkingPlayer is
		/// </summary>
		public ulong NetworkId { get; private set; }
		
		/// <summary>
		/// IP address of the NetworkingPlayer
		/// </summary>
		public string Ip { get; private set; }

		/// <summary>
		/// Name of the NetworkingPlayer
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// This is the message group that this particular player is a part of
		/// </summary>
		public ushort MessageGroup { get; private set; }

		/// <summary>
		/// Last ping sent to the NetworkingPlayer
		/// </summary>
		public DateTime LastPing { get; private set; }
		public UnityEngine.Vector3 Position { get; set; }
		public NetworkedMonoBehavior PlayerObject { get; private set; }

		/// <summary>
		/// The amount of time in seconds to disconnect this player if no messages are sent
		/// </summary>
		public float InactiveTimeoutSeconds { get; set; }

		private const float PLAYER_TIMEOUT_DISCONNECT = 180.0f;

		/// <summary>
		/// Constructor for the NetworkingPlayer
		/// </summary>
		/// <param name="networkId">NetworkId set for the NetworkingPlayer</param>
		/// <param name="ip">IP address of the NetworkingPlayer</param>
		/// <param name="socketEndpoint">The socket to the Networking player</param>
		/// <param name="name">Name of the NetworkingPlayer</param>
		public NetworkingPlayer(ulong networkId, string ip, object socketEndpoint, string name)
		{
			NetworkId = networkId;
			Ip = ip;
			SocketEndpoint = socketEndpoint;
			Name = name;
			LastPing = DateTime.Now;
			InactiveTimeoutSeconds = PLAYER_TIMEOUT_DISCONNECT;
		}

		/// <summary>
		/// Ping the NetworkingPlayer
		/// </summary>
		public void Ping()
		{
			LastPing = DateTime.Now;
		}

		/// <summary>
		/// Assigns the object that is the players identity object
		/// </summary>
		/// <param name="behavior">The NetworkedMonoBehavior that represents the player</param>
		public void SetMyBehavior(NetworkedMonoBehavior behavior)
		{
			if (!behavior.isPlayer || PlayerObject != null)
				return;

			PlayerObject = behavior;
		}

		/// <summary>
		/// Set the name of this player
		/// </summary>
		/// <param name="name">The name to be assigned</param>
		public void Rename(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Assigns the message group for this player
		/// </summary>
		/// <param name="messageGroup">The numerical identifier of the message group</param>
		public void SetMessageGroup(ushort messageGroup)
		{
			MessageGroup = messageGroup;
		}
	}
}