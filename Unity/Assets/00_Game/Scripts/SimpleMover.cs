using UnityEngine;
using BeardedManStudios.Network;

public class SimpleMover
    : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
    }

	private void Update()
	{
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        UpdateRB();
        UpdateTransform();

    }

    private void UpdateRB()
    {
        if (!rb)
            return;

        //rb.velocity = velocity;
        //rb.AddForce(Vector3.up, ForceMode.VelocityChange);

        var force = 250.0f;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            rb.AddForce(Vector3.up * force, ForceMode.Acceleration);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            rb.AddForce(Vector3.down * force, ForceMode.Acceleration);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            rb.AddTorque(Vector3.right * force, ForceMode.Acceleration);

		if (Input.GetKeyDown(KeyCode.LeftArrow))
            rb.AddTorque(Vector3.left * force, ForceMode.Acceleration);
	}

    private void UpdateTransform()
    {
        if (rb)
            return;

		if (Input.GetKeyDown(KeyCode.UpArrow))
			transform.position += Vector3.up;

		if (Input.GetKeyDown(KeyCode.DownArrow))
			transform.position += Vector3.down;

		if (Input.GetKeyDown(KeyCode.RightArrow))
			transform.Rotate(Vector3.up, 45.0f);

		if (Input.GetKeyDown(KeyCode.LeftArrow))
			transform.Rotate(Vector3.right, 45.0f);
	}
}