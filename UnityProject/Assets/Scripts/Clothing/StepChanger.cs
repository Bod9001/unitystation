using NaughtyAttributes;
using ScriptableObjects.Audio;
using UnityEngine;

namespace Clothing
{
	/// <summary>
	/// Handles the change of StepType when players equip or unequip this item
	/// </summary>
	public class StepChanger : MonoBehaviour, IServerInventoryMove
	{
		[Expandable]
		[SerializeField]
		[Tooltip("Pack of sounds that this StepChanger will replace")]
		private FloorSounds soundChange;

		[SerializeField][Tooltip("Slot where this StepChanger should take effect.")]
		private NamedSlot slot = NamedSlot.feet;

		[SerializeField]
		[Tooltip("If true, this step changer " +
		         "will have priority over other step changers when putting on (Hardsuits for example)")]
		private bool hasPriority;

		private Equipment Equipment;

		private bool IsPuttingOn(InventoryMove info)
		{
			return info.ToSlot != null &&
			       info.ToSlot.NamedSlot == slot &&
			       info.ToRootPlayer;
		}

		private bool IsTakingOff(InventoryMove info)
		{
			return info.FromSlot != null &&
			       info.FromSlot.NamedSlot == slot &&
			       info.FromPlayer;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (soundChange == null) return;

			if (IsPuttingOn(info))
			{
				Equipment = info.ToSlot.GetRootStorage().GetComponent<Equipment>();
				if (Equipment is null) return;

				if (hasPriority == false)
				{
					Equipment.SecondaryStepSound = soundChange;
					if (Equipment.StepSound) return;
				}

				Equipment.StepSound = soundChange;
			}

			if (IsTakingOff(info))
			{
				Equipment = info.FromSlot.GetRootStorage().GetComponent<Equipment>();
				if (Equipment is null) return;

				HandleTakingOff();
			}
		}
		/// <summary>
		/// Stupid logic to handle all possible interaction combinations with clownshoes and hardsuits
		/// </summary>
		private void HandleTakingOff()
		{
			switch (hasPriority)
			{
				case true when Equipment.SecondaryStepSound:
					Equipment.StepSound = Equipment.SecondaryStepSound;
					return;
				case true:
					Equipment.StepSound = null;
					return;
				case false when Equipment.StepSound == soundChange:
					Equipment.StepSound = null;
					return;
				case false when Equipment.StepSound != soundChange:
					if (Equipment.SecondaryStepSound == soundChange)
					{
						Equipment.SecondaryStepSound = null;
					}

					return;
			}
		}
	}
}
