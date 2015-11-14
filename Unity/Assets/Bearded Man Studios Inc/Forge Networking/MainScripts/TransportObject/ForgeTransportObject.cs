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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BeardedManStudios.Network
{
	public class ForgeTransportObject
	{
		public delegate void TransportFinished(ForgeTransportObject target);

		public event TransportFinished transportFinished
		{
			add
			{
				transportFinishedInvoker += value;
			}
			remove
			{
				transportFinishedInvoker -= value;
			}
		}
		TransportFinished transportFinishedInvoker;

#if NETFX_CORE
		IEnumerable<FieldInfo> fields;
#else
		FieldInfo[] fields;
#endif

		private static ulong currentId = 0;
		private ulong id = 0;
		private string identifier = "BMS_INTERNAL_TransportObject_";
		private object serializerMutex = new Object();
		private BMSByte serializer = new BMSByte();

		public ForgeTransportObject()
		{
			id = currentId++;
			identifier += id.ToString();
			Initialize();
		}

		public ForgeTransportObject(string id)
		{
			identifier += id;
			Initialize();
		}

		private void Initialize()
		{
			if (Networking.PrimarySocket == null)
				return;

			Networking.PrimarySocket.AddCustomDataReadEvent(identifier, ReadFromNetwork);

#if NETFX_CORE
			fields = this.GetType().GetRuntimeFields();
#else
			fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
		}

		public void Send(NetworkReceivers receivers = NetworkReceivers.Others, bool reliable = true)
		{
			lock (serializerMutex)
			{
				serializer.Clear();
				foreach (FieldInfo field in fields)
					ObjectMapper.MapBytes(serializer, field.GetValue(this));

				Networking.WriteCustom(identifier, Networking.PrimarySocket, serializer, reliable, receivers);
			}
		}

		private void ReadFromNetwork(NetworkingPlayer sender, NetworkingStream stream)
		{
			Deserialize(stream);
		}

		private void Deserialize(NetworkingStream stream)
		{
			lock (serializerMutex)
			{
				foreach (FieldInfo field in fields)
					field.SetValue(this, ObjectMapper.Map(field.FieldType, stream));

				if (transportFinishedInvoker != null)
					transportFinishedInvoker(this);
			}
		}
	}
}