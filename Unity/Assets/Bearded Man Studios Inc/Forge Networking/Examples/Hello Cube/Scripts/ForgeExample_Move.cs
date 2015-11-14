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
using System.Collections.Generic;

namespace BeardedManStudios.Forge.Examples
{
	public class ForgeExample_Move : NetworkedMonoBehavior
	{
		[NetSync("Print", NetworkCallers.Everyone, NetSync.Interpolate.False)]
		public int number = 0;

		[NetSync("Print", NetworkCallers.Everyone)]
		public float floatNumber = 0.0f;

		[NetSync]
		public string cat = string.Empty;

		private int testNum = 0;

		public TextMesh nameMesh = null;

		private string SERVER_ONLY = "test";
		private BMSByte cachedData = new BMSByte();

		[NetSync(NetSync.Interpolate.False)]
		public float dontLerp = 0.0f;

		public void Print()
		{
			Debug.Log("The int is " + number + " and the float is " + floatNumber);
		}

		protected override void Awake()
		{
			base.Awake();

			Networking.PrimarySocket.AddCustomDataReadEvent(SERVER_ONLY, ReadFromNetwork);
		}

		protected override void Start()
		{
			// Always call the base Start method
			base.Start();

			if (!IsOwner)
				return;

			// Read raw data from the network
			OwningNetWorker.rawDataRead += OwningNetWorker_rawDataRead;
		}

		protected override void NetworkStart()
		{
			base.NetworkStart();

			if (OwningNetWorker.IsServer) // Fired whenever a client says that they are ready on the network
				OwningNetWorker.clientReady += (player) => { Debug.Log("The player with id " + player.NetworkId + " is ready to go!"); };
			else // Tell the server that we are ready on the network, this can be called at any time
				Networking.ClientReady(OwningNetWorker);

			enteredProximity += (mine, other) => { Debug.Log(other.name + " entered my (" + mine.name + ") proximity"); };
			exitedProximity += (mine, other) => { Debug.Log(other.name + " left my (" + mine.name + ") proximity"); };
		}

		protected override void NetworkInitialized()
		{
			base.NetworkInitialized();
			Debug.Log("The variables have been initially replicated across the network");
		}

		private void OwningNetWorker_rawDataRead(NetworkingPlayer sender, BMSByte data)
		{
			// In this test we are just writing a string across the network
			string message = System.Text.Encoding.UTF8.GetString(data.byteArr, data.StartIndex(), data.Size);

			Debug.Log("Hello " + message);
		}

		protected override void OwnerUpdate()
		{
			base.OwnerUpdate();

			if (Input.GetKeyDown(KeyCode.B))
				AssignName("Brent");
			else if (Input.GetKeyDown(KeyCode.F))
				AssignName("Farris");

			if (Input.GetKey(KeyCode.UpArrow))
				transform.position += Vector3.up * 5.0f * Time.deltaTime;

			if (Input.GetKey(KeyCode.DownArrow))
				transform.position += Vector3.down * 5.0f * Time.deltaTime;

			if (Input.GetKey(KeyCode.LeftArrow))
				transform.position += Vector3.left * 5.0f * Time.deltaTime;

			if (Input.GetKey(KeyCode.RightArrow))
				transform.position += Vector3.right * 5.0f * Time.deltaTime;

			if (Input.GetKeyDown(KeyCode.T))
			{
				cachedData = ServerSerialze(testNum++);
				Networking.WriteCustom(SERVER_ONLY, Networking.PrimarySocket, cachedData, true, NetworkReceivers.Server);
			}

			if (Input.GetKeyDown(KeyCode.Space))
			{
				number++;
				cat += "Cat";
			}
			else if (Input.GetKeyDown(KeyCode.V))
				floatNumber += 50.35f;

			if (Input.GetKeyDown(KeyCode.S))
				Cache.Set<int>("test", 9);

			if (Input.GetKeyDown(KeyCode.G))
			{
				Cache.Request<int>("test", (object x) =>
				{
					Debug.Log(x);
				});
			}

			if (OwningNetWorker.IsServer && Input.GetKeyDown(KeyCode.N))
			{
				Networking.ChangeClientScene(OwningNetWorker, "ForgeWriteCustom");
				Application.LoadLevel("ForgeWriteCustom");
			}
			else if (OwningNetWorker.IsServer && Input.GetKeyDown(KeyCode.M))
			{
				Networking.ChangeClientScene(OwningNetWorker, "ForgeHelloCubeResources");
				Application.LoadLevel("ForgeHelloCubeResources");
			}

			if (Input.GetKeyDown(KeyCode.L))
				dontLerp += 5.35f;

			if (Input.GetKeyDown(KeyCode.C))
				RPC("MessageGroupTest", NetworkReceivers.MessageGroup, (byte)9);

			if (Input.GetKeyDown(KeyCode.Alpha0))
			{
				Debug.Log("Setting message group to 0");
				Networking.SetMyMessageGroup(OwningNetWorker, (ushort)0);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				Debug.Log("Setting message group to 1");
				Networking.SetMyMessageGroup(OwningNetWorker, (ushort)1);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				Debug.Log("Setting message group to 2");
				Networking.SetMyMessageGroup(OwningNetWorker, (ushort)2);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				Debug.Log("Setting message group to 3");
				Networking.SetMyMessageGroup(OwningNetWorker, (ushort)3);
			}
		}

		private void AssignName(string newName)
		{
			NetworkingManager.Instance.SetName(newName);
			RPC("UpdatePlayers");
		}

		[BRPC]
		private void UpdatePlayers()
		{
			NetworkingManager.Instance.PollPlayerList(GetPlayers);
		}

		private void GetPlayers(List<NetworkingPlayer> players)
		{
			CallOnMainThread((object[] args) =>
			{
				// Note:  This is not optimal, it is just for the idea
				foreach (KeyValuePair<ulong, SimpleNetworkedMonoBehavior> kv in networkedBehaviors)
				{
					if (kv.Value is ForgeExample_Move)
					{
						foreach (NetworkingPlayer player in players)
						{
							if (kv.Value.OwnerId == player.NetworkId)
							{
								((ForgeExample_Move)kv.Value).nameMesh.text = !string.IsNullOrEmpty(player.Name) ? player.Name : "Client";
								break;
							}
						}
					}
				}
			}, null);
		}

		private void OnGUI()
		{
			if (!IsOwner)
				return;

			GUILayout.Space(25);

			// The server NetworkingManager object controls how fast the client's times are updated
			GUILayout.Label("The current server time is: " + NetworkingManager.Instance.ServerTime);
			GUILayout.Label("Press B or F to assign your name across the network.");
			GUILayout.Label("This will also updated get the latest list of players");

			GUILayout.Label("Bytes In: " + OwningNetWorker.BandwidthIn);
			GUILayout.Label("Bytes Out: " + OwningNetWorker.BandwidthOut);

			GUILayout.Label("Kilobytes In: " + (OwningNetWorker.BandwidthIn / 1024.0f));
			GUILayout.Label("Kilobytes Out: " + (OwningNetWorker.BandwidthOut / 1024.0f));

			GUILayout.Label("Megabytes In: " + (OwningNetWorker.BandwidthIn / 1024.0f / 1024.0f));
			GUILayout.Label("Megabytes Out: " + (OwningNetWorker.BandwidthOut / 1024.0f / 1024.0f));

			GUILayout.Label("Current Frame: " + NetworkingManager.Instance.CurrentFrame);
		}

		private BMSByte ServerSerialze(int num)
		{
			cachedData.Clear();
			return ObjectMapper.MapBytes(cachedData, "hello world from the server - " + num);
		}

		private void ReadFromNetwork(NetworkingPlayer sender, NetworkingStream stream)
		{
			ServerDeserialize(stream);
		}

		private void ServerDeserialize(NetworkingStream stream)
		{
			string recievedResponse = ObjectMapper.Map<string>(stream);
			Debug.Log("Received response: " + recievedResponse);
		}

		protected override void EnteredProximity()
		{
			base.EnteredProximity();
			this.GetComponent<Renderer>().enabled = true;
		}

		protected override void ExitedProximity()
		{
			base.ExitedProximity();
			this.GetComponent<Renderer>().enabled = false;
		}

		[BRPC]
		private void MessageGroupTest(byte number)
		{
			Debug.Log("You have received the number " + number + " because you are in the message group " + OwningNetWorker.Me.MessageGroup);
		}
	}
}