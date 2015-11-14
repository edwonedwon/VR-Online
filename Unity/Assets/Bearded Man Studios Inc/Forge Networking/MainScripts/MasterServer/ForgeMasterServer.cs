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

using System;
using System.Linq;

#if !NETFX_CORE
using System.Net;
using System.Threading;
#endif
using System.Collections.Generic;

namespace BeardedManStudios.Network
{
	public class ForgeMasterServer : MonoBehaviour
	{
		private const string REGISTER_SERVER = "BMS_INTERNAL_Register_Server";
		private const string UNREGISTER_SERVER = "BMS_INTERNAL_UnRegister_Server";
		private const string UPDATE_SERVER = "BMS_INTERNAL_Update_Server";
		private const string GET_HOSTS = "BMS_INTERNAL_Get_Hosts";
		public const int COUNT_PER_PAGE = 15;

		public const ushort PORT = 15939;																		// Port number

		public static ForgeMasterServer Instance { get; private set; }

		private static Action<HostInfo[]> requestHostsCallback = null;

		private int playerCount = 1024;																			// Maximum player count -- excluding this server

		private CrossPlatformUDP socket = null;																	// The initial connection socket

		private List<HostInfo> hosts = new List<HostInfo>();
#if !NETFX_CORE
		private Dictionary<string, Dictionary<ushort, IPEndPoint>> natHosts = new Dictionary<string, Dictionary<ushort, IPEndPoint>>();
#endif

		public static bool natComplete = false;

#if !NETFX_CORE
		private Thread pingThread = null;
#endif

		private int sleepTime = 2500;
		private int timeoutTime = 30000;

		public static string MasterServerIp { get; private set; }
		public static void SetIp(string ip)
		{
			MasterServerIp = ip;
		}

#if !NETFX_CORE
		private void PingHosts()
		{
			while (true)
			{
				for (int i = 0; i < hosts.Count; i++)
				{
					if ((DateTime.Now - hosts[i].lastPing).TotalMilliseconds > timeoutTime)
						hosts.RemoveAt(i--);
				}

				Thread.Sleep(sleepTime);
			}
		}
#endif

		private void PingRecieved(string ip)
		{
			try
			{
				var host = hosts.First(h => h.IpAddress + "+" + h.port == ip);

				if (host != null)
					host.lastPing = DateTime.Now;
			}
			catch
			{

			}
		}

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		private void Start()
		{
			StartServer();
			Networking.PrimarySocket.AddCustomDataReadEvent(REGISTER_SERVER, RegisterServerRequest);
			Networking.PrimarySocket.AddCustomDataReadEvent(UNREGISTER_SERVER, UnRegisterServerRequest);
			Networking.PrimarySocket.AddCustomDataReadEvent(UPDATE_SERVER, UpdateServerRequest);
			Networking.PrimarySocket.AddCustomDataReadEvent(GET_HOSTS, GetHostsRequestToServer);

			((CrossPlatformUDP)Networking.PrimarySocket).pingEvent += PingRecieved;

#if !NETFX_CORE
			pingThread = new Thread(PingHosts);
			pingThread.Start();
#endif
		}

		private static void Request(string host, Action<NetWorker> call)
		{
			if (Networking.Sockets != null && Networking.Sockets.ContainsKey(PORT))
				Networking.Disconnect(PORT);

			NetWorker socket = Networking.Connect(host, PORT, Networking.TransportationProtocolType.UDP);
			socket.MasterServerFlag = true;

			socket.AddCustomDataReadEvent(REGISTER_SERVER, null);
			socket.AddCustomDataReadEvent(UNREGISTER_SERVER, null);
			socket.AddCustomDataReadEvent(UPDATE_SERVER, null);
			socket.AddCustomDataReadEvent(GET_HOSTS, GetHostsRequestToClient);

			if (socket.Connected)
				call(socket);
			else
				socket.connected += delegate() { call(socket); };
		}

		/// <summary>
		/// This method is used to register a server to the master server to be retreived by clients
		/// </summary>
		/// <param name="host">The master server host address</param>
		/// <param name="port">The port that this server will be running on</param>
		/// <param name="maxPlayers">The maximum amount of players allowed on this server</param>
		/// <param name="name">The name for this server</param>
		/// <param name="gameType">The type of game for this server (ie. deathmatch, capture the flag, etc.)</param>
		/// <param name="comment">The comment for this server (usually user populated for loading screens)</param>
		/// <param name="password">The password for this server</param>
		/// <param name="sceneName">The scene name that this server is currently on</param>
		public static void RegisterServer(string host, ushort port, int maxPlayers, string name, string gameType = "", string comment = "", string password = "", string sceneName = "")
		{
			Action<NetWorker> call = delegate(NetWorker socket)
			{
				BMSByte data = new BMSByte();
				ObjectMapper.MapBytes(data, port, maxPlayers, name, gameType, comment, password, sceneName);
				Networking.WriteCustom(REGISTER_SERVER, socket, data, true, NetworkReceivers.Server);
			};

			Request(host, call);
		}

