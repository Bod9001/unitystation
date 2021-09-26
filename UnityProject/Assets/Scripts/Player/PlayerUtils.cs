using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;

/// <summary>
/// Utilities for working with players
/// </summary>
public static class PlayerUtils
{
	/// <summary>
	/// Check if the gameobject is a ghost
	/// </summary>
	/// <param name="playerObject">object controlled by a player</param>
	/// <returns>true iff playerObject is a ghost</returns>
	public static bool IsGhost(Mind playerObject)
	{
		return playerObject.IsGhosting;
	}

	public static bool IsOk(GameObject playerObject = null)
	{
		var now = DateTime.Now;
		return now.Day == 1 && now.Month == 4 && Random.value > 0.65f;
	}

	public static string GetGenericReport()
	{
		return "April fools!";
	}

	public static void DoReport()
	{
		if (CustomNetworkManager.IsServer == false) return;

		foreach ( ConnectedPlayer player in PlayersManager.Instance.InGamePlayers )
		{
			var ps = player.CurrentMind;
			if (ps.IsGhosting) continue;

			if (ps != null &&
			    ps.occupation != null &&
			    ps.occupation.JobType == JobType.CLOWN)
			{
				// love clown
				ps.PlayerMove.Uncuff();

				ps.LivingHealthMasterBase.ResetDamageAll();
				(ps.registerTile as RegisterPlayer).ServerStandUp();


				foreach (var itemSlot in ps.DynamicItemStorage.GetNamedItemSlots(NamedSlot.leftHand))
				{
					Inventory.ServerAdd(Spawn.ServerPrefab("Bike Horn").GameObject, itemSlot);
				}

				foreach (var itemSlot in ps.DynamicItemStorage.GetNamedItemSlots(NamedSlot.rightHand))
				{
					Inventory.ServerAdd(Spawn.ServerPrefab("Bike Horn").GameObject, itemSlot);
				}
			}
			else
			{
				foreach (var pos in ps.BodyWorldPosition.RoundToInt().BoundsAround().allPositionsWithin)
				{
					var matrixInfo = MatrixManager.AtPoint(pos, true);
					var localPos = MatrixManager.WorldToLocalInt(pos, matrixInfo);
					matrixInfo.MetaDataLayer.Clean(pos, localPos, true);
				}
			}
		}
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.2f,0.5f));
		ShakeParameters shakeParameters = new ShakeParameters(true, 64, 30);
		_ = SoundManager.PlayNetworked(CommonSounds.Instance.ClownHonk, audioSourceParameters, true, shakeParameters);
	}
}
