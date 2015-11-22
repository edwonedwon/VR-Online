using UnityEngine;
using BeardedManStudios.Network;

public class NetworkPlayerInstance
	: NetworkedMonoBehavior
{
    MeshCollider controller0_collider;
    MeshCollider controller1_collider;

    public GameObject SpawnPrefab;

    public void OnConnect()
    {
        var hue = name.GetHashCode() % 255;
        var color = HueToRgb(hue / 255.0f);
        var renderer = GetComponent<Renderer>();

        if (renderer)
        {
            renderer.material.color = color;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!IsOwner)
            return;

        SyncVR();

        // sync position from controllers
        var rigidbody = GetComponent<Rigidbody>();

        // test
        if (Input.GetKeyDown(KeyCode.UpArrow)) rigidbody.AddForce(Vector3.up * 250.0f, ForceMode.Acceleration);
        if (Input.GetKeyDown(KeyCode.DownArrow)) rigidbody.AddForce(Vector3.down * 250.0f, ForceMode.Acceleration);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) rigidbody.AddTorque(Vector3.left * 250.0f, ForceMode.Acceleration);
        if (Input.GetKeyDown(KeyCode.RightArrow)) rigidbody.AddTorque(Vector3.right * 250.0f, ForceMode.Acceleration);
    }

    protected override void Awake()
    {
        base.Awake();

        var controller0 = transform.FindChild("Controller0");
        var controller1 = transform.FindChild("Controller1");

        controller0_collider = controller0.gameObject.GetComponent<MeshCollider>();
        controller1_collider = controller1.gameObject.GetComponent<MeshCollider>();

        AddNetworkVariable(() => controller0_collider.enabled, x => controller0_collider.enabled = (bool)x);
        AddNetworkVariable(() => controller1_collider.enabled, x => controller1_collider.enabled = (bool)x);
    }

    public void SyncVR()
    {
        if (!SteamVR.active)
            return;

        var controller0 = transform.FindChild("Controller0");
        var controller1 = transform.FindChild("Controller1");
        var head = transform.FindChild("Head");

        if (!head)
            return;

        var steam_controller_manager = FindObjectOfType<SteamVR_ControllerManager>();
        var steam_controller0 = steam_controller_manager.left;
        var steam_controller1 = steam_controller_manager.right;
        var steam_cam = FindObjectOfType<SteamVR_Camera>();

        if (steam_controller0)
        {
            controller0.transform.position = steam_controller0.transform.position;
            controller0.transform.rotation = steam_controller0.transform.rotation;

            UpdateControllerInput(steam_controller0, controller0.gameObject);
        }

        if (steam_controller1)
        {
            controller1.transform.position = steam_controller1.transform.position;
            controller1.transform.rotation = steam_controller1.transform.rotation;

            UpdateControllerInput(steam_controller1, controller1.gameObject);
        }

        if (steam_cam)
        {
            head.transform.position = steam_cam.transform.position;
            head.transform.rotation = steam_cam.transform.rotation;
        }
    }

    public void UpdateControllerInput(GameObject controller, GameObject obj)
    {
        var tracked_object = GetComponent<SteamVR_TrackedObject>();
        int index = (int)tracked_object.index;
        var device = SteamVR_Controller.Input(index);

        var trigger_down = device.GetHairTrigger();
        var trigger_press = device.GetHairTriggerDown();
        var trigger_release = device.GetHairTriggerUp();

        var collider = obj.GetComponent<BoxCollider>();
        
        collider.enabled = trigger_down;

        var touchpad_down = device.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);

        if (touchpad_down && !trigger_down)
        {
            var prefab = Resources.Load("NetworkSpawnCube");

            Network.Instantiate(prefab, obj.transform.position, Quaternion.identity, 1);
        }
    }

    private Color HueToRgb(float hue)
    {
        float r = Mathf.Abs(hue * 6.0f - 3.0f) - 1.0f;
        float g = 2.0f - Mathf.Abs(hue * 6.0f - 2.0f);
        float b = 2.0f - Mathf.Abs(hue * 6.0f - 4.0f);

        return new Color(
            Mathf.Clamp01(r),
            Mathf.Clamp01(g),
            Mathf.Clamp01(b)
        );
    }
}
