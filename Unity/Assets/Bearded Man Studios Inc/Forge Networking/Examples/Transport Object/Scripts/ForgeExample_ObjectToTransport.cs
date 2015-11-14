using UnityEngine;
using System.Collections;

using BeardedManStudios.Network;

public class ForgeExample_ObjectToTransport : ForgeTransportObject
{
	public int apple = 0;
	public float brent = 9.93f;
	public string cat = "cat";
	public bool dog = false;

	public ForgeExample_ObjectToTransport()
		: base()
	{

	}

	public override string ToString()
	{
		return "apple: " + apple.ToString() + "\n" +
			"brent: " + brent.ToString() + "\n" +
			"cat: " + cat + "\n" +
			"dog: " + dog.ToString();
	}
}