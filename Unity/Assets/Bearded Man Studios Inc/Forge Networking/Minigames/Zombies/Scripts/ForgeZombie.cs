using BeardedManStudios.Network;
using UnityEngine;
using System.Collections;

/// <summary>
/// This zombie is created by the server and is only controlled by the server
/// </summary>
public class ForgeZombie : NetworkedMonoBehavior
{
	[NetSync]
	public int Health = 100; // Zombie health

	private bool _dead = false;
	private Renderer _thisRenderer;
	public bool destroy;

	protected override void Awake()
	{
		base.Awake();
		_thisRenderer = GetComponent<Renderer>();
	}

	protected override void Update()
	{
		base.Update();

		if (destroy)
		{
			Networking.Destroy(this);
			return;
		}

		if (!OwningNetWorker.IsServer)
			return;

		// To check if we are dead
		if (_dead)
			return;

		if (Health <= 0 && OwningNetWorker.IsServer) // Zombie died!
		{
			_dead = true;
			Networking.Destroy(this); // Destroy it! :)
		}
		else
		{
			ForgePlayer_Zombie closestPlayer = null;
			float dist = 100; // Min distance

			// Check for the closest player
			foreach (ForgePlayer_Zombie player in ForgePlayer_Zombie.ZombiePlayers)
			{
				float distance = Vector3.Distance(player.transform.position, transform.position);
				if (distance < dist)
				{
					closestPlayer = player;
					dist = distance;
				}
			}

			if (closestPlayer != null && dist > 2) // Move towards the closest player
				transform.position -= (transform.position - closestPlayer.transform.position) * Time.deltaTime;
		}
	}

	public void Damage(ForgeZombieBullet bullet)
	{
		if (bullet != null)
		{
			if (OwningNetWorker.IsServer) // Only the server cares about the damage
				Health -= bullet.BulletDamage;

			if (Health < 35)
			{
				_thisRenderer.material.color = Color.red;
			}
		}
	}
}