		/// <summary>
		/// This method is used to manually unregister a server.  If a server aborts the master server will automatically remove it
		/// </summary>
		/// <param name="host">The host of the master server</param>
		/// <param name="port">The port that this server was running on</param>
		public static void UnRegisterServer(string host, ushort port)
		{
			Action<NetWorker> call = delegate(NetWorker socket)
			{
				BMSByte data = new BMSByte();
				ObjectMapper.MapBytes(data, port);
				Networking.WriteCustom(UNREGISTER_SERVER, socket, data, true, NetworkReceivers.Server);
			};

			Request(host, call);
		}

		private void RegisterServerRequest(NetworkingPlayer sender, NetworkingStream stream)
		{
			ushort port = ObjectMapper.Map<ushort>(stream);
			int maxPlayers = ObjectMapper.Map<int>(stream);
			string name = ObjectMapper.Map<string>(stream);
			string gameType = ObjectMapper.Map<string>(stream);
			string comment = ObjectMapper.Map<string>(stream);
			string password = ObjectMapper.Map<string>(stream);
			string sceneName = ObjectMapper.Map<string>(stream);

			HostInfo host = null;

			try { host = hosts.First(h => h.IpAddress == sender.Ip.Split('+')[0] && h.port == port); }
			catch { }

			if (host == null)
			{
				hosts.Add(new HostInfo() { ipAddress = sender.Ip, port = port, maxPlayers = maxPlayers, name = name, gameType = gameType, comment = comment, password = password, sceneName = sceneName, lastPing = DateTime.Now });
				Debug.Log("Registered a new server " + sender.Ip.Split('+')[0] + ":" + port);
			}
			else
			{
				host.name = name;
				host.maxPlayers = maxPlayers;
				host.gameType = gameType;
				host.comment = comment;
				host.password = password;
				host.sceneName = sceneName;
				host.lastPing = DateTime.Now;
				Debug.Log("Updated the registration of a server " + host.IpAddress+ ":" + host.port);
			}

			socket.Disconnect(sender, "Register Complete");
		}

		private void UnRegisterServerRequest(NetworkingPlayer sender, NetworkingStream stream)
		{
			ushort port = ObjectMapper.Map<ushort>(stream);

			for (int i = 0; i < hosts.Count; i++)
			{
				if (hosts[i].ipAddress == sender.Ip && hosts[i].port == port)
				{
					hosts.RemoveAt(i);
					break;
				}
			}

			socket.Disconnect(sender, "UnRegister Complete");
		}

		// TODO:  Support updating the master server on scene change to "Application.loadedLevelName"

		/// <summary>
		/// This method is used to update the current player count on the master server for this server
		/// </summary>
		/// <param name="host">The host of the master server</param>
		/// <param name="port">The port number that this server is running on</param>
		/// <param name="playerCount">The current player count for this server</param>
		public static void UpdateServer(string host, ushort port, int playerCount)
		{
			Action<NetWorker> call = delegate(NetWorker socket)
			{
				BMSByte data = new BMSByte();
				ObjectMapper.MapBytes(data, port, playerCount);
				Networking.WriteCustom(UPDATE_SERVER, socket, data, true, NetworkReceivers.Server);
			};

			Request(host, call);
		}

		private void UpdateServerRequest(NetworkingPlayer sender, NetworkingStream stream)
		{
			ushort port = ObjectMapper.Map<ushort>(stream);

			HostInfo host = null;

			try { host = hosts.First(h => h.IpAddress == sender.Ip.Split('+')[0] && h.port == port); }
			catch { }

			if (host == null)
			{
				socket.Disconnect(sender, "Host not found");
				return;
			}

			host.connectedPlayers = ObjectMapper.Map<int>(stream);
			socket.Disconnect(sender, "Update Complete");
			Debug.Log("Updated a server " + host.IpAddress + ":" + host.port);
		}

