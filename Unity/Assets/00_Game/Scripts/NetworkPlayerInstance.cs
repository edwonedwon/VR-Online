using UnityEngine;
using BeardedManStudios.Network;

public class NetworkPlayerInstance
	: SimpleNetworkedMonoBehavior
{
    public void OnConnect()
    {
        var hue = name.GetHashCode() % 255;
        var color = HueToRgb(hue / 255.0f);
        var renderer = GetComponent<Renderer>();
        var material = renderer.material;

        material.color = color;
    }

    public void Update()
    {
        if (!IsOwner)
            return;

        // sync position from controllers
        var rigidbody = GetComponent<Rigidbody>();

        // test
        if (Input.GetKeyDown(KeyCode.UpArrow)) rigidbody.AddForce(Vector3.up * 250.0f, ForceMode.Acceleration);
        if (Input.GetKeyDown(KeyCode.DownArrow)) rigidbody.AddForce(Vector3.down * 250.0f, ForceMode.Acceleration);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) rigidbody.AddTorque(Vector3.left * 250.0f, ForceMode.Acceleration);
        if (Input.GetKeyDown(KeyCode.RightArrow)) rigidbody.AddTorque(Vector3.right * 250.0f, ForceMode.Acceleration);
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
