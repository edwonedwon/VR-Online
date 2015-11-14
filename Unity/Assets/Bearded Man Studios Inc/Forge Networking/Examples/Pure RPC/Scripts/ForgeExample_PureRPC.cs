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
	public class ForgeExample_PureRPC : SimpleNetworkedMonoBehavior
	{
		[BRPC]
		private void Move(Vector3 movement)
		{
			transform.position += movement;
		}

		[BRPC]
		private void ByteData(byte[] test)
		{
			Debug.Log(Encryptor.Encoding.GetString(test, 0, test.Length));
		}

		[BRPC]
		private void BMSByteObject(BMSByte bmsByte)
		{
			Debug.Log(bmsByte.GetString(0));
		}

		protected override void Update()
		{
			base.Update();

			if (!IsOwner)
				return;

			if (Input.GetKeyDown(KeyCode.Space))
				RPC("Move", NetworkReceivers.All, Vector3.up);
			else if (Input.GetKeyDown(KeyCode.A))
				RPC("ByteData", NetworkReceivers.All, Encryptor.Encoding.GetBytes("Hello World Byte Array!"));
			else if (Input.GetKeyDown(KeyCode.B))
				RPC("BMSByteObject", NetworkReceivers.All, ObjectMapper.MapBytes(new BMSByte(), "Hello World BMSByte!"));
		}
	}
}