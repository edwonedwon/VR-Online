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



using System.Net;
using System.Threading;

namespace BeardedManStudios.Network
{
	class ForgeMasterServerPing
	{
#if NETFX_CORE
		public ForgeMasterServerPing(CrossPlatformUDP currentSocket) { }
		public void Disconnect() { }
#else
		private Thread masterServerPing;
		private IPEndPoint masterServerEndpoint;
		private CrossPlatformUDP socket = null;

		public int sleepTime = 10000;

		public ForgeMasterServerPing(CrossPlatformUDP currentSocket)
		{
			// TODO:  Throw a master server exception
			if (string.IsNullOrEmpty(ForgeMasterServer.MasterServerIp))
				throw new NetworkException("The master server ip has not been assigned with the ForgeMasterServer.SetIp static method.");

			socket = currentSocket;
			masterServerEndpoint = new IPEndPoint(IPAddress.Parse(ForgeMasterServer.MasterServerIp), ForgeMasterServer.PORT);
			masterServerPing = new Thread(PingHostThread);
			masterServerPing.Start();
		}

		private void PingHostThread()
		{
			while (true)
			{
				socket.Ping(null, masterServerEndpoint);
				Thread.Sleep(sleepTime);
			}
		}

		public void Disconnect()
		{
			masterServerPing.Abort();
		}
#endif
	}
}