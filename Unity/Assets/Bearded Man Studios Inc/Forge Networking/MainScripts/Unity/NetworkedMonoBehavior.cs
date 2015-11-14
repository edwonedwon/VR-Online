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

#if NETFX_CORE
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace BeardedManStudios.Network
{
	/// <summary>
	/// This is a class that can serialize class variables across the network
	/// </summary>
	[AddComponentMenu("Forge Networking/Networked MonoBehavior")]
	public class NetworkedMonoBehavior : SimpleNetworkedMonoBehavior
	{
		/// <summary>
		/// A delegate for execuing input events from the client on the server
		/// </summary>
		/// <param name="keyCode">The input key that was pressed</param>
		/// <param name="frame">The frame that this was requested on</param>
		protected delegate void InputRequest(KeyCode keyCode, int frame);

		/// <summary>
		/// A delegate for execuing input events from the client on the server
		/// </summary>
		/// <param name="keyCode">The input key that was pressed</param>
		protected delegate void FramelessInputRequest(KeyCode keyCode);

		/// <summary>
		/// A delegate for execuing mouse input events from the client on the server
		/// </summary>
		/// <param name="buttonIndex">The index of the button that was pressed</param>
		/// <param name="frame">The frame that this was requested on</param>
		protected delegate void MouseInputRequest(int buttonIndex, int frame);

		/// <summary>
		/// A delegate for execuing mouse input events from the client on the server
		/// </summary>
		/// <param name="buttonIndex">The index of the button that was pressed</param>
		protected delegate void FramelessMouseInputRequest(int buttonIndex);

		public delegate void ProximityEvent(NetworkedMonoBehavior myPlayer, NetworkedMonoBehavior otherPlayer);

		public event ProximityEvent enteredProximity
		{
			add
			{
				enteredProximityInvoker += value;
			}
			remove
			{
				enteredProximityInvoker -= value;
			}
		}
		ProximityEvent enteredProximityInvoker;

		public event ProximityEvent exitedProximity
		{
			add
			{
				exitedProximityInvoker += value;
			}
			remove
			{
				exitedProximityInvoker -= value;
			}
		}
		ProximityEvent exitedProximityInvoker;

		public List<ulong> proximityBehaviors = new List<ulong>();
		
		/// <summary>
		/// Attribute for easily replicating properties and fields across the network
		/// </summary>
		protected class NetSync : Attribute
		{
			/// <summary>
			/// Just to make it more visual rather than just a boolean
			/// </summary>
			public enum Interpolate
			{
				True,
				False
			}

			/// <summary>
			/// The name of the method to execute when the value has changed
			/// </summary>
			public string method;

			/// <summary>
			/// Used to determine who is to call the callback method when the value has changed
			/// </summary>
			public NetworkCallers callers;

			/// <summary>
			/// This is uesd to determine if this value ignores interpolation
			/// </summary>
			public Interpolate interpolate;

			public NetSync()
			{
				this.method = string.Empty;
				this.callers = NetworkCallers.Everyone;
				this.interpolate = Interpolate.True;
			}

			public NetSync(Interpolate ignoreInterpolation)
			{
				this.interpolate = ignoreInterpolation;
			}

			public NetSync(string method, NetworkCallers callers)
			{
				this.method = method;
				this.callers = callers;
				this.interpolate = Interpolate.True;
			}

			public NetSync(string method, NetworkCallers callers, Interpolate ignoreInterpolation)
			{
				this.method = method;
				this.callers = callers;
				this.interpolate = ignoreInterpolation;
			}
		}

		protected sealed class ManualNetSync : NetSync { }

		/// <summary>
		/// An enum for easy visual serialization of properties
		/// </summary>
		public enum SerializeVector3Properties
		{
			None,
			X,
			Y,
			Z,
			XY,
			XZ,
			YZ,
			XYZ
		}

		/// <summary>
		/// Used to determine what fields to serialize for the position
		/// </summary>
		[HideInInspector]
		public SerializeVector3Properties serializePosition = SerializeVector3Properties.XYZ;

		/// <summary>
		/// Used to determine if the position should be independantly lerped
		/// </summary>
		[HideInInspector]
		public bool lerpPosition = true;

		/// <summary>
		/// Used to determine what fields to serialize for the rotation
		/// </summary>
		[HideInInspector]
		public SerializeVector3Properties serializeRotation = SerializeVector3Properties.XYZ;

		/// <summary>
		/// Used to determine if the rotation should be independantly lerped
		/// </summary>
		[HideInInspector]
		public bool lerpRotation = true;

		/// <summary>
		/// Used to determine what fields to serialize for the scale
		/// </summary>
		[HideInInspector]
		public SerializeVector3Properties serializeScale = SerializeVector3Properties.None;

		/// <summary>
		/// Used to determine if the scale should be independantly lerped
		/// </summary>
		[HideInInspector]
		public bool lerpScale = true;

		/// <summary>
		/// If this is a reliable NetworkedMonoBehavior object
		/// </summary>
		[HideInInspector]
		public bool isReliable = false;

		/// <summary>
		/// If you want to Interpolate the values across the network for smooth movement
		/// </summary>
		[HideInInspector]
		public bool interpolateFloatingValues = true;

		/// <summary>
		/// The lerp time it will take
		/// </summary>
		[HideInInspector]
		public float lerpT = 0.25f;

		/// <summary>
		/// The cutoff point to when it will stop lerping
		/// </summary>
		[HideInInspector]
		public float lerpStopOffset = 0.01f;

		/// <summary>
		/// The cutoff point for when it will stop lerping
		/// </summary>
		[HideInInspector]
		public float lerpAngleStopOffset = 1.0f;

		/// <summary>
		/// The delay it takes to send to the server
		/// </summary>
		[HideInInspector]
		public float networkTimeDelay = 0.1f;

		/// <summary>
		/// The current time for the delay counter
		/// </summary>
		private float timeDelayCounter = 0;

		/// <summary>
		/// A list of all of the properties that are to be serialized across the network
		/// </summary>
		private List<NetRef<object>> Properties = new List<NetRef<object>>();

		/// <summary>
		/// A list of all of the properties that are to be manually serialized across the network
		/// </summary>
		private List<NetRef<object>> ManualProperties = new List<NetRef<object>>();
		private string ManualPropertiesId { get { return "BMS_INTERNAL_ManualProperties_" + NetworkedId; } }

		/// <summary>
		/// Get whether this is a player or not
		/// </summary>
		[HideInInspector]
		public bool isPlayer = false;

		/// <summary>
		/// If this is true, then a network destroy will be called on disconnect
		/// </summary>
		[HideInInspector]
		public bool destroyOnDisconnect = false;

		/// <summary>
		/// When this is true, the client can only send inputs to the server via the Request() method
		/// </summary>
		[HideInInspector]
		public bool serverIsAuthority = false;

		/// <summary>
		/// Used with <see cref="serverIsAuthority"/> in order to simulate inputs on the cilent side
		/// </summary>
		[HideInInspector]
		public bool clientSidePrediction = false;

		/// <summary>
		/// This is the maximum distance offset that the player can be from the server before it is syncronized
		/// </summary>
		public float authoritativeSyncDistance = 0.25f;

		/// <summary>
		/// This is the maximum rotation in degrees that the player can be from the server before it is syncronized
		/// </summary>
		public float authoritativeSyncRotation = 5.0f;

		/// <summary>
		/// An event that is fired on the server when an input down was requested from a client
		/// </summary>
		protected event InputRequest inputDownRequest = null;

		/// <summary>
		/// An event that is fired on the server when an input up was requested from a client
		/// </summary>
		protected event InputRequest inputUpRequest = null;

		/// <summary>
		/// An event that is fired every update while in between a input down request and an input up request
		/// </summary>
		protected event FramelessInputRequest inputRequest = null;

		/// <summary>
		/// An event that is fired on the server when a mouse input down was requested from a client
		/// </summary>
		protected event MouseInputRequest mouseDownRequest = null;

		/// <summary>
		/// An event that is fired on the server when a mouse input up was requested from a client
		/// </summary>
		protected event MouseInputRequest mouseUpRequest = null;

		/// <summary>
		/// An event that is fired every update while in between a mouse down request and an mouse up request
		/// </summary>
		protected event FramelessMouseInputRequest mouseRequest = null;

		private List<KeyCode> currentKeys = new List<KeyCode>();
		private List<int> mouseIndices = new List<int>();
		private List<int> keyUpBuffer = new List<int>();
		private List<int> mouseUpBuffer = new List<int>();

		public static NetworkedMonoBehavior MyPlayer { get; private set; }

		private NetworkingPlayer serverTargetPlayer = null;

		/// <summary>
		/// A cached object that is constantly updated which is added here to optimize garbage collection
		/// </summary>
		private object valueGetter = null;

		/// <summary>
		/// The primary writing stream for this object to send data across the network
		/// </summary>
		private NetworkingStream writeStream = new NetworkingStream();

		/// <summary>
		/// Used for lerping from the previous position to the new position
		/// </summary>
		private Vector3 previousPosition = Vector3.zero;

		/// <summary>
		/// Used for lerping from the previous rotation to the new rotation
		/// </summary>
		private Quaternion previousRotation = Quaternion.identity;

		/// <summary>
		/// Used for lerping from the previous scale to the new scale
		/// </summary>
		private Vector3 previousScale = Vector3.zero;

		/// <summary>
		/// The new destination position for this object as described from the network
		/// </summary>
		private Vector3 targetPosition = Vector3.zero;

		/// <summary>
		/// The new rotation for this object as described from the network
		/// </summary>
		private Vector3 targetRotation = Vector3.zero;

		/// <summary>
		/// The new scale for this object as described from the network
		/// </summary>
		private Vector3 targetScale = Vector3.zero;

		/// <summary>
		/// Used for converting the target rotation (Vector3) into a Quaternion
		/// </summary>
		private Quaternion convertedTargetRotation = Quaternion.identity;

		/// <summary>
		/// Determines if this object has already serialized for its new set of values
		/// </summary>
		protected bool HasSerialized { get; private set; }

		private string myUniqueId = "BMS_INTERNAL_Properties_";

		/// <summary>
		/// If true then this object will teleport to where it is as soon as a client connects
		/// </summary>
		[HideInInspector]
		public bool teleportToInitialPositions = true;
		
		public bool DataInitialized
		{
			get
			{
				return teleportToInitialPositions == false;
			}
			set
			{
				if (!teleportToInitialPositions)
					return;

				teleportToInitialPositions = !value;

				if (value)
					NetworkInitialized();
			}
		}

		protected virtual void NetworkInitialized()
		{

		}

		/// <summary>
		/// Determines if the collider has already been turned off for this object
		/// </summary>
		private bool turnedOffCollider = false;

		/// <summary>
		/// Unity5 Reference to the rigidbody
		/// </summary>
		protected Rigidbody rigidbodyRef = null;

		/// <summary>
		/// Unity5 Reference to the collider
		/// </summary>
		protected Collider colliderRef = null;

		/// <summary>
		/// The main byte buffer for serialization (sending across the network)
		/// </summary>
		private BMSByte serializedBuffer = new BMSByte();

		private delegate object GetValueDelegate(object obj);
		private delegate void SetValueDelegate(object obj, object value);

		/// <summary>
		/// Locate a NetworkedMonoBehavior with a given ID
		/// </summary>
		/// <param name="id">ID of the NetworkedMonoBehavior</param>
		/// <returns></returns>
		new public static NetworkedMonoBehavior Locate(ulong id)
		{
			// TODO:  Check if it is null or not

			if (networkedBehaviors.ContainsKey(id))
				return (NetworkedMonoBehavior)networkedBehaviors[id];

			return null;
		}

		protected override void Awake()
		{
			rigidbodyRef = GetComponent<Rigidbody>();
			colliderRef = GetComponent<Collider>();

			if (colliderRef != null && colliderRef.enabled)
			{
				colliderRef.enabled = false;
				turnedOffCollider = true;
			}

			base.Awake();

#if NETFX_CORE
			// Get all of the fields for this class
			List<FieldInfo> fields = this.GetType().GetRuntimeFields().OrderBy(x => x.Name).ToList();
			// Get all of the properties for this class
			List<PropertyInfo> properties = this.GetType().GetRuntimeProperties().OrderBy(x => x.Name).ToList();
#else
			// Get all of the fields for this class
			FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
			// Get all of the properties for this class
			PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
#endif

			// Go throug all of the found fields and find any that are to be synced across the network
#if NETFX_CORE
			for (int i = 0; i < fields.Count; i++)
#else
			for (int i = 0; i < fields.Length; i++)
#endif
			{
				// If this field has a [NetSync] attribute then add it to the variables to be synced
				NetSync[] netSyncs = fields[i].GetCustomAttributes(typeof(NetSync), true) as NetSync[];
				if (netSyncs.Length != 0)
				{
					// Create a temporary reference to this particular object to be used
					FieldInfo field = fields[i];
					GetValueDelegate get = (GetValueDelegate)Delegate.CreateDelegate(typeof(GetValueDelegate), field, "GetValue");
					SetValueDelegate set = (SetValueDelegate)Delegate.CreateDelegate(typeof(SetValueDelegate), field, typeof(FieldInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) }));
					AddNetworkVariable(() => get(this), x => set(this, x), netSyncs[0], netSyncs[0] is ManualNetSync);
				}
			}

			// Go throug all of the found propperties and find any that are to be synced across the network
#if NETFX_CORE
			for (int i = 0; i < properties.Count; i++)
#else
			for (int i = 0; i < properties.Length; i++)
#endif
			{
				// If this property has a [NetSync] attribute then add it to the variables to be synced
#if NETFX_CORE
				if (properties[i].GetCustomAttribute<NetSync>() != null)
#else
				if (properties[i].GetCustomAttributes(typeof(NetSync), true).Length != 0)
#endif
				{
					// Make sure that the property is read and writeable otherwise there is no reason for it to sync
					if (!properties[i].CanWrite || !properties[i].CanRead)
						throw new NetworkException("Properties marked with the [NetSync] attribute must be readable and writeable");

					// Create a temporary reference to this particular object to be used
					PropertyInfo property = properties[i];
					AddNetworkVariable(() => property.GetValue(this, null), x => property.SetValue(this, x, null));
				}
			}
		}

		protected override void Start()
		{
			base.Start();

			previousPosition = transform.position;
			previousRotation = transform.rotation;
			previousScale = transform.localScale;

			targetPosition = transform.position;
			targetRotation = transform.eulerAngles;
			targetScale = transform.localScale;
		}

		/// <summary>
		/// Setup this NetworkedMonoBehavior with the owner of this object along with the networked ID
		/// </summary>
		/// <param name="owningSocket">The socket that owns this object</param>
		/// <param name="isOwner">Is this the owner of this object</param>
		/// <param name="networkId">Network ID of who owns it</param>
		/// <param name="ownerId">The network identifyer for the player who owns this object</param>
		public override void Setup(NetWorker owningSocket, bool isOwner, ulong networkId, ulong ownerId)
		{
			base.Setup(owningSocket, isOwner, networkId, ownerId);

			bool foundServerAuthority = false, clientPrediction = false;

			foreach (NetworkedMonoBehavior behavior in GetComponents<NetworkedMonoBehavior>())
			{
				if (behavior.serverIsAuthority)
				{
					foundServerAuthority = true;
					clientPrediction = behavior.clientSidePrediction;
					break;
				}
			}

			if (rigidbodyRef != null)
			{
				if ((!OwningNetWorker.IsServer && foundServerAuthority && !clientPrediction) || (!IsOwner && !foundServerAuthority))
				{
					rigidbodyRef.constraints = RigidbodyConstraints.FreezeAll;
					rigidbodyRef.useGravity = false;
				}
			}

			if (isPlayer && OwningNetWorker.IsServer)
				serverTargetPlayer = OwningPlayer;

			if (turnedOffCollider)
			{
				if ((OwningNetWorker.IsServer && foundServerAuthority) || (IsOwner && !foundServerAuthority))
				{
					turnedOffCollider = false;
					colliderRef.enabled = true;
				}
			}
		}

		[BRPC]
		protected void InitializeObject()
		{
			if (!OwningNetWorker.IsServer)
				return;

			AutoritativeSerialize();
		}

		protected override void NetworkStart()
		{
			base.NetworkStart();

			// Statically assign the player to be this object to have access to it globally
			if (IsOwner && isPlayer && MyPlayer == null)
				MyPlayer = this;

			OwningNetWorker.AddCustomDataReadEvent(ManualPropertiesId, DeserializeManualProperties);
			myUniqueId += NetworkedId.ToString();

			RPC("InitializeObject", NetworkReceivers.Server);
		}

		private BMSByte manualPropertyBytes = new BMSByte();
		public void SerializeManualProperties(bool reliable = false, NetworkReceivers receivers = NetworkReceivers.All)
		{
			if (ManualProperties == null || ManualProperties.Count == 0 || !IsSetup)
				return;

			manualPropertyBytes.Clear();
			foreach (NetRef<object> obj in ManualProperties)
			{
				ObjectMapper.MapBytes(manualPropertyBytes, obj.Value);
				obj.Callback(this);
			}

			Networking.WriteCustom(ManualPropertiesId, OwningNetWorker, manualPropertyBytes, reliable, receivers);
		}

		private void DeserializeManualProperties(NetworkingPlayer player, NetworkingStream stream)
		{
			foreach (NetRef<object> obj in ManualProperties)
			{
				if (!interpolateFloatingValues || obj.IgnoreLerp)
				{
					if (obj.Assign(ObjectMapper.Map(obj.Value.GetType(), stream)))
						obj.Callback(this, true);
				}
				else
					obj.Lerp(ObjectMapper.Map(obj.Value.GetType(), stream));
			}
		}

		/// <summary>
		/// Add a network variable to the NetworkedMonoBehavior to use
		/// </summary>
		/// <param name="getter">Variable to get</param>
		/// <param name="setter">Variable to set</param>
		protected void AddNetworkVariable(Func<object> getter, Action<object> setter, NetSync netSync = null, bool manualProperty = false)
		{
			if (IsSetup)
				throw new NetworkException(6, "Network variables can not be added after the Awake method of this MonoBehaviour");

			Action callback = null;
			NetworkCallers callers = NetworkCallers.Everyone;
			NetSync.Interpolate useInterpolation = NetSync.Interpolate.True;

			if (netSync != null)
			{
				if (!string.IsNullOrEmpty(netSync.method))
				{
#if NETFX_CORE
					callback = () => { this.GetType().GetRuntimeMethod(netSync.method, null).Invoke(this, new object[] { }); };
#else
					callback = (Action)Delegate.CreateDelegate(typeof(Action), this, netSync.method);
#endif
				}

				callers = netSync.callers;
				useInterpolation = netSync.interpolate;
			}

			if (manualProperty)
				ManualProperties.Add(new NetRef<object>(getter, setter, callback, callers, useInterpolation == NetSync.Interpolate.False));
			else
				Properties.Add(new NetRef<object>(getter, setter, callback, callers, useInterpolation == NetSync.Interpolate.False));
		}

		private void UpdateValues()
		{
			previousPosition = transform.position;
			previousRotation = transform.rotation;
			previousScale = transform.localScale;

			if (Properties == null)
				return;

			foreach (NetRef<object> property in Properties)
				property.Clean();
		}

		[BRPC]
		protected void KeyDownRequest(int keyCode, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (currentKeys.Contains((KeyCode)keyCode))
				return;

			currentKeys.Add((KeyCode)keyCode);

			if (inputDownRequest != null)
				inputDownRequest((KeyCode)keyCode, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The input key " + ((KeyCode)keyCode).ToString() + " was requested from the client but no input request inputDownRequest has not been assigned");
#endif

			if (keyUpBuffer.Contains(keyCode))
			{
				KeyUpRequest(keyCode, frame);
				keyUpBuffer.Remove(keyCode);
			}
		}

		[BRPC]
		protected void KeyUpRequest(int keyCode, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (!currentKeys.Contains((KeyCode)keyCode))
			{
				keyUpBuffer.Add(keyCode);
				return;
			}

			if (inputUpRequest != null)
				inputUpRequest((KeyCode)keyCode, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The input key " + ((KeyCode)keyCode).ToString() + " was requested from the client but no input request inputUpRequest has not been assigned");
#endif

			currentKeys.Remove((KeyCode)keyCode);
		}

		[BRPC]
		protected void MouseDownRequest(int index, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (mouseIndices.Contains(index))
				return;

			mouseIndices.Add(index);

			if (mouseDownRequest != null)
				mouseDownRequest(index, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The input key " + index.ToString() + " was requested from the client but no mouse input request mouseDownRequest has not been assigned");
#endif

			if (mouseUpBuffer.Contains(index))
			{
				MouseUpRequest(index, frame);
				mouseUpBuffer.Remove(index);
			}
		}

		[BRPC]
		protected void MouseUpRequest(int index, int frame)
		{
			if (!OwningNetWorker.IsServer && !clientSidePrediction)
				return;

			if (!mouseIndices.Contains(index))
			{
				mouseUpBuffer.Add(index);
				return;
			}

			if (mouseUpRequest != null)
				mouseUpRequest(index, frame);
#if UNITY_EDITOR
			else
				Debug.LogError("The mouse index " + index.ToString() + " was requested from the client but no mouse input request mouseUpRequest has not been assigned");
#endif

			mouseIndices.Remove(index);
		}

		protected void InputCheck(KeyCode keyCode)
		{
			if (!IsOwner)
				return;

			if (Input.GetKeyDown(keyCode))
			{
				RPC("KeyDownRequest", NetworkReceivers.Server, (int)keyCode, NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					KeyDownRequest((int)keyCode, NetworkingManager.Instance.CurrentFrame);
			}
			
			if (Input.GetKeyUp(keyCode))
			{
				RPC("KeyUpRequest", NetworkReceivers.Server, (int)keyCode, NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					KeyUpRequest((int)keyCode, NetworkingManager.Instance.CurrentFrame);
			}
		}

		protected void MouseCheck(int index)
		{
			if (!IsOwner)
				return;

			if (Input.GetMouseButtonDown(index))
			{
				RPC("MouseDownRequest", NetworkReceivers.Server, index, NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					MouseDownRequest(index, NetworkingManager.Instance.CurrentFrame);
			}
			
			if (Input.GetMouseButtonUp(index))
			{
				RPC("MouseUpRequest", NetworkReceivers.Server, index, NetworkingManager.Instance.CurrentFrame);

				if (clientSidePrediction)
					MouseUpRequest(index, NetworkingManager.Instance.CurrentFrame);
			}
		}

		protected override void Update()
		{
			HasSerialized = false;

			base.Update();

#if UNITY_EDITOR
			if (!IsSetup)
				throw new NetworkException(7, "Either the base class \"Start\" or \"NetworkStart\" method is hidden (use \"protected override void Start\" instead and call base.Start() at the beginning of the function) or you currently have not established a connection.");
#endif
			if (serverIsAuthority && (OwningNetWorker.IsServer || (IsOwner && clientSidePrediction)))
			{
				if (inputRequest != null)
				{
					foreach (KeyCode key in currentKeys)
						inputRequest(key);
				}

				if (mouseRequest != null)
				{
					foreach (int button in mouseIndices)
						mouseRequest(button);
				}
			}

			if ((Properties == null || Properties.Count == 0) &&
				serializePosition == SerializeVector3Properties.None &&
				serializeRotation == SerializeVector3Properties.None &&
				serializeScale == SerializeVector3Properties.None)
			{
				OwnedUpdate();
				return;
			}

			// TODO:  Examine other parts of removal for this
			//if (isPlayer && OwningNetWorker.IsServer && serverTargetPlayer != null)
			//	serverTargetPlayer.UpdatePosition(transform.position);

			if (OwningNetWorker.IsServer && serverTargetPlayer != null)
				serverTargetPlayer.Position = transform.position;

			if ((OwningNetWorker.IsServer && serverIsAuthority) || (!serverIsAuthority && IsOwner))
			{
				if (networkTimeDelay > 0)
				{
					timeDelayCounter += Time.deltaTime;

					if (timeDelayCounter < networkTimeDelay)
					{
						OwnedUpdate();
						return;
					}

					timeDelayCounter = 0.0f;
				}

				if (Properties != null)
				{
					foreach (NetRef<object> obj in Properties)
					{
						if (obj.IsDirty)
						{
							DoSerialize();
							break;
						}
					}
				}

				if (!HasSerialized)
				{
					if ((serializePosition != SerializeVector3Properties.None && transform.position != previousPosition) ||
						(serializeRotation != SerializeVector3Properties.None && transform.rotation != previousRotation) ||
						(serializeScale != SerializeVector3Properties.None && transform.localScale != previousScale))
					{
						DoSerialize();
					}
				}
			}
			else
			{
				//if (newData.Ready)
				//	Deserialize(newData);

				if (clientSidePrediction)
				{
					if (currentKeys.Count != 0 || mouseIndices.Count != 0)
					{
						// TODO:  If the player is too far from targetPosition then fix it
						OwnedUpdate();
						return;
					}
				}

				if (serializePosition != SerializeVector3Properties.None && transform.position != targetPosition)
				{
					if (!serverIsAuthority || !clientSidePrediction || (Vector3.Distance(transform.position, targetPosition) > authoritativeSyncDistance))
					{
						if (!lerpPosition || teleportToInitialPositions)
							transform.position = targetPosition;
						else
							transform.position = Vector3.Lerp(transform.position, targetPosition, lerpT);

						if (Vector3.Distance(transform.position, targetPosition) <= lerpStopOffset)
							transform.position = targetPosition;
					}
				}

				if (serializeRotation != SerializeVector3Properties.None && transform.eulerAngles != targetRotation)
				{
					convertedTargetRotation = Quaternion.Euler(targetRotation);

					if (!serverIsAuthority || !clientSidePrediction || (Quaternion.Angle(transform.rotation, convertedTargetRotation) > authoritativeSyncRotation))
					{
						if (!lerpRotation || teleportToInitialPositions)
							transform.rotation = convertedTargetRotation;
						else
							transform.rotation = Quaternion.Slerp(transform.rotation, convertedTargetRotation, lerpT);

						if (Quaternion.Angle(transform.rotation, convertedTargetRotation) <= lerpAngleStopOffset)
							transform.rotation = convertedTargetRotation;
					}
				}

				if (serializeScale != SerializeVector3Properties.None && transform.localScale != targetScale)
				{
					if (!lerpScale || teleportToInitialPositions)
						transform.localScale = targetScale;
					else
						transform.localScale = Vector3.Lerp(transform.localScale, targetScale, lerpT);

					if (Vector3.Distance(transform.localScale, targetScale) <= lerpStopOffset)
						transform.localScale = targetScale;
				}

				foreach (NetRef<object> obj in Properties)
				{
					if (!obj.Lerping)
						continue;

					UpdateRemoteNetRef(obj);
				}

				foreach (NetRef<object> obj in ManualProperties)
				{
					if (!obj.Lerping)
						continue;

					UpdateRemoteNetRef(obj);
				}
			}

			OwnedUpdate();
		}

		private void UpdateRemoteNetRef(NetRef<object> obj)
		{
			valueGetter = obj.Value;

			bool finalize = false;

			if (valueGetter is float)
			{
				obj.Value = (float)Mathf.Lerp((float)valueGetter, (float)obj.LerpTo, lerpT);

				if (Math.Abs((float)obj.LerpTo - (float)valueGetter) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is double)
			{
				obj.Value = BeardedMath.Lerp((double)valueGetter, (double)obj.LerpTo, lerpT);

				if (Math.Abs((double)obj.LerpTo - (double)valueGetter) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Vector2)
			{
				obj.Value = Vector2.Lerp((Vector2)valueGetter, (Vector2)obj.LerpTo, lerpT);

				if (Vector2.Distance((Vector2)valueGetter, (Vector2)obj.LerpTo) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Vector3)
			{
				obj.Value = Vector3.Lerp((Vector3)valueGetter, (Vector3)obj.LerpTo, lerpT);

				if (Vector3.Distance((Vector3)valueGetter, (Vector3)obj.LerpTo) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Vector4)
			{
				obj.Value = Vector4.Lerp((Vector4)valueGetter, (Vector4)obj.LerpTo, lerpT);

				if (Vector4.Distance((Vector4)valueGetter, (Vector4)obj.LerpTo) <= lerpStopOffset)
					finalize = true;
			}
			else if (valueGetter is Quaternion)
			{
				obj.Value = Quaternion.Slerp((Quaternion)valueGetter, (Quaternion)obj.LerpTo, lerpT);

				if (Quaternion.Angle((Quaternion)valueGetter, (Quaternion)obj.LerpTo) <= lerpAngleStopOffset)
					finalize = true;
			}
			else
				finalize = true;

			if (finalize)
			{
				obj.AssignToLerp();
				obj.Callback(this, true);
			}
		}

		private void OwnedUpdate()
		{
			if (newData.Ready)
			{
				newData.Reset();
				if (turnedOffCollider) { turnedOffCollider = false; colliderRef.enabled = true; }
				if (teleportToInitialPositions) DataInitialized = true;
			}

			if (IsOwner)
				OwnerUpdate();
			else
				NonOwnerUpdate();
		}

		protected virtual void OwnerUpdate()
		{

		}

		protected virtual void NonOwnerUpdate()
		{

		}

		protected virtual void FixedUpdate()
		{
			if (IsOwner)
				OwnerFixedUpdate();
			else
				NonOwnerFixedUpdate();
		}

		protected virtual void OwnerFixedUpdate()
		{

		}

		protected virtual void NonOwnerFixedUpdate()
		{

		}

		public void AutoritativeSerialize()
		{
			if (OwningNetWorker.IsServer)
				DoSerialize(true);
		}

		private void DoSerialize(bool isAuthoritative = false)
		{
			serializedBuffer = Serialized();

			if (OwningNetWorker is CrossPlatformUDP)
			{
				writeStream.SetProtocolType(Networking.ProtocolType.UDP);
				Networking.WriteUDP(OwningNetWorker, myUniqueId, writeStream.Prepare(OwningNetWorker, NetworkingStream.IdentifierType.NetworkedBehavior, this, serializedBuffer, (isAuthoritative ?  NetworkReceivers.All : (OwningNetWorker.ProximityBasedMessaging ? NetworkReceivers.OthersProximity : NetworkReceivers.Others))), isReliable);
			}
			else
			{
				writeStream.SetProtocolType(Networking.ProtocolType.TCP);
				Networking.WriteTCP(OwningNetWorker, writeStream.Prepare(OwningNetWorker, NetworkingStream.IdentifierType.NetworkedBehavior, this, serializedBuffer, OwningNetWorker.ProximityBasedMessaging ? NetworkReceivers.OthersProximity : NetworkReceivers.Others));
			}

			HasSerialized = true;
			UpdateValues();
		}

		private void PrepareNextSerializedTransform(SerializeVector3Properties type, Vector3 value)
		{
			switch (type)
			{
				case SerializeVector3Properties.X:
					ObjectMapper.MapBytes(serializedBuffer, value.x);
					break;
				case SerializeVector3Properties.Y:
					ObjectMapper.MapBytes(serializedBuffer, value.y);
					break;
				case SerializeVector3Properties.Z:
					ObjectMapper.MapBytes(serializedBuffer, value.z);
					break;
				case SerializeVector3Properties.XY:
					ObjectMapper.MapBytes(serializedBuffer, value.x);
					ObjectMapper.MapBytes(serializedBuffer, value.y);
					break;
				case SerializeVector3Properties.XZ:
					ObjectMapper.MapBytes(serializedBuffer, value.x);
					ObjectMapper.MapBytes(serializedBuffer, value.z);
					break;
				case SerializeVector3Properties.YZ:
					ObjectMapper.MapBytes(serializedBuffer, value.y);
					ObjectMapper.MapBytes(serializedBuffer, value.z);
					break;
				case SerializeVector3Properties.XYZ:
					ObjectMapper.MapBytes(serializedBuffer, value);
					break;
				default:
					return;
			}
		}

		/// <summary>
		/// Get the serialzed version of this NetworkedMonoBehavior
		/// </summary>
		/// <returns></returns>
		public override BMSByte Serialized()
		{
			serializedBuffer.Clear();

			PrepareNextSerializedTransform(serializePosition, transform.position);

			// Sending rotation across the network as a Vector3 instead of Vector4 to save bandwidth
			PrepareNextSerializedTransform(serializeRotation, transform.eulerAngles);
			PrepareNextSerializedTransform(serializeScale, transform.localScale);

			if (Properties != null)
			{
				foreach (NetRef<object> obj in Properties)
				{
					ObjectMapper.MapBytes(serializedBuffer, obj.Value);
					obj.Callback(this);
				}
			}

			return serializedBuffer;
		}

		private NetworkingStream newData = new NetworkingStream();
		/// <summary>
		/// Prepare this to be Deserialized if it is not the owner
		/// </summary>
		/// <param name="stream">Stream of data to use</param>
		public void PrepareDeserialize(NetworkingStream stream)
		{
			if ((IsOwner && !serverIsAuthority) || (OwningNetWorker.IsServer && serverIsAuthority))
				return;

			newData.Reset();
			newData.Bytes.Clone(stream.Bytes);
			newData.ManualReady();

			Deserialize(newData);
		}

		private Vector3 GetNextSerializedTransform(SerializeVector3Properties type, NetworkingStream stream, Vector3 standard)
		{
			switch (type)
			{
				case SerializeVector3Properties.X:
					standard.x = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.Y:
					standard.y = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.Z:
					standard.z = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.XY:
					standard.x = ObjectMapper.Map<float>(stream);
					standard.y = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.XZ:
					standard.x = ObjectMapper.Map<float>(stream);
					standard.z = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.YZ:
					standard.y = ObjectMapper.Map<float>(stream);
					standard.z = ObjectMapper.Map<float>(stream);
					break;
				case SerializeVector3Properties.XYZ:
					return ObjectMapper.Map<Vector3>(stream);
			}

			return standard;
		}

		/// <summary>
		/// Only Deserialize the stream of data that is not the owner
		/// </summary>
		/// <param name="stream">Stream of data to use</param>
		public override void Deserialize(NetworkingStream stream)
		{
			if ((IsOwner && !serverIsAuthority) || (OwningNetWorker.IsServer && serverIsAuthority))
				return;

			stream.ResetByteReadIndex();

			targetPosition = GetNextSerializedTransform(serializePosition, stream, targetPosition);
			targetRotation = GetNextSerializedTransform(serializeRotation, stream, targetRotation);
			targetScale = GetNextSerializedTransform(serializeScale, stream, targetScale);

			if (Properties == null)
				return;

			foreach (NetRef<object> obj in Properties)
			{
				if (!interpolateFloatingValues || obj.IgnoreLerp)
				{
					if (obj.Assign(ObjectMapper.Map(obj.Value.GetType(), stream)))
						obj.Callback(this, true);
				}
				else
					obj.Lerp(ObjectMapper.Map(obj.Value.GetType(), stream));
			}
		}

		public override void Disconnect()
		{
			base.Disconnect();

			if (isPlayer || destroyOnDisconnect)
				Networking.Destroy(this);
		}

		[BRPC]
		protected virtual void EnteredProximity()
		{
			if (enteredProximityInvoker != null)
				enteredProximityInvoker(MyPlayer, this);
		}

		[BRPC]
		protected virtual void ExitedProximity()
		{
			if (exitedProximityInvoker != null)
				exitedProximityInvoker(MyPlayer, this);
		}

		public void ProximityInCheck(NetworkedMonoBehavior other)
		{
			if (!OwningNetWorker.IsServer)
				return;

			if (proximityBehaviors.Contains(other.NetworkedId))
				return;

			proximityBehaviors.Add(other.NetworkedId);
			other.proximityBehaviors.Add(this.NetworkedId);

			if (IsServerOwner)
				Unity.MainThreadManager.Run(other.EnteredProximity);
			else
				other.AuthoritativeRPC("EnteredProximity", OwningNetWorker, OwningPlayer, false);

			if (other.IsServerOwner)
				Unity.MainThreadManager.Run(EnteredProximity);
			else
				AuthoritativeRPC("EnteredProximity", OwningNetWorker, other.OwningPlayer, false);
		}

		public void ProximityOutCheck(NetworkedMonoBehavior other)
		{
			if (!OwningNetWorker.IsServer)
				return;

			if (!proximityBehaviors.Contains(other.NetworkedId))
				return;

			proximityBehaviors.Remove(other.NetworkedId);
			other.proximityBehaviors.Remove(this.NetworkedId);

			if (IsServerOwner)
				Unity.MainThreadManager.Run(other.ExitedProximity);
			else
				other.AuthoritativeRPC("ExitedProximity", OwningNetWorker, OwningPlayer, false);

			if (other.IsServerOwner)
				Unity.MainThreadManager.Run(ExitedProximity);
			else
				AuthoritativeRPC("ExitedProximity", OwningNetWorker, other.OwningPlayer, false);
		}
	}
}