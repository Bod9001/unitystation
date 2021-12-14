using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Doors;
using Items;
using Messages.Server;
using UnityEngine;
using UnityEngine.Events;
using Objects;
using ScriptableObjects.Audio;

public partial class PlayerSync
{
	private Queue<PlayerAction> serverPendingActions = new Queue<PlayerAction>();
	private readonly int maxServerQueue = 1;

	[Command]
	private void CmdProcessAction(PlayerAction action)
	{
		if (action.moveAction == MoveAction.None) return;

		if (serverPendingActions.Count > maxServerQueue)
		{
			return;
		}

		if (action.moveAction != MoveAction.None)
		{
			Logger.Log("o3o");
		}

		//add action to server simulation queue
		serverPendingActions.Enqueue(action);
	}


	private void ServerProcessMove()
	{
		if (ReadyToMove && serverPendingActions.Count > 0)
		{
			if (true) //Gravity
			{
				var Action = serverPendingActions.Dequeue();

				TargetLocalPosition = Action.Direction() + transform.localPosition.To2();
				ReadyToMove = false;
			}
			else
			{
				Logger.LogError("Implement");
			}

		}
	}


}
