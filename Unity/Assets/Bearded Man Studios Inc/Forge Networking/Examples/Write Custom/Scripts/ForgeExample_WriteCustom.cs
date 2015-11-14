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

public class ForgeExample_WriteCustom : MonoBehaviour
{
	public string id = "MyClassThing_01";

	public int num = 0;
	public bool buul = false;
	public float money = 3.1f;
	public string first = "brent";
	public double big = 0.0000004;
	public Vector2 v2 = new Vector2(5, 13);
	public Vector3 v3 = new Vector3(9, 133, 123);
	public Vector4 v4 = new Vector4(1, 3, 34);

	private BMSByte cachedData = new BMSByte();

	private void Start()
	{
		Debug.Log("Registering read callback event");
		Networking.PrimarySocket.AddCustomDataReadEvent(id, ReadFromNetwork);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			num = 9;
			buul = true;
			money = 100.53f;
			first = "Farris";
			big = 214324.3533;
			v2 = Vector2.one * 5;
			v3 = Vector3.one * 9;
			v4 = Vector4.one * 2.31f;

			cachedData.Clone(Serialize());
			Networking.WriteCustom(id, Networking.PrimarySocket, cachedData, true);
			Debug.Log("WriteCustom");
		}
	}

	private BMSByte Serialize()
	{
		return ObjectMapper.MapBytes(cachedData, num, buul, money, first, big, v2, v3, v4);
	}

	private void ReadFromNetwork(NetworkingPlayer sender, NetworkingStream stream)
	{
		Debug.Log("Reading");
		Deserialize(stream);
	}

	private void Deserialize(NetworkingStream stream)
	{
		num = ObjectMapper.Map<int>(stream);
		buul = ObjectMapper.Map<bool>(stream);
		money = ObjectMapper.Map<float>(stream);
		first = ObjectMapper.Map<string>(stream);
		big = ObjectMapper.Map<double>(stream);
		v2 = ObjectMapper.Map<Vector2>(stream);
		v3 = ObjectMapper.Map<Vector3>(stream);
		v4 = ObjectMapper.Map<Vector4>(stream);
	}
}