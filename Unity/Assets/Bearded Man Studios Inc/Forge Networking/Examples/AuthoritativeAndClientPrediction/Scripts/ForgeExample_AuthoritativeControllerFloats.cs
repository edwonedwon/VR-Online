using UnityEngine;
using System.Collections;

using BeardedManStudios.Network;

public class ForgeExample_AuthoritativeControllerFloats : NetworkedMonoBehavior
{
	[NetSync]
	public float horizontal = 0.0f;

	[NetSync]
	public float vertical = 0.0f;

	protected override void Update()
	{
		base.Update();

		if (!IsOwner)
			return;

		horizontal = Input.GetAxis("Horizontal");
		vertical = Input.GetAxis("Vertical");
	}
}