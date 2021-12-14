using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client.NewPlayer;
using UnityEngine;
using Mirror;
using Objects;
using Player.Movement;

public partial class PlayerSync : NetworkBehaviour, IPlayerControllable
{

	private Vector2 TargetLocalPosition;
	private float Speed = 5;
	private bool ReadyToMove = true;

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}


	private void UpdateMe()
	{
		if (isServer)
		{
			ServerProcessMove();
			//server stuff
		}

		// if (this.gameObject == PlayerManager.LocalPlayer)
		// {
		// 	CheckPossibleMovementClient();
		// }

		Animate();
	}

	private void Animate()
	{
		transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetLocalPosition,
			Speed * Time.deltaTime * transform.localPosition.DistanceSpeedModifier(TargetLocalPosition));

		if (transform.localPosition.To2() == TargetLocalPosition)
		{
			ReadyToMove = true;
		}

	}


}