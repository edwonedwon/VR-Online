using System;
using System.Text;
using UnityEngine;
using System.Collections;
using BeardedManStudios.Network;
using UnityEngine.UI;

/// <summary>
/// This is a simple Forge Chat Utility
/// </summary>
public class ForgeChat : SimpleNetworkedMonoBehavior
{
	//Our chatbox
	public Text Chatbox;

	//Our chat input
	public InputField ChatInput;

	//My name, set by my player socket
	private string _myName;

	/// My message color
	private Color _myMessageColor = Color.white;

	//All the chat messages
	private string _chatMessages;

	/// The maximum we are allowed to store
	private const int MAX_STORED_MESSAGES = 100;

	// To help performance
	private System.Text.StringBuilder _textBuilder = new StringBuilder();

	protected override void NetworkStart()
	{
		base.NetworkStart();

		if (Networking.PrimarySocket.IsServer)
		{
			_myName = "SERVER";
			_myMessageColor = Color.yellow;
		}
		else
			_myName = "Guest" + OwningNetWorker.Uniqueidentifier;
	}

	public void EndChatMessage()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			SendChatMessage();
		}
	}

	//Called when we hit send on the chat message
	public void SendChatMessage()
	{
		_textBuilder.Length = 0;
		_textBuilder.Append("<color=#");
		_textBuilder.Append(ColorToHex(_myMessageColor));
		_textBuilder.Append(">");
		_textBuilder.Append(_myName);
		_textBuilder.Append(": ");
		_textBuilder.Append(ChatInput.text);
		_textBuilder.Append("</color>");
		_textBuilder.Append(System.Environment.NewLine);
		_chatMessages = _textBuilder.ToString() + _chatMessages;
		RPC("ReceiveChatMessage", NetworkReceivers.Others, _textBuilder.ToString());

		ChatInput.text = string.Empty;
		UpdateChatWindow();
	}

	[BRPC]
	private void ReceiveChatMessage(string message)
	{
		_chatMessages = message + _chatMessages;
		UpdateChatWindow();
	}

	//Updates the chat window
	private void UpdateChatWindow()
	{
		string[] messages = _chatMessages.Split(new char[]{'\n'});
		
		if (messages.Length > MAX_STORED_MESSAGES)
		{
			_textBuilder.Length = 0;
			for (int i = 0; i < MAX_STORED_MESSAGES - 50; ++i)
			{
				_textBuilder.Append(messages[i]);
				_textBuilder.Append('\n');
			}
			_chatMessages = _textBuilder.ToString();
		}
		Chatbox.text = _chatMessages;
	}

	//Converts a color to hex
	private string ColorToHex(Color col)
	{
		return FloatToHex(col.r) + FloatToHex(col.g) + FloatToHex(col.b);
	}

	private string FloatToHex(float value)
	{
		return ((int)(value * 255)).ToString("x2");
	}
}
