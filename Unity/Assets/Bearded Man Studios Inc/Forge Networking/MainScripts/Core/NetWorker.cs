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



#if BMS_DEBUGGING
#define BMS_DEBUGGING_UNITY
#endif

using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

#if !NETFX_CORE
using System.Reflection;
using System.Net.Sockets;
using System.Threading;
using System.Net;
#else
using Windows.Networking.Sockets;
#endif

namespace BeardedManStudios.Network
{
	public abstract class NetWorker
	{
		public const string CLIENT_READY_DYNAMIC_COMMAND = "ready";

		protected const int READ_THREAD_TIMEOUT = 50;
		public string Host { get; protected set; }
		public ushort Port { get; protected set; }

		private int threadSpeed = READ_THREAD_TIMEOUT;
		protected int ThreadSpeed { get { return threadSpeed; } set { threadSpeed = value; } }
		public void SetThreadSpeed(int speed) { ThreadSpeed = speed; }
		public NetworkingPlayer CurrentStreamOwner { get; protected set; }

		public Dictionary<string, DateTime> banList = new Dictionary<string, DateTime>();

		public bool MasterServerFlag { get; set; }

		/// <summary>
		/// The maximum connections allowed on this NetWorker(Socket)
		/// </summary>
		public int MaxConnections { get; protected set; }

		/// <summary>
		/// Current amount of connections
		/// </summary>
		public int Connections { get; private set; }

		/// <summary>
		/// Players conencted to this NetWorker(Socket)
		/// </summary>
		public virtual List<NetworkingPlayer> Players { get; protected set; }

		/// <summary>
		/// Assigns a list of players for a client
		/// </summary>
		public void AssignPlayers(List<NetworkingPlayer> playerList)
		{
			Players = playerList;
		}

		protected Dictionary<ulong, List<NetworkingStream>> rpcBuffer = new Dictionary<ulong, List<NetworkingStream>>();

		protected Dictionary<ulong, List<KeyValuePair<uint, NetworkingStream>>> udpRpcBuffer = new Dictionary<ulong, List<KeyValuePair<uint, NetworkingStream>>>();

		protected Dictionary<string, List<Action<NetworkingPlayer>>> dynamicCommands = new Dictionary<string, List<Action<NetworkingPlayer>>>();

		/// <summary>
		/// Basic event response delegate
		/// </summary>
		public delegate void BasicEvent();

		/// <summary>
		/// Basic event response delegate
		/// </summary>
		public delegate void PingReceived(HostInfo host, int time);

		/// <summary>
		/// Network Exception response delegate
		/// </summary>
		/// <param name="exception">Exception thrown</param>
		public delegate void NetworkErrorEvent(Exception exception);

		/// <summary>
		/// Network Message response delegate
		/// </summary>
		/// <param name="stream">Stream responded with</param>
		public delegate void NetworkMessageEvent(NetworkingStream stream);

		/// <summary>
		/// Direct Network Message response delegate
		/// </summary>
		/// <param name="player">Player responded with</param>
		/// <param name="stream">Stream responded with</param>
		public delegate void DirectNetworkMessageEvent(NetworkingPlayer player, NetworkingStream stream);

		/// <summary>
		/// Direct Raw Network Message response delegate
		/// </summary>
		/// <param name="data">Stream responded with</param>
		public delegate void DirectRawNetworkMessageEvent(NetworkingPlayer player, BMSByte data);

		/// <summary>
		/// Player connection response delegate
		/// </summary>
		/// <param name="player">Player who connected</param>
		public delegate void PlayerConnectionEvent(NetworkingPlayer player);

		/// <summary>
		/// An delegate signature to use for registering events around dynamic commands
		/// </summary>
		/// <param name="player">The player that called the dynamic command</param>
		public delegate void DynamicCommandEvent(NetworkingPlayer player);

		/// <summary>
		/// String response delegate
		/// </summary>
		/// <param name="message">String message responded with</param>
		public delegate void StringResponseEvent(string message);

		/// <summary>
		/// Byte array response delegate
		/// </summary>
		/// <param name="bytes">Byte array responded with</param>
		public delegate void ByteResponseEvent(byte[] bytes);

		/// <summary>
		/// This will make it so that only players who are close to one another will get updates about each other
		/// </summary>
		public bool ProximityBasedMessaging { get; set; }

		/// <summary>
		/// This is the distance in Unity units of the range that players need to be in to get updates about each other
		/// </summary>
		public float ProximityMessagingDistance { get; set; }

		protected List<NetworkingPlayer> alreadyUpdated = new List<NetworkingPlayer>();

		/// <summary>
		/// For determining if nat hole punching should be used
		/// </summary>
		public bool UseNatHolePunch { get; set; }

		/// <summary>
		/// When this is true the bandwidth usage will be tracked
		/// </summary>
		public bool TrackBandwidth { get; set; }

		/// <summary>
		/// This represents all of the bytes that have came in
		/// </summary>
		public ulong BandwidthIn { get; protected set; }

