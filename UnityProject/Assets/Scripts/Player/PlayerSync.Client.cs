using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public partial class PlayerSync
{

	public bool ActionQueued = false;



	public void ReceivePlayerMoveAction(PlayerAction moveActions)
	{
		if (moveActions.moveAction == MoveAction.None) return;
		if (ReadyToMove == false) return;
		CmdProcessAction(moveActions);
	}

	public void CheckPossibleMovementClient()
	{
		//Animate?
		//CmdProcessAction(new PlayerAction());
	}

}
