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

namespace BeardedManStudios.Network
{
	/// <summary>
	/// This class is responsible for holding references to variables that are going to be serialized across the network
	/// </summary>
	/// <typeparam name="T">The type of variable that is being tracked</typeparam>
	sealed class NetRef<T>
	{
		/// <summary>
		/// The method for getting the value of the target object
		/// </summary>
		private readonly Func<T> getter;

		/// <summary>
		/// The method for setting the value of the target object
		/// </summary>
		private readonly Action<T> setter;

		/// <summary>
		/// The callback to be executed when the value of the target object has changed
		/// </summary>
		public readonly Action callback;

		/// <summary>
		/// The type of callers that will be allowed to invoke the callback when the value has changed
		/// </summary>
		public readonly NetworkCallers callbackCallers;

		/// <summary>
		/// Create an instance NetRef
		/// </summary>
		/// <param name="getter">The getter function for the variable</param>
		/// <param name="setter">The setter function for the variable</param>
		/// <param name="callback">The optional callback to be executed when the replication has complete</param>
		/// <param name="callbackCallers">The group of callers that will execute the callback method</param>
		public NetRef(Func<T> getter, Action<T> setter, Action callback, NetworkCallers callbackCallers, bool ignoreInterpolation = false)
		{
			this.getter = getter;
			this.setter = setter;
			this.callback = callback;
			this.IgnoreLerp = ignoreInterpolation;
			this.callbackCallers = callbackCallers;

			PreviousValue = getter();
		}

		/// <summary>
		/// Determines if this property ignores interpolation (even if it is turned on)
		/// </summary>
		public bool IgnoreLerp { get; private set; }

		/// <summary>
		/// The object to lerp the value to
		/// </summary>
		public T LerpTo { get; private set; }

		/// <summary>
		/// The speed of the lerp for this object
		/// </summary>
		public float LerpT { get; set; }

		/// <summary>
		/// Determines if the object is currenly lerping to its destination value
		/// </summary>
		public bool Lerping { get; private set; }

		/// <summary>
		/// The previous value for this object
		/// </summary>
		public T PreviousValue { get; private set; }

		/// <summary>
		/// The current value of the target object
		/// </summary>
		public T Value { get { return getter(); } set { PreviousValue = value; setter(value); } }

		/// <summary>
		/// Used to determine if the value has changed at all or is not currently its target value
		/// </summary>
		public bool IsDirty { get { return !PreviousValue.Equals(Value); } }

		/// <summary>
		/// Used to setup the lerp for this object
		/// </summary>
		/// <param name="to">The target object to lerp to</param>
		public void Lerp(T to)
		{
			if (!Value.Equals(to))
				Lerping = true;

			LerpTo = to;
			PreviousValue = to;
		}

		/// <summary>
		/// Finalizes the setting of the object's value
		/// </summary>
		public void Clean() { PreviousValue = Value; Lerping = false; }

		/// <summary>
		/// Forcefully assign the value to the lerp destination object
		/// </summary>
		public void AssignToLerp()
		{
			Value = LerpTo;
			Lerping = false;
		}

		public bool Assign(T val)
		{
			Lerping = false;

			if (!Equals(PreviousValue, val))
			{
				Value = val;
				return true;
			}

			Value = val;
			return false;
		}

		/// <summary>
		/// Call the callback that is attached to this value change
		/// </summary>
		/// <param name="sender">The object that has the value that is being watched</param>
		public void Callback(NetworkedMonoBehavior sender, bool overrideDirty = false)
		{
			// Only execute if the value has changed
			if (callback == null || (!IsDirty && !overrideDirty))
				return;

			Unity.MainThreadManager.Run(() =>
			{
				switch (callbackCallers)
				{
					case NetworkCallers.Everyone:
						callback();
						break;
					case NetworkCallers.Others:
						if (!sender.IsOwner)
							callback();
						break;
					case NetworkCallers.Owner:
						if (sender.IsOwner)
							callback();
						break;
				}
			});
		}
	}
}