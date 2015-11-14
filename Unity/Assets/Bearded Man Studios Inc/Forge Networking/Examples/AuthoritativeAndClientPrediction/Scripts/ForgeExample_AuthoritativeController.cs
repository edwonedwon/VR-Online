using UnityEngine;
using System.Collections;

using BeardedManStudios.Network;

public class ForgeExample_AuthoritativeController : NetworkedMonoBehavior
{
	/// <summary>
	/// The speed of our character
	/// </summary>
	public float speed = 5.0f;

	protected override void Start()
	{
		base.Start();

		// An event that fires whenever a key down request has happened
		inputDownRequest += Input;

		// An event that fires whenever a key has been pressed down and has not been released yet
		inputRequest += InputDown;

		// An event that fires whenever a key releasedrequest has happened
		inputUpRequest += InputUp;

		// An event that fires whenever a mouse button down request has happened
		mouseDownRequest += MouseDown;

		// An event that fires whenever a mouse button has been pressed down and has not been released yet
		mouseRequest += Mouse;

		// An event that fires whenever a mouse button up request has happened
		mouseUpRequest += MouseUp;
	}

	/// <summary>
	/// Called when there was an mouse button release request
	/// </summary>
	/// <param name="keyCode">The index of the button that was released</param>
	private void MouseUp(int buttonIndex, int frame)
	{
		Debug.Log("The mouse button with index " + buttonIndex + " was released!");
	}

	/// <summary>
	/// Called while mouse button was pressed down and has not been released yet
	/// </summary>
	/// <param name="keyCode">The index of the button that was pressed down</param>
	private void Mouse(int buttonIndex)
	{
		Debug.Log("The mouse button with index " + buttonIndex + " is currently being pressed down!");
	}

	/// <summary>
	/// Called when there was an mouse button down request
	/// </summary>
	/// <param name="buttonIndex">The index of the button that was pressed down</param>
	private void MouseDown(int buttonIndex, int frame)
	{
		Debug.Log("The mouse button with index " + buttonIndex + " was pressed down!");

		if (buttonIndex == 0)
			Debug.Log("Requested frame was on " + frame + " current frame is " + NetworkingManager.Instance.CurrentFrame);
	}

	/// <summary>
	/// Called when there was an input key release request
	/// </summary>
	/// <param name="keyCode">The key that was released</param>
	private void InputUp(KeyCode keyCode, int frame)
	{
		Debug.Log(keyCode.ToString() + " was released and this is " + (!OwningNetWorker.IsServer ? "not" : "") + " the server");
	}

	/// <summary>
	/// Called while an input key was pressed down and has not been released yet
	/// </summary>
	/// <param name="keyCode">The key that is currenlty pressed down</param>
	private void Input(KeyCode keyCode, int frame)
	{
		Debug.Log(keyCode.ToString() + " was pressed and this is " + (!OwningNetWorker.IsServer ? "not" : "") + " the server");
	}

	/// <summary>
	/// Called when there was an input key down request
	/// </summary>
	/// <param name="keyCode">The key that was pressed down</param>
	private void InputDown(KeyCode keyCode)
	{
		switch (keyCode)
		{
			case KeyCode.LeftArrow:
				transform.position += Vector3.left * speed * Time.deltaTime;
				break;
			case KeyCode.RightArrow:
				transform.position += Vector3.right * speed * Time.deltaTime;
				break;
		}
	}

	protected override void Update()
	{
		base.Update();

		// Check for a right arrow down or up and request on server if changed
		InputCheck(KeyCode.RightArrow);

		// Check for a left arrow down or up and request on server if changed
		InputCheck(KeyCode.LeftArrow);

		// Check for a left mouse button down or up and request on server if changed
		MouseCheck(0);
	}
}