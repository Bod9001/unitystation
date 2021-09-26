using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Mirror;
using Systems.Electricity;
using HealthV2;

namespace Objects.Medical
{
	public class DNAScanner : NetworkBehaviour, ICheckedInteractable<MouseDrop>, IAPCPowerable
	{
		public ClosetControl ClosetControl;

		public LivingHealthMasterBase occupant;
		public string statusString;

		public bool Powered => powered;
		[SyncVar(hook = nameof(SyncPowered))] private bool powered;
		// tracks whether we've recieved our first power update from electricity.
		// allows us to avoid syncing power when it is unchanged
		private bool powerInit;

		private enum ScannerState
		{
			OpenUnpowered,
			OpenPowered,
			ClosedUnpowered,
			ClosedPowered,
			ClosedPoweredWithOccupant
		}

		public Engineering.APC RelatedAPC;

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncPowered(powered, powered);
			RelatedAPC = GetComponent<APCPoweredDevice>().RelatedAPC;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			statusString = "Ready to scan.";
			SyncPowered(powered, powered);
			RelatedAPC = GetComponent<APCPoweredDevice>().RelatedAPC;
		}

		//TODO 	occupant ;

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (side == NetworkSide.Server && ClosetControl.IsClosed)
				return false;
			if (!Validations.CanInteract(interaction.Performer, side))
				return false;
			if (!Validations.IsAdjacent(interaction.Performer.GameObjectBody, interaction.DroppedObject))
				return false;
			if (!Validations.IsAdjacent(interaction.Performer.GameObjectBody, gameObject))
				return false;
			if (interaction.Performer == interaction.DroppedObject)
				return false;
			return true;
		}

		public void ServerPerformInteraction(MouseDrop drop)
		{
			var objectBehaviour = drop.DroppedObject.GetComponent<ObjectBehaviour>();
			if (objectBehaviour)
			{
				ClosetControl.ServerStorePlayer(objectBehaviour);
				ClosetControl.ServerToggleClosed(true);
			}
		}

		protected void UpdateSpritesOnStatusChange()
		{
			if (ClosetControl.ClosetStatus == ClosetStatus.Open)
			{

				if (!powered)
				{
					ClosetControl.doorSpriteHandler.ChangeSprite((int) ScannerState.OpenUnpowered);
				}
				else
				{
					ClosetControl.doorSpriteHandler.ChangeSprite((int) ScannerState.OpenPowered);
				}
			}
			else if (!powered)
			{
				ClosetControl.doorSpriteHandler.ChangeSprite((int) ScannerState.ClosedUnpowered);
			}
			else if (ClosetControl.ClosetStatus == ClosetStatus.Closed)
			{
				ClosetControl.doorSpriteHandler.ChangeSprite((int) ScannerState.ClosedPowered);
			}
			else if (ClosetControl.ClosetStatus == ClosetStatus.ClosedWithOccupant)
			{
				if (gameObject != null && gameObject.activeInHierarchy)
				{
					ClosetControl.doorSpriteHandler.ChangeSprite((int) ScannerState.ClosedPoweredWithOccupant);
				}
			}
		}

		private void SyncPowered(bool oldValue, bool value)
		{
			// does nothing if power is unchanged and
			// we've already init'd
			if (powered == value && powerInit) return;

			powered = value;
			if (powered == false)
			{
				if (ClosetControl.IsLocked)
				{
					ClosetControl.ServerToggleLocked(false);
				}
			}
			UpdateSpritesOnStatusChange();
		}

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			RelatedAPC = GetComponent<APCPoweredDevice>().RelatedAPC;
			if (state == PowerState.Off || state == PowerState.LowVoltage)
			{
				SyncPowered(powered, false);
			}
			else
			{
				SyncPowered(powered, true);
			}

			if (powerInit == false)
			{
				powerInit = true;
			}
		}

		#endregion
	}
}
