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
using System.Collections.Generic;

namespace BeardedManStudios.Network.Unity
{
	public class MainThreadManager : MonoBehaviour
	{
		/// <summary>
		/// The singleton instance of the Main Thread Manager
		/// </summary>
		public static MainThreadManager Instance { get; private set; }

		/// <summary>
		/// This will create a main thread manager if one is not already created
		/// </summary>
		public static void Create()
		{
			if (Instance != null)
				return;

			Instance = new GameObject("MAIN_THREAD_MANAGER").AddComponent<MainThreadManager>();
			DontDestroyOnLoad(Instance.gameObject);
		}

		/// <summary>
		/// A list of functions to run
		/// </summary>
		private static List<Action> mainThreadActions = new List<Action>();

		/// <summary>
		/// A mutex to be used to prevent threads from overriding each others logic
		/// </summary>
		private static object mutex = new System.Object();

		// Setup the singleton in the Awake
		private void Awake()
		{
			// If an instance already exists then delete this copy
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			// Assign the static reference to this object
			Instance = this;

			// This object should move through scenes
			DontDestroyOnLoad(gameObject);
		}
		
		/// <summary>
		/// Add a function to the list of functions to call on the main thread via the Update function
		/// </summary>
		/// <param name="action">The method that is to be run on the main thread</param>
		public static void Run(Action action)
		{
			// Make sure to lock the mutex so that we don't override
			// other threads actions
			lock (mutex)
			{
				mainThreadActions.Add(action);
			}
		}

		private void Update()
		{
			// If there are any functions in the list, then run
			// them all and then clear the list
			if (mainThreadActions.Count > 0)
			{
				// Ditto the last lock bro'
				lock (mutex)
				{
					foreach (Action action in mainThreadActions)
						action();

					mainThreadActions.Clear();
				}
			}
		}
	}
}