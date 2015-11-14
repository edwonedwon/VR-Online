using System.Collections.Generic;
using BeardedManStudios.Network;
using UnityEngine;
using System.Collections;

public class ForgePlayer_Zombie : NetworkedMonoBehavior
{
	public Transform Bullet;

	[NetSync]
	public int Health = 100; //Player health

	public int Damage = 30;

	[NetSync]
	public bool Shooting = false; //If the player is attacking

	public bool RapidFire = false;
	public float RapidFireTimespan = 3;
	private float _firingRate = 0.5f; //Local variable only the server controls
	private float _timespan = 0;

	public static List<ForgePlayer_Zombie> ZombiePlayers = new List<ForgePlayer_Zombie>(); 

	//Debug font
	GUIStyle blackFont = new GUIStyle();

	protected override void Awake()
	{
		base.Awake();
		blackFont.normal.textColor = Color.black;
		ZombiePlayers.Add(this);
	}

	protected override void Update()
	{
		base.Update();

		if (Shooting) //Local bullet shooting!
		{
			_timespan -= Time.deltaTime;
			if (_timespan <= 0)
			{
				_timespan = RapidFire ? _firingRate * 0.5f : _firingRate;

				float lifespan = 5;
				Ray gunRay = new Ray(transform.position, transform.forward);
				ForgeZombie hitZombie = null;
				RaycastHit[] gunHit;
#if UNITY_EDITOR
				Debug.DrawRay(transform.position, transform.forward * 100, Color.green);
#endif

				gunHit = Physics.RaycastAll(gunRay, 100);
				if (gunHit != null && gunHit.Length > 0)
				{
					foreach (RaycastHit hit in gunHit)
					{
						if (hit.collider != null)
						{
							if (hit.collider.name.Contains("Zombie"))
							{
								hitZombie = hit.collider.GetComponent<ForgeZombie>();
								//We hit a zombie! woot!
								float distance = Vector3.Distance(transform.position, hit.collider.transform.position);
								lifespan = distance * 0.02f;
								break;
							}
						}
					}
				}

				//Shoot the bullet!
				Transform bullet = Instantiate(Bullet, transform.position + (transform.forward * 1.2f), transform.rotation) as Transform;

				if (hitZombie != null)
					bullet.GetComponent<ForgeZombieBullet>().Setup(hitZombie, lifespan);
			}
		}

		if (RapidFire)
		{
			RapidFireTimespan -= Time.deltaTime;
			if (RapidFireTimespan <= 0)
			{
				RapidFireTimespan = 3;
				RapidFire = false;
			}
		}
		
		//Only the owner of the object controls the below section
		if (!IsOwner)
			return;

		//Follow controls
		//if (Input.GetKey(KeyCode.W))
		//	transform.position += transform.forward * 5.0f * Time.deltaTime;

		//if (Input.GetKey(KeyCode.S))
		//	transform.position += -transform.forward * 5.0f * Time.deltaTime;

		//if (Input.GetKey(KeyCode.A))
		//	transform.position += -transform.right * 5.0f * Time.deltaTime;

		//if (Input.GetKey(KeyCode.D))
		//	transform.position += transform.right * 5.0f * Time.deltaTime;

		//Non-Forward Controls
		if (Input.GetKey(KeyCode.W))
			transform.position += Vector3.forward * 5.0f * Time.deltaTime;

		if (Input.GetKey(KeyCode.S))
			transform.position += -Vector3.forward * 5.0f * Time.deltaTime;

		if (Input.GetKey(KeyCode.A))
			transform.position += -Vector3.right * 5.0f * Time.deltaTime;

		if (Input.GetKey(KeyCode.D))
			transform.position += Vector3.right * 5.0f * Time.deltaTime;

		Vector3 mousePos = Input.mousePosition;
		Vector3 objectPos = Camera.main.WorldToScreenPoint(transform.position);
		mousePos.x = mousePos.x - objectPos.x;
		mousePos.y = mousePos.y - objectPos.y;
		float playerRotationAngle = Mathf.Atan2(mousePos.x, mousePos.y) * Mathf.Rad2Deg;

		transform.rotation = Quaternion.Euler (new Vector3(0,playerRotationAngle, 0));

		if (Input.GetMouseButton(0))
			Shooting = true;
		else
			Shooting = false;

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Networking.Disconnect();
		}
	}

	private void OnDestroy()
	{
		ZombiePlayers.Remove(this);
	}

	[BRPC]
	public void EnableRapidFire()
	{
		RapidFireTimespan = 3;
		RapidFire = true;
	}

	public override void Disconnect()
	{
		base.Disconnect();
		ZombiePlayers.Clear();
	}

	private void OnGUI()
	{
		if (!IsOwner)
			return;

		if (!Networking.PrimarySocket.TrackBandwidth)
			return;

		// The server NetworkingManager object controls how fast the client's times are updated
		GUILayout.BeginArea(new Rect(Screen.width * 0.35f, Screen.height * 0.8f, Screen.width * 35f, Screen.height * 0.2f));
		GUILayout.Label("The current server time is: " + NetworkingManager.Instance.ServerTime, blackFont);
		GUILayout.Label("Bytes In: " + OwningNetWorker.BandwidthIn, blackFont);
		GUILayout.Label("Bytes Out: " + OwningNetWorker.BandwidthOut, blackFont);
		GUILayout.EndArea();
	}
}
