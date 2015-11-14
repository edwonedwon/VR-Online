using UnityEngine;

public class SimpleMover
    : MonoBehaviour
{
	private void Update()
	{
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