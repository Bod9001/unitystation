using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatInputContext : IChatInputContext
{
	/// <summary>
	/// Return default channel for a player. Depends on a current headset, antags, etc
	/// Can return ChatChannel.None if default channel is unknown
	/// Note: works only on a local client!
	/// </summary>
	public ChatChannel DefaultChannel
	{
		get
		{
			// Player doesn't even connected to the game?
			if (LocalPlayerManager.CurrentMind.OrNull()?.AssignedPlayer.OrNull()?.Connection == null)
			{
				return ChatChannel.None;
			}

			return LocalPlayerManager.CurrentMind.DefaultChatChannel;
		}
	}
}
