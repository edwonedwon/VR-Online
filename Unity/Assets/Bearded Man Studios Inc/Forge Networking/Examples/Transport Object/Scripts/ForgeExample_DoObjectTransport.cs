using UnityEngine;
using System.Collections;
using BeardedManStudios.Network;

public class ForgeExample_DoObjectTransport : MonoBehaviour
{
	public ForgeExample_ObjectToTransport transportObject = new ForgeExample_ObjectToTransport();

	private void Start()
	{
		transportObject.transportFinished += TransportComplete;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			transportObject.apple = 9;
			transportObject.brent = 3.14f;
			transportObject.cat = "hat";
			transportObject.dog = true;
			transportObject.Send();
		}
	}

	private void TransportComplete(ForgeTransportObject target)
	{
		Debug.Log("The transport has completed");
		Debug.Log(target.ToString());
	}
}