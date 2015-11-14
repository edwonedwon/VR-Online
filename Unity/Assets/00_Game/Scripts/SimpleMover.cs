using UnityEngine;
using BeardedManStudios.Network;

public class SimpleMover
    : MonoBehaviour
{
    private Rigidbody rigidbody;
    private Vector3 velocity;

    private void Start()
    {
    }

	private void Update()
	{
        if (rigidbody == null)
        {
            rigidbody = GetComponent<Rigidbody>();
        }

        UpdateRB();
        UpdateTransform();

    }

    private void UpdateRB()
    {
        if (!rigidbody)
            return;

        //rigidbody.velocity = velocity;
        //rigidbody.AddForce(Vector3.up, ForceMode.VelocityChange);

        var force = 250.0f;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            rigidbody.AddForce(Vector3.up * force, ForceMode.Acceleration);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            rigidbody.AddForce(Vector3.down * force, ForceMode.Acceleration);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            rigidbody.AddTorque(Vector3.right * force, ForceMode.Acceleration);

		if (Input.GetKeyDown(KeyCode.LeftArrow))
            rigidbody.AddTorque(Vector3.left * force, ForceMode.Acceleration);
	}

    private void UpdateTransform()
    {
        if (rigidbody)
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