using UnityEngine;
using System.Collections;

/// <summary>
/// This is not optimized to be pooling, 
/// I recommend pooling your bullets rather than destroying it like so
/// </summary>
public class ForgeZombieBullet : MonoBehaviour
{
	public int BulletDamage = 35;
	public float Lifespan = 5;

	private float _bulletSpeed = 30;
	private ForgeZombie _hitTarget;

	// Update is called once per frame
	void Update()
	{
		transform.position += (transform.forward * Time.deltaTime) * _bulletSpeed;

		Lifespan -= Time.deltaTime;
		if (Lifespan <= 0) //Make these bullets pooled so it just sets it inactive/active (re-using it)
		{
			if (_hitTarget != null)
				_hitTarget.Damage(this);

			Destroy(gameObject);
		}
	}

	public void Setup(ForgeZombie hitTarget, float lifespan)
	{
		_hitTarget = hitTarget;
		Lifespan = lifespan;
	}
}