		/// <summary>
		/// This represents all of the bytes that have went out
		/// </summary>
		public ulong BandwidthOut { get; protected set; }

#if NETFX_CORE
		protected IPEndPointWinRT hostEndpoint = null;
		protected object groupEP = null;
#else
		protected IPEndPoint hostEndpoint = null;
		protected IPEndPoint groupEP = null;
#endif

		protected NetworkingPlayer server = null;

		/// <summary>
		/// This will turn on proximity based messaging, see ProximityBasedMessaging property of this class
		/// </summary>
		/// <param name="updateDistance">The distance in Unity units of the range that players need to be in to get updates about each other</param>
		public void MakeProximityBased(float updateDistance)
		{
			ProximityBasedMessaging = true;
			ProximityMessagingDistance = updateDistance;
		}

		/// <summary>
		/// This is a referenct to the current players identity on the network (server and client)
		/// </summary>
		public NetworkingPlayer Me { get; protected set; }

		/// <summary>
		/// The event to hook into for when a NetWorker(Socket) connects
		/// </summary>
		public event BasicEvent connected
		{
			add
			{
				connectedInvoker += value;
			}
			remove
			{
				connectedInvoker -= value;
			}
		}
		BasicEvent connectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

#if NETFX_CORE
		protected async void OnConnected()
#else
		protected void OnConnected()
#endif
		{
#if NETFX_CORE
			await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(50));
#else
			System.Threading.Thread.Sleep(50);
#endif

			if (Networking.IsBareMetal)
			{
				if (connectedInvoker != null)
					connectedInvoker();

				Connected = true;
				Disconnected = false;
			}
			else
			{
				if (connectedInvoker != null)
				{
					try
					{
						// If there is not a MAIN_THREAD_MANAGER then throw the error and disconnect
						BeardedManStudios.Network.Unity.MainThreadManager.Run(delegate()
						{
							connectedInvoker();
						});

						Connected = true;
						Disconnected = false;
					}
#if UNITY_EDITOR
					catch (Exception e)
					{
						UnityEngine.Debug.LogException(e);
#else
					catch
					{
#endif
						Disconnect();
					}
				}
			}
		}

		/// <summary>
		/// The event to hook into for when a NetWorker(Socket) disconnects
		/// </summary>
		public event BasicEvent disconnected
		{
			add
			{
				disconnectedInvoker += value;
			}
			remove
			{
				disconnectedInvoker -= value;
			}
		} BasicEvent disconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when a server disconnects
		/// </summary>
		public event StringResponseEvent serverDisconnected
		{
			add
			{
				serverDisconnectedInvoker += value;
			}
			remove
			{
				serverDisconnectedInvoker -= value;
			}
		} StringResponseEvent serverDisconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		public event DynamicCommandEvent clientReady
		{
			add
			{
				clientReadyInvoker += value;
			}
			remove
			{
				clientReadyInvoker -= value;
			}
		} DynamicCommandEvent clientReadyInvoker;

		protected void OnDisconnected()
		{
			Connected = false;
			Disconnected = true;

			if (Networking.IsBareMetal)
			{
				if (disconnectedInvoker != null)
					disconnectedInvoker();
			}
			else
			{
				if (disconnectedInvoker != null)
					disconnectedInvoker();
			}
		}

		#region Timeout Disconnect
		protected DateTime lastReadTime;
		public int LastRead
		{
			get
			{
				return (int)(DateTime.Now - lastReadTime).TotalMilliseconds;
			}
		}
		public int ReadTimeout = 0;

#if NETFX_CORE
		protected async void TimeoutCheck()
#else
		protected void TimeoutCheck()
#endif
		{
			while (true)
			{
				if (ReadTimeout == 0)
				{
#if NETFX_CORE
					await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(3000));
#else
					System.Threading.Thread.Sleep(3000);
#endif
					continue;
				}

#if NETFX_CORE
				await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromMilliseconds(ReadTimeout - LastRead + 1));
#else
				System.Threading.Thread.Sleep(ReadTimeout - LastRead + 1);
#endif

