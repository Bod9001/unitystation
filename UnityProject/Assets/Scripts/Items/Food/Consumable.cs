using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

/// <summary>
/// Item that can be drinked or eaten by player
/// Also supports force feeding other player
/// </summary>
public abstract class Consumable : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public void ServerPerformInteraction(HandApply interaction)
	{
		var targetPlayer = MindManager.Instance.Get(interaction.TargetObject, true);
		if (targetPlayer == null)
		{
			return;
		}

		var feederSlot = interaction.Performer.GetActiveHandSlot();
		if (feederSlot.Item == null)
		{   //Already been eaten or the food is no longer in hand
			return;
		}


		TryConsume(interaction.Performer, targetPlayer);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//this item shouldn't be a target
		if (Validations.IsTarget(gameObject, interaction)) return false;
		var Dissectible = interaction?.TargetObject.OrNull()?.GetComponent<Dissectible>();
		if (Dissectible != null)
		{
			if (Dissectible.GetBodyPartIsopen)
			{
				return false;
			}
		}

		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return CanBeConsumedBy(MindManager.Instance.Get(interaction.TargetObject, true));
	}

	/// <summary>
	/// Check thats eater can consume this item
	/// </summary>
	/// <param name="eater">Player that want to eat item</param>
	/// <returns></returns>
	public virtual bool CanBeConsumedBy(Mind eater)
	{
		if (eater == null || eater.IsGhosting)
		{
			return false;
		}

		return true;
	}


	/// <summary>
	/// Try to consume this item by eater. Server side only.
	/// </summary>
	/// <param name="eater">Player that want to eat item</param>
	public void TryConsume(Mind eater)
	{
		TryConsume(eater, eater);
	}

	/// <summary>
	/// Try to consume this item by eater. Server side only.
	/// </summary>
	/// <param name="feeder">Player that feed eater. Can be same as eater.</param>
	/// <param name="eater">Player that is going to eat item</param>
	public abstract void TryConsume(Mind feeder, Mind eater);
}
