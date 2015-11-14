using UnityEngine;
using BeardedManStudios.Network;

public class NetworkPlayerInstance
	: NetworkedMonoBehavior
{
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

    public void Update()
    {
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


    public void SyncVR()
    {
        if (!SteamVR.active)
            return;

        var controller0 = transform.FindChild("Controller0");
        var controller1 = transform.FindChild("Controller1");
        var head = transform.FindChild("Head");

        var steam_controller_manager = FindObjectOfType<SteamVR_ControllerManager>();
        var steam_controller0 = steam_controller_manager.left;
        var steam_controller1 = steam_controller_manager.right;
        var steam_cam = FindObjectOfType<SteamVR_Camera>();

        if (steam_controller0)
        {
            controller0.transform.position = steam_controller0.transform.position;
            controller0.transform.rotation = steam_controller0.transform.rotation;
        }

        if (steam_controller1)
        {
            controller1.transform.position = steam_controller1.transform.position;
            controller1.transform.rotation = steam_controller1.transform.rotation;
        }

        if (steam_cam)
        {
            head.transform.position = steam_cam.transform.position;
            head.transform.rotation = steam_cam.transform.rotation;
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