				if (Connected && LastRead >= ReadTimeout)
				{
					TimeoutDisconnect();
				}
			}
		}

		/// <summary>
		/// The event to hook into for when a NetWorker(Socket) disconnects
		/// </summary>
		public event BasicEvent timeoutDisconnected
		{
			add
			{
				timeoutDisconnectedInvoker += value;
			}
			remove
			{
				timeoutDisconnectedInvoker -= value;
			}
		} BasicEvent timeoutDisconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnTimeoutDisconnected()
		{
			Connected = false;
			Disconnected = true;

			if (timeoutDisconnectedInvoker != null)
				timeoutDisconnectedInvoker();
		}
		#endregion

		protected void OnDisconnected(string reason)
		{
			if (IsServer) return;

			Connected = false;
			Disconnected = true;

			if (serverDisconnectedInvoker != null) serverDisconnectedInvoker(reason);
		}

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) receives and error
		/// </summary>
		public event NetworkErrorEvent error
		{
			add
			{
				errorInvoker += value;
			}
			remove
			{
				errorInvoker -= value;
			}
		} NetworkErrorEvent errorInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnError(Exception exception)
		{
			if (errorInvoker != null)
			{
				if (Networking.IsBareMetal)
					errorInvoker(exception);
				else
					BeardedManStudios.Network.Unity.MainThreadManager.Run(delegate() { errorInvoker(exception); });
			}
		}

		public void ThrowException(NetworkException exception) { OnError(exception); }

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) sends data
		/// </summary>
		public event NetworkMessageEvent dataSent
		{
			add
			{
				dataSentInvoker += value;
			}
			remove
			{
				dataSentInvoker -= value;
			}
		} NetworkMessageEvent dataSentInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnDataSent(NetworkingStream stream) { if (dataSentInvoker != null) dataSentInvoker(stream); }

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) reads data
		/// </summary>
		public event DirectNetworkMessageEvent dataRead
		{
			add
			{
				dataReadInvoker += value;
			}
			remove
			{
				dataReadInvoker -= value;
			}
		} DirectNetworkMessageEvent dataReadInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) reads raw data
		/// </summary>
		public event DirectRawNetworkMessageEvent rawDataRead
		{
			add
			{
				rawDataReadInvoker += value;
			}
			remove
			{
				rawDataReadInvoker -= value;
			}
		} DirectRawNetworkMessageEvent rawDataReadInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnDataRead(NetworkingPlayer player, NetworkingStream stream)
		{
			if (dataReadInvoker != null) dataReadInvoker(player, stream);

			if (stream.identifierType == NetworkingStream.IdentifierType.Custom)
				OnCustomDataRead(stream.Customidentifier, player, stream);
		}

		protected void OnRawDataRead(NetworkingPlayer sender, BMSByte data)
		{
			if (rawDataReadInvoker != null)
			{
				data.MoveStartIndex(sizeof(byte));
				rawDataReadInvoker(sender, data);
			}
		}

		private uint currentCustomId = 0;
		private Dictionary<string, uint> customReadIdentifiers = new Dictionary<string, uint>();
		public Dictionary<string, uint> CustomReadIdentifiers { get { return customReadIdentifiers; } }
		private Dictionary<uint, string> customReadIdentifiersIds = new Dictionary<uint, string>();
		public Dictionary<uint, string> CustomReadIdentifiersIds { get { return customReadIdentifiersIds; } }
		private Dictionary<string, System.Action<NetworkingPlayer, NetworkingStream>> customDataRead = new Dictionary<string, System.Action<NetworkingPlayer, NetworkingStream>>();

		/// <summary>
		/// Add a custom event to the NetWorker(Socket) read event
		/// </summary>
		/// <param name="id">Unique identifier to pass with</param>
		/// <param name="action">Action to be added to the events to be called upon</param>
		public void AddCustomDataReadEvent(string id, System.Action<NetworkingPlayer, NetworkingStream> action)
		{
			if (!customDataRead.ContainsKey(id))
			{
				customReadIdentifiersIds.Add(currentCustomId, id);
				customReadIdentifiers.Add(id, currentCustomId++);
				customDataRead.Add(id, action);
			}
			else
				customDataRead[id] = action;
		}

		/// <summary>
		/// Adds a callback function for when a specified command is recieved on the network
		/// </summary>
		/// <param name="command">The name of the command to listen for</param>
		/// <param name="action">The callback action to add to the stack</param>
		public void AddDynaicCommandEvent(string command, System.Action<NetworkingPlayer> action)
		{
			if (!dynamicCommands.ContainsKey(command))
				dynamicCommands.Add(command, new List<Action<NetworkingPlayer>>());

			dynamicCommands[command].Add(action);
		}

		/// <summary>
		/// Removes all callbacks from a given command
		/// </summary>
		/// <param name="command">The name of the command to remove all callbacks for</param>
		public void RemoveDynaicCommandEvent(string command)
		{
			if (dynamicCommands.ContainsKey(command))
				dynamicCommands.Remove(command);
		}

		/// <summary>
		/// Removes a specific callback from a given command event stack
		/// </summary>
		/// <param name="command">The name of the command to remove a specified callback for</param>
		/// <param name="action">The specific callback to remove from the command stack</param>
		public void RemoveDynaicCommandEvent(string command, System.Action<NetworkingPlayer> action)
		{
			if (dynamicCommands.ContainsKey(command))
				dynamicCommands[command].Remove(action);
		}

		/// <summary>
		/// Remove a custom event from the NetWorker(Socket) read event
		/// </summary>
		/// <param name="id">Unique identifier to pass with</param>
		public void RemoveCustomDataReadEvent(string id) { if (customDataRead.ContainsKey(id)) customDataRead.Remove(id); }

		protected void OnCustomDataRead(string id, NetworkingPlayer player, NetworkingStream stream) { if (customDataRead.ContainsKey(id)) customDataRead[id](player, stream); }

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) recieves another player
		/// </summary>
		public event PlayerConnectionEvent playerConnected
		{
			add
			{
				playerConnectedInvoker += value;
			}
			remove
			{
				playerConnectedInvoker -= value;
			}
		} PlayerConnectionEvent playerConnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnPlayerConnected(NetworkingPlayer player)
		{
			if (playerConnectedInvoker != null)
			{
				if (Networking.IsBareMetal)
					playerConnectedInvoker(player);
				else
				{
					BeardedManStudios.Network.Unity.MainThreadManager.Run(delegate()
					{
						playerConnectedInvoker(player);
					});
				}
			}

			Connections++;
		}

		/// <summary>
		/// The event to hook into for when this NetWorker(Socket) disconnects another player
		/// </summary>
		public event PlayerConnectionEvent playerDisconnected
		{
			add
			{
				playerDisconnectedInvoker += value;
			}
			remove
			{
				playerDisconnectedInvoker -= value;
			}
		} PlayerConnectionEvent playerDisconnectedInvoker;	// Because iOS doesn't have a JIT - Multi-cast function pointer.

		protected void OnPlayerDisconnected(NetworkingPlayer player)
		{
			Unity.MainThreadManager.Run(() =>
			{
				foreach (SimpleNetworkedMonoBehavior behavior in SimpleNetworkedMonoBehavior.networkedBehaviors.Values)
				{
					if (behavior == null)
						continue;

#if NETFX_CORE
					if (behavior is NetworkedMonoBehavior)
#else
					if (behavior.GetType().IsSubclassOf(typeof(NetworkedMonoBehavior)))
#endif
					{
						if (((NetworkedMonoBehavior)behavior).isPlayer && behavior.OwnerId == player.NetworkId)
						{
							Networking.Destroy(behavior);
							break;
						}
					}
				}
			});

			if (Players.Contains(player))
				Players.Remove(player);

			if (playerDisconnectedInvoker != null)
			{
				if (Networking.IsBareMetal)
					playerDisconnectedInvoker(player);
				else
				{
					BeardedManStudios.Network.Unity.MainThreadManager.Run(delegate()
					{
						playerDisconnectedInvoker(player);
					});
				}
			}

			Connections--;
		}

		/// <summary>
		/// Returns a value whether or not this NetWorker(Socket) is the server
		/// </summary>
		public bool IsServer
		{
			get
			{
				if (this is DefaultServerTCP)
					return true;

				if (this is WinMobileServer)
					return true;

				if (this is CrossPlatformUDP)
					return ((CrossPlatformUDP)this).IsServer;

				return false;
			}
		}

		protected class StreamRead
		{
			public int clientIndex = -1;
			public BMSByte bytes = new BMSByte();

			public StreamRead() { }

			public StreamRead Prepare(int c, BMSByte b)
			{
				clientIndex = c;
				bytes.Clone(b);

				return this;
			}
		}

		/// <summary>
		/// The player count on this NetWorker(Socket)
		/// </summary>
		public ulong ServerPlayerCounter { get; protected set; }

		/// <summary>
		/// Whether or not this NetWorker(Socket) is connected
		/// </summary>
		public bool Connected { get; protected set; }

		/// <summary>
		/// Whether or not this NetWorker(Socket) was once connected and now is disconnected
		/// </summary>
		public bool Disconnected { get; protected set; }

		/// <summary>
		/// The update identifier of the NetWorker(Socket)
		/// </summary>
		public ulong Uniqueidentifier { get; private set; }

		/// <summary>
		/// Constructor of the NetWorker(Socket)
		/// </summary>
		public NetWorker()
		{
			if (!Networking.IsBareMetal)
				Unity.NetWorkerKiller.AddNetWorker(this);
		}

		/// <summary>
		/// Constructor of the NetWorker(Socket) with a Maximum allowed connections count
		/// </summary>
		/// <param name="maxConnections">The maximum number of connections allowed on this NetWorker(Socket)</param>
		public NetWorker(int maxConnections)
		{
			MaxConnections = maxConnections;

			if (!Networking.IsBareMetal)
				Unity.NetWorkerKiller.AddNetWorker(this);
		}

		~NetWorker() { Disconnect(); }

		abstract public void Connect(string hostAddress, ushort port);

		abstract public void Disconnect();

		abstract public void TimeoutDisconnect();

		/// <summary>
		/// Disconnect a player on this NetWorker(Socket)
		/// </summary>
		/// <param name="player">Player to disconnect</param>
		public virtual void Disconnect(NetworkingPlayer player, string reason = null)
		{
			if (alreadyUpdated.Contains(player))
				alreadyUpdated.Remove(player);

			OnPlayerDisconnected(player);
		}

		abstract public void Write(NetworkingStream stream);

		abstract public void Write(NetworkingPlayer player, NetworkingStream stream);

		abstract public void Send(byte[] data, int length, object endpoint = null);

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		public virtual void Write(string updateidentifier, NetworkingStream stream, bool reliable = false) { }

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Packets to send</param>
		public virtual void Write(uint updateidentifier, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null) { }

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier, Player, and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="player">Player to write with</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Packets to send</param>
		public virtual void Write(string updateidentifier, NetworkingPlayer player, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null) { }

		/// <summary>
		/// Write to the NetWorker(Socket) with a given Update Identifier, Player, and Network Stream
		/// </summary>
		/// <param name="updateidentifier">Unique update identifier to be used</param>
		/// <param name="player">Player to write with</param>
		/// <param name="stream">Network stream being written with</param>
		/// <param name="reliable">If this is a reliable send</param>
		/// <param name="packets">Packets to send</param>
		public virtual void Write(uint updateidentifier, NetworkingPlayer player, NetworkingStream stream, bool reliable = false, List<BMSByte> packets = null) { }

		public virtual void WriteRaw(NetworkingPlayer player, BMSByte data, string uniqueId, bool reliable = false) { }
		public virtual void WriteRaw(BMSByte data, string uniqueId, bool relayToServer = true, bool reliable = false) { }

		private object rpcMutex = new object();

		/// <summary>
		/// Read the data of a player and data stream
		/// </summary>
		/// <param name="player">Player to read from</param>
		/// <param name="stream">Network stream being read from</param>
		public void DataRead(NetworkingPlayer player, NetworkingStream stream)
		{
#if BMS_DEBUGGING_UNITY
			UnityEngine.Debug.Log("[NetWorker DataRead Player IP] " + (player != null ? player.Ip : string.Empty));
			UnityEngine.Debug.Log("[NetWorker DataRead Player NetworkID] " + (player != null ? player.NetworkId.ToString() : string.Empty));
			UnityEngine.Debug.Log("[NetWorker DataRead Stream Bytes] " + ((stream != null && stream.Bytes != null) ? stream.Bytes.Count.ToString() : string.Empty));
			UnityEngine.Debug.Log("[NetWorker DataRead Stream NetworkID] " + (stream != null ? stream.NetworkId.ToString() : string.Empty));
#endif
			OnDataRead(player, stream);

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC)
			{
#if BMS_DEBUGGING_UNITY
				UnityEngine.Debug.Log("[NetWorker DataRead New Stream RPC]");
#endif
				lock (rpcMutex)
				{
					new NetworkingStreamRPC(stream);
				}
			}
		}

		/// <summary>
		/// Get the new player updates
		/// </summary>
		public virtual void GetNewPlayerUpdates() { }

		public void ClearBufferRPC()
		{
			if (!IsServer)
				return;

			udpRpcBuffer.Clear();
			rpcBuffer.Clear();
		}

		protected void ServerBufferRPC(NetworkingStream stream)
		{
			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && stream.BufferedRPC)
			{
				if (!rpcBuffer.ContainsKey(stream.RealSenderId))
					rpcBuffer.Add(stream.RealSenderId, new List<NetworkingStream>());

				NetworkingStream clonedStream = new NetworkingStream(stream.ProtocolType);
				clonedStream.Bytes.BlockCopy(stream.Bytes.byteArr, stream.Bytes.StartIndex(), stream.Bytes.Size);
				clonedStream.AssignSender(Me, stream.NetworkedBehavior);

				rpcBuffer[stream.RealSenderId].Add(clonedStream);
			}
		}

		protected void RelayStream(NetworkingStream stream)
		{
			if (stream.Receivers == NetworkReceivers.Server)
				return;

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && stream.BufferedRPC)
			{
				NetworkingStream clonedStream = new NetworkingStream(stream.ProtocolType).PrepareFinal(this, stream.identifierType, stream.NetworkedBehaviorId, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, senderId: stream.RealSenderId);
				clonedStream.AssignSender(stream.Sender, stream.NetworkedBehavior);

				if (!rpcBuffer.ContainsKey(stream.RealSenderId))
					rpcBuffer.Add(stream.RealSenderId, new List<NetworkingStream>());

				rpcBuffer[stream.RealSenderId].Add(clonedStream);
			}

			writeStream.SetProtocolType(stream.ProtocolType);

			if (!Networking.IsBareMetal)
				writeStream.Prepare(this, stream.identifierType, stream.NetworkedBehavior, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, stream.RealSenderId);
			else
				writeStream.PrepareFinal(this, stream.identifierType, stream.NetworkedBehaviorId, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, stream.RealSenderId);

			// Write what was read to all the clients
			Write(writeStream);
		}

		protected void ServerBufferRPC(uint updateidentifier, NetworkingStream stream)
		{
			if (stream.Receivers != NetworkReceivers.AllBuffered && stream.Receivers != NetworkReceivers.OthersBuffered)
				return;

			if (!ReferenceEquals(stream.NetworkedBehavior, null) && !stream.NetworkedBehavior.IsClearedForBuffer)
			{
				stream.NetworkedBehavior.ResetBufferClear();
				return;
			}

			if (stream.identifierType == NetworkingStream.IdentifierType.RPC && stream.BufferedRPC)
			{
#if BMS_DEBUGGING_UNITY
				UnityEngine.Debug.Log("[NetWorker ServerBuffering RPC]");
#endif
				if (!udpRpcBuffer.ContainsKey(stream.RealSenderId))
					udpRpcBuffer.Add(stream.RealSenderId, new List<KeyValuePair<uint, NetworkingStream>>());

				NetworkingStream clonedStream = new NetworkingStream(stream.ProtocolType);
				clonedStream.Bytes.BlockCopy(stream.Bytes.byteArr, stream.Bytes.StartIndex(), stream.Bytes.Size);
				clonedStream.AssignSender(Me, stream.NetworkedBehavior);

				udpRpcBuffer[stream.RealSenderId].Add(new KeyValuePair<uint, NetworkingStream>(updateidentifier, clonedStream));
			}
		}

		private NetworkingStream writeStream = new NetworkingStream();
		protected void RelayStream(uint updateidentifier, NetworkingStream stream)
		{
			writeStream.SetProtocolType(stream.ProtocolType);
			if (!Networking.IsBareMetal)
				writeStream.Prepare(this, stream.identifierType, stream.NetworkedBehavior, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, stream.RealSenderId);
			else
				writeStream.PrepareFinal(this, stream.identifierType, stream.NetworkedBehaviorId, stream.Bytes, stream.Receivers, stream.BufferedRPC, stream.Customidentifier, stream.RealSenderId);

			Write(updateidentifier, writeStream);
		}

		protected void UpdateNewPlayer(NetworkingPlayer player)
		{
			if (alreadyUpdated.Contains(player))
				return;

			alreadyUpdated.Add(player);

			if (rpcBuffer.Count > 0)
			{
				foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
				{
					foreach (NetworkingStream stream in kv.Value)
					{
						Write(player, stream);
					}
				}
			}

			if (udpRpcBuffer.Count > 0)
			{
				foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
				{
					foreach (KeyValuePair<uint, NetworkingStream> stream in kv.Value)
					{
						Write(stream.Key, player, stream.Value, true);
					}
				}
			}
		}

		protected void CleanRPCForPlayer(NetworkingPlayer player)
		{
			if (rpcBuffer.ContainsKey(player.NetworkId))
				rpcBuffer.Remove(player.NetworkId);
		}

		protected void CleanUDPRPCForPlayer(NetworkingPlayer player)
		{
			if (udpRpcBuffer.ContainsKey(player.NetworkId))
				udpRpcBuffer.Remove(player.NetworkId);
		}

		/// <summary>
		/// Executes a custom read on an id, player and a stream
		/// </summary>
		/// <param name="id">The id of this read</param>
		/// <param name="player">The player to call this read</param>
		/// <param name="stream">The stream to pass the read to</param>
		public void ExecuteCustomRead(string id, NetworkingPlayer player, NetworkingStream stream)
		{
			customDataRead[id](player, stream);
		}

		/// <summary>
		/// Assign a unique id to this NetWorker(Socket)
		/// </summary>
		/// <param name="id">Unique ID to assign with</param>
		public void AssignUniqueId(ulong id)
		{
			Uniqueidentifier = id;
		}

		/// <summary>
		/// Removes a buffered ID from the networker
		/// </summary>
		/// <param name="id"></param>
		public bool ClearBufferedInstantiateFromID(ulong id)
		{
			bool removedSuccessfull = false;
			if (IsServer)
			{
				ulong key = 0;
				int x = 0;
				byte[] uniqueID = new byte[sizeof(ulong)];
				BMSByte streamBytes = null;
				int unique = -1;
				string methodName = string.Empty;
				ulong networkID = 0;

				if (this is CrossPlatformUDP)
				{
					if (udpRpcBuffer.Count > 0)
					{
						foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
						{
							x = 0;
							foreach (KeyValuePair<uint, NetworkingStream> stream in kv.Value)
							{
								if (!ReferenceEquals(stream.Value.NetworkedBehavior, NetworkingManager.Instance))
									continue;

								streamBytes = stream.Value.Bytes;

								for (int i = 0; i < sizeof(int); ++i)
									uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.STREAM_UNIQUE_ID + i];

								unique = BitConverter.ToInt32(uniqueID, 0);

								if (NetworkingManager.Instance.RPCs.ContainsKey(unique))
								{
									methodName = NetworkingManager.Instance.RPCs[unique].Name;

									if (methodName == NetworkingStreamRPC.INSTANTIATE_METHOD_NAME)
									{
										for (int i = 0; i < uniqueID.Length; ++i)
											uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.NETWORKING_UNIQUE_ID + i];

										networkID = BitConverter.ToUInt64(uniqueID, 0);

										if (networkID == id)
										{
											removedSuccessfull = true;
											key = kv.Key;
											break;
										}
									}
								}

								if (removedSuccessfull)
									break;

								x++;
							}

							if (removedSuccessfull)
								break;
						}

						if (removedSuccessfull)
						{
							//Successfully removed the instantiate from the buffer
#if NETWORKING_DEBUG_BUFFER
							string debugText = "UDP BUFFER\n=================\nBefore:\n";
							foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
							{
								debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";
							}
							debugText += "Remove Key[" + key + "] x[" + x + "]\n";
#endif
							udpRpcBuffer[key].RemoveAt(x);
							for (int i = 0; i < udpRpcBuffer[key].Count; ++i)
							{
								if (udpRpcBuffer[key][i].Value.NetworkedBehavior.NetworkedId == id)
									udpRpcBuffer[key].RemoveAt(i--);
							}
#if NETWORKING_DEBUG_BUFFER
							debugText += "=================\nAfter:\n";
							foreach (KeyValuePair<ulong, List<KeyValuePair<uint, NetworkingStream>>> kv in udpRpcBuffer)
							{
								debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";
							}
							UnityEngine.Debug.Log(debugText);
#endif
						}
					}
				}
				else
				{
					if (rpcBuffer.Count > 0)
					{
						foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
						{
							x = 0;

							foreach (NetworkingStream stream in kv.Value)
							{
								if (!ReferenceEquals(stream.NetworkedBehavior, NetworkingManager.Instance))
									continue;

								streamBytes = stream.Bytes;

								for (int i = 0; i < sizeof(int); ++i)
									uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.STREAM_UNIQUE_ID + i];

								unique = BitConverter.ToInt32(uniqueID, 0);

								if (NetworkingManager.Instance.RPCs.ContainsKey(unique))
								{
									methodName = NetworkingManager.Instance.RPCs[unique].Name;

									if (methodName == NetworkingStreamRPC.INSTANTIATE_METHOD_NAME)
									{
										for (int i = 0; i < uniqueID.Length; ++i)
											uniqueID[i] = streamBytes.byteArr[NetworkingStreamRPC.NETWORKING_UNIQUE_ID + i];

										networkID = BitConverter.ToUInt64(uniqueID, 0);

										if (networkID == id)
										{
											removedSuccessfull = true;
											key = kv.Key;
											break;
										}
									}
								}

								if (removedSuccessfull)
									break;

								x++;
							}

							if (removedSuccessfull)
								break;
						}

						if (removedSuccessfull)
						{
							//Successfully removed the instantiate from the buffer
#if NETWORKING_DEBUG_BUFFER
							string debugText = "RPC BUFFER\n=================\nBefore:\n";
							foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
							{
								debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";								
							}
							debugText += "Remove Key[" + key + "] x[" + x + "]\n";
#endif
							rpcBuffer[key].RemoveAt(x);
							for (int i = 0; i < rpcBuffer[key].Count; ++i)
							{
								if (rpcBuffer[key][i].NetworkedBehavior.NetworkedId == id)
									rpcBuffer[key].RemoveAt(i--);
							}
#if NETWORKING_DEBUG_BUFFER
							debugText += "=================\nAfter:\n";
							foreach (KeyValuePair<ulong, List<NetworkingStream>> kv in rpcBuffer)
							{
								debugText += "id[" + kv.Key + "] count [" + kv.Value.Count + "]\n";
							}
							UnityEngine.Debug.Log(debugText);
#endif
						}
					}
				}
			}

			return removedSuccessfull;
		}

		public double PreviousServerPing { get; protected set; }						// Milliseconds
		protected DateTime previousPingTime;
		protected bool sendNewPing = true;

		public delegate void PingEvent(string ipAndPort);
		public event PingEvent pingEvent
		{
			add
			{
				pingEventInvoker += value;
			}
			remove
			{
				pingEventInvoker -= value;
			}
		} PingEvent pingEventInvoker;

		public void Ping(object endpoint = null, object overrideHost = null)
		{
#if NETFX_CORE
			IPEndPointWinRT overridedHost = (IPEndPointWinRT)overrideHost;
#else
			IPEndPoint overridedHost = (IPEndPoint)overrideHost;
#endif

			if (IsServer && endpoint == null)
			{
#if !NETFX_CORE
				if (overrideHost == null)
#endif
					return;
			}

			byte[] ping = new byte[1] { 3 };
			try
			{
				if (overridedHost != null)
					Send(ping, ping.Length, overridedHost);
				else
				{
					if (IsServer)
						Send(ping, ping.Length, endpoint);
					else
						Send(ping, ping.Length, hostEndpoint);
				}
			}
			catch
			{
				if (!IsServer)
					Disconnect();
			}
		}

		public void ExecutedPing()
		{
			PreviousServerPing = (DateTime.Now - previousPingTime).TotalMilliseconds;
			sendNewPing = true;
		}

		public virtual void ProcessReliableUDPRawMessage(BMSByte rawBuffer) { }

		protected BMSByte rawBuffer = new BMSByte();
		public virtual bool ProcessReceivedData(NetworkingPlayer sender, BMSByte bytes, byte startByte, string endpoint = "", Action<NetworkingPlayer> cacheUpdate = null)
		{
			if (bytes.Size == 0)
			{
				if (bytes.byteArr[0] == 3)
				{
					if (IsServer)
					{
						// TODO:  Implement ping for WinRT
						if (pingEventInvoker != null && !string.IsNullOrEmpty(endpoint))
							pingEventInvoker(endpoint);

						if (sender == null)
							Ping(new IPEndPoint(IPAddress.Parse(endpoint.Split('+')[0]), int.Parse(endpoint.Split('+')[1])));
						else
							Ping(sender.SocketEndpoint);
					}
					else
						ExecutedPing();

					return true;
				}

				if (IsServer)
					Send(new byte[1] { 1 }, 1, groupEP);

				return true;
			}

			if (startByte != 0)
			{
				if (!IsServer)
					sender = server;
				else if (sender != null)
					sender.Ping();

				rawBuffer.Clone(bytes);

				switch (rawBuffer.byteArr[0])
				{
					case 1:	// User raw write
						OnRawDataRead(sender, rawBuffer);
						break;
					case 2:	// Scene load raw write
						// The server is never told what scene to load
						if (IsServer) return true;
						string sceneName = rawBuffer.GetString(1);
						BeardedManStudios.Network.Unity.MainThreadManager.Run(() => { UnityEngine.Application.LoadLevel(sceneName); });
						break;
					case 3:	// Cache request
						if (IsServer)
						{
							if (sender != null)
								Cache.NetworkRead(rawBuffer, sender);
						}
						else
							Cache.NetworkRead(rawBuffer);
						break;
					case 4:	// Nat registration request
						if (IsServer)
						{
							if (!ReferenceEquals(ForgeMasterServer.Instance, null))
							{
								if (rawBuffer.byteArr[1] == 1)
								{
#if !NETFX_CORE
									string[] parts = endpoint.Split('+');
									ushort internalPort = System.BitConverter.ToUInt16(rawBuffer.byteArr, 2);
									ForgeMasterServer.Instance.RegisterNatRequest(parts[0], ushort.Parse(parts[1]), internalPort);
#endif
								}
								else if (rawBuffer.byteArr[1] == 2)
								{
									string[] parts = endpoint.Split('+');
									ushort internalPort = System.BitConverter.ToUInt16(rawBuffer.byteArr, 2);
									ushort targetPort = System.BitConverter.ToUInt16(rawBuffer.byteArr, 4);
									string targetHost = Encryptor.Encoding.GetString(rawBuffer.byteArr, 6, rawBuffer.byteArr.Length - 7);

									ForgeMasterServer.Instance.PullNatRequest(parts[0], ushort.Parse(parts[1]), internalPort, targetHost, targetPort);
								}
							}
							else
							{

#if !NETFX_CORE
								if (rawBuffer.byteArr[1] == 3)
								{
									ushort targetPort = System.BitConverter.ToUInt16(rawBuffer.byteArr, 2);
									string targetHost = Encryptor.Encoding.GetString(rawBuffer.byteArr, 4, rawBuffer.byteArr.Length - 4);

									IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(targetHost), targetPort);

									Send(new byte[] { 4, 4, 0 }, 3, targetEndpoint);
								}
#endif
							}

							return true;
						}
						break;
					case 5:
						if (cacheUpdate != null)
							cacheUpdate(sender);
						break;
					case 6: // Set the message group for this client
						if (IsServer)
						{
							if (sender != null)
								sender.SetMessageGroup(System.BitConverter.ToUInt16(rawBuffer.byteArr, 1));
						}
						break;
					case 7: // Dynamic command
						string command = rawBuffer.GetString(1);

						if (command == CLIENT_READY_DYNAMIC_COMMAND && clientReadyInvoker != null)
							clientReadyInvoker(sender);

						if (dynamicCommands.ContainsKey(command))
						{
							foreach (Action<NetworkingPlayer> callback in dynamicCommands[command])
								callback(sender);
						}
						break;
				}

				return true;
			}

			return false;
		}

		public void BanPlayer(ulong playerId, int minutes)
		{
			NetworkingPlayer player = null;

			try { player = Players.First(p => p.NetworkId == playerId); }
			catch { throw new NetworkException("Could not find the player with id " + playerId); }

			BanPlayer(player, minutes);
		}

		public void BanPlayer(NetworkingPlayer player, int minutes)
		{
			banList.Add(player.Ip.Split('+')[0], (DateTime.Now + new TimeSpan(0, minutes, 0)));
			Disconnect(player, "Server has banned you for " + minutes + " minutes");
		}
	}
}