		/// <summary>
		/// This method requests all of the hosts from the master server
		/// </summary>
		/// <param name="host">The host of the master server</param>
		/// <param name="pageNumber">This is the page number (starting from 0). So if you want to get entries 0-n then you will pass 0, if you want n-n+n then pass 1</param>
		/// <param name="callback">This is the method that will be called once the master server responds with the host list</param>
		public static void GetHosts(string host, ushort pageNumber, Action<HostInfo[]> callback)
		{
			requestHostsCallback = callback;

			Action<NetWorker> call = delegate(NetWorker socket)
			{
				BMSByte data = new BMSByte();
				ObjectMapper.MapBytes(data, pageNumber);
				Networking.WriteCustom(GET_HOSTS, socket, data, true, NetworkReceivers.Server);
			};

			Request(host, call);
		}

		private void GetHostsRequestToServer(NetworkingPlayer sender, NetworkingStream stream)
		{
			ushort pageNumber = ObjectMapper.Map<ushort>(stream);

			List<HostInfo> subList = new List<HostInfo>();
			for (int i = pageNumber * COUNT_PER_PAGE; i < COUNT_PER_PAGE; i++)
			{
				if (hosts.Count <= i)
					break;

				subList.Add(hosts[i]);
			}

			BMSByte data = new BMSByte();
			ObjectMapper.MapBytes(data, subList.Count);

			foreach (HostInfo host in hosts)
				ObjectMapper.MapBytes(data, host.IpAddress, host.port, host.maxPlayers, host.name, host.password, host.gameType, host.connectedPlayers, host.comment, host.sceneName);

			Networking.WriteCustom(GET_HOSTS, socket, data, sender, true);
		}

		private static void GetHostsRequestToClient(NetworkingPlayer sender, NetworkingStream stream)
		{
			int count = ObjectMapper.Map<int>(stream);

			List<HostInfo> hostList = new List<HostInfo>();
			for (int i = 0; i < count; i++)
			{
				hostList.Add(new HostInfo()
				{
					ipAddress = ObjectMapper.Map<string>(stream),
					port = ObjectMapper.Map<ushort>(stream),
					maxPlayers = ObjectMapper.Map<int>(stream),
					name = ObjectMapper.Map<string>(stream),
					password = ObjectMapper.Map<string>(stream),
					gameType = ObjectMapper.Map<string>(stream),
					connectedPlayers = ObjectMapper.Map<int>(stream),
					comment = ObjectMapper.Map<string>(stream),
					sceneName = ObjectMapper.Map<string>(stream)
				});
			}

			Networking.Disconnect(PORT);
			requestHostsCallback(hostList.ToArray());
		}

		/// <summary>
		/// This method is called when the host server button is clicked
		/// </summary>
		private void StartServer()
		{
			// Create a host connection
			socket = Networking.Host(PORT, Networking.TransportationProtocolType.UDP, playerCount, false) as CrossPlatformUDP;
			Networking.SetPrimarySocket(socket);
		}

#if !NETFX_CORE
		private void OnApplicationQuit()
		{
			pingThread.Abort();
		}
#endif

#if !NETFX_CORE
		public void RegisterNatRequest(string ip, ushort port, ushort internalPort)
		{
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ip), port);

			if (!natHosts.ContainsKey(ip))
				natHosts.Add(ip, new Dictionary<ushort, IPEndPoint>());

			if (!natHosts[ip].ContainsKey(port))
				natHosts[ip].Add(internalPort, endpoint);

			byte[] data = new byte[] { 4, 4 };
			socket.ReadClient.Send(data, data.Length, endpoint);
		}
#endif

		/// <summary>
		/// This method is used on the server to attempt to connect through the NAT hole punch server to the client
		/// </summary>
		/// <param name="ip">This is the ip address of the NAT hole punch server</param>
		/// <param name="port">This is the port number of the NAT hole punch server</param>
		/// <param name="internalPort">This is the internal port of the client that this server is trying to communicate with</param>
		/// <param name="requestHost">This is the client host address that this server is trying to communicate with</param>
		/// <param name="requestPort">This is the client port number that this server is trying to communicate with</param>
		public void PullNatRequest(string ip, ushort port, ushort internalPort, string requestHost, ushort requestPort)
		{
#if !NETFX_CORE
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ip), port);

			if (!natHosts.ContainsKey(requestHost))
			{
				foreach (string	key in natHosts.Keys)
				{
					Debug.Log(key);
				}

				byte[] data = new byte[] { 4, 4, 0 };
				socket.ReadClient.Send(data, data.Length, endpoint);
				return;
			}

			if (!natHosts[requestHost].ContainsKey(requestPort))
			{
				foreach (ushort key in natHosts[requestHost].Keys)
				{
					Debug.Log(key);
				}

				byte[] data = new byte[] { 4, 4, 0 };
				socket.ReadClient.Send(data, data.Length, endpoint);
				return;
			}

			IPEndPoint targetEndpoint = natHosts[requestHost][requestPort];

			List<byte> sendData = new List<byte>(new byte[] { 4, 4, 3 });
			sendData.AddRange(System.BitConverter.GetBytes((ushort)targetEndpoint.Port));
			sendData.AddRange(Encryptor.Encoding.GetBytes(targetEndpoint.Address.ToString()));
			socket.ReadClient.Send(sendData.ToArray(), sendData.Count, endpoint);

			sendData = new List<byte>(new byte[] { 4, 4, 3 });
			sendData.AddRange(System.BitConverter.GetBytes(port));
			sendData.AddRange(Encryptor.Encoding.GetBytes(ip));
			socket.ReadClient.Send(sendData.ToArray(), sendData.Count, targetEndpoint);
#endif
		}

		/// <summary>
		/// This method is used on the client to attempt to connect to the server through the NAT hole punch server
		/// </summary>
		/// <param name="socket">This is the socket that is being used for the communication with the server</param>
		/// <param name="port">This is the port number that this client is bound to</param>
		/// <param name="requestHost">This is the host address of the server that this client is trying to connect to</param>
		/// <param name="requestPort">This is the host port of the server that this client is trying to connect to</param>
		/// <param name="proxyHost">This is the NAT hole punch server host address</param>
		/// <param name="proxyPort">This is the NAT hole punch server port number</param>
		/// <returns></returns>
		public static bool RequestNat(CrossPlatformUDP socket, ushort port, string requestHost, ushort requestPort, string proxyHost, ushort proxyPort = PORT)
		{
#if !NETFX_CORE
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(proxyHost), proxyPort);

			List<byte> data = new List<byte>(new byte[] { 4, 4, 2 });
			data.AddRange(BitConverter.GetBytes(port));
			data.AddRange(BitConverter.GetBytes(requestPort));
			data.AddRange(Encryptor.Encoding.GetBytes(requestHost));

			try
			{
				int tryCount = 10;
				while (socket.ReadClient.Available == 0)
				{
					socket.ReadClient.Send(data.ToArray(), data.Count, endpoint);
					Thread.Sleep(500);

					if (--tryCount <= 0)
						throw new Exception("Unable to contact proxy host");
				}

				string endpointStr = "";
				BMSByte otherBytes = socket.ReadClient.Receive(ref endpoint, ref endpointStr);

				BMSByte found = new BMSByte();
				found.Clone(otherBytes);

				if (found.byteArr[2] == 0)
					return false;

				ushort targetPort = System.BitConverter.ToUInt16(found.byteArr, 3);
				string targetHost = Encryptor.Encoding.GetString(found.byteArr, 5, found.byteArr.Length - 6);

				IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(targetHost), targetPort);

				tryCount = 20;
				while (socket.ReadClient.Available == 0)
				{
					socket.ReadClient.Send(new byte[] { 4, 4, 0 }, 3, targetEndpoint);
					Thread.Sleep(500);

					if (--tryCount <= 0)
						throw new Exception("Unable to contact proxy host");
				}

#if UNITY_EDITOR
				Debug.Log("Connected via NAT traversal");
#endif
			}
#if UNITY_EDITOR
			catch (Exception e)
			{
				Debug.LogException(e);
			}
#else
			catch { }
#endif
#endif

			return true;
		}

		/// <summary>
		/// This method is used to allow a server to register itself with the NAT hole punching server
		/// </summary>
		/// <param name="socket">This is the socket that is being used for the communication with the clients</param>
		/// <param name="port">The port number that is being used for this server</param>
		/// <param name="proxyHost">The ip address of the the NAT hole punching (router) server</param>
		/// <param name="proxyPort">The port number for the NAT hole punch server</param>
		public static void RegisterNat(CrossPlatformUDP socket, ushort port, string proxyHost, ushort proxyPort = PORT)
		{
#if !NETFX_CORE
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(proxyHost), proxyPort);

			List<byte> data = new List<byte>(new byte[] { 4, 4, 1 });
			data.AddRange(BitConverter.GetBytes(port));

			try
			{
				int tryCount = 10;
				while (socket.ReadClient.Available == 0)
				{
					socket.ReadClient.Send(data.ToArray(), data.Count, endpoint);
					Thread.Sleep(500);

					if (--tryCount <= 0)
						throw new Exception("Unable to contact proxy host");
				}

#if UNITY_EDITOR
				Debug.Log("The hole punching registration for this server is complete");
#endif
			}
#if UNITY_EDITOR
			catch (Exception e)
			{
				Debug.LogException(e);
			}
#else
			catch { }
#endif
#endif
		}
	}
}