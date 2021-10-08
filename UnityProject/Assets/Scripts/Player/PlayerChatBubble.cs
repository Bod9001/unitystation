using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Messages.Server;
using Mirror;
using UnityEngine;

public class PlayerChatBubble : NetworkBehaviour
{
	public Transform chatBubbleTarget;

	public PushPull PushPull;
	public LivingHealthMasterBase LivingHealthMasterBase;
	public void Awake()
	{
		PushPull = this.GetComponent<PushPull>();
		LivingHealthMasterBase = this.GetComponent<LivingHealthMasterBase>();
	}

	[Server]
	public void ServerToggleChatIcon(bool turnOn, string message, ChatChannel chatChannel, ChatModifier chatModifier)
	{
		if (!PushPull.VisibleState || ( LivingHealthMasterBase.IsDead || LivingHealthMasterBase.IsCrit))
		{
			//Don't do anything with chat icon if player is invisible or not spawned in
			//This will also prevent clients from snooping other players local chat messages that aren't visible to them
			return;
		}

		// Cancel right away if the player cannot speak.
		if ((chatModifier & ChatModifier.Mute) == ChatModifier.Mute)
		{
			return;
		}

		ShowChatBubbleMessage.SendToNearby(gameObject, message, true, chatModifier);
	}
}
