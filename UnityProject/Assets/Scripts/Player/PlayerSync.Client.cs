using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public partial class PlayerSync
{
	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (ReadyToMove == false)
		{
			return;
		}

		//client validation so they don't think they can walk into walls

		CmdProcessAction(moveActions);
	}

	public void CheckPossibleMovementClient()
	{
		//Animate?
		//CmdProcessAction(new PlayerAction());
	}
}