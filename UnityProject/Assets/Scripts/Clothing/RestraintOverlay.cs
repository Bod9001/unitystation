using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using UI.Action;

namespace UI.Items
{
	/// <summary>
	/// Handles the overlays for the handcuff sprites
	/// </summary>
	public class RestraintOverlay : ClothingItem, IActionGUI
	{
		// TODO Different colored overlays for different restraints
		[SerializeField]
		private List<Sprite> handCuffOverlays = new List<Sprite>();

		[SerializeField] private SpriteRenderer spriteRend = null;
		private IEnumerator uncuffCoroutine;
		private float healthCache;
		private Vector3Int positionCache;

		public Mind LocalMind;

		[SerializeField]
		private ActionData actionData = null;

		public ActionData ActionData => actionData;

		public override void SetReference(GameObject Item)
		{
			GameObjectReference = Item;
			if (Item == null)
			{
				spriteRend.sprite = null;
			}
			else
			{
				spriteRend.sprite = handCuffOverlays[referenceOffset];
			}
			DetermineAlertUI();
		}

		public override void UpdateSprite()
		{
			if (GameObjectReference != null)
			{
				spriteRend.sprite = handCuffOverlays[referenceOffset];
			}
		}

		private void DetermineAlertUI()
		{
			if (MindManager.Instance.Get(gameObject) != LocalPlayerManager.CurrentMind) return;

			UIActionManager.ToggleLocal(this, GameObjectReference != null);
		}

		public void ServerBeginUnCuffAttempt()
		{
			if (uncuffCoroutine != null)
				StopCoroutine(uncuffCoroutine);

			float resistTime = GameObjectReference.GetComponent<Restraint>().ResistTime;
			healthCache = LocalMind.LivingHealthMasterBase.OverallHealth;
			positionCache = LocalMind.registerTile.LocalPositionServer;
			if (!CanUncuff()) return;

			var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Unbuckle, false, false, true), TryUncuff);
			bar.ServerStartProgress(LocalMind.registerTile, resistTime, LocalMind);
			Chat.AddActionMsgToChat(
				LocalMind,
				$"You are attempting to remove the cuffs. This takes up to {resistTime:0} seconds",
				LocalMind.ExpensiveName() + " is attempting to remove their cuffs");
		}

		private void TryUncuff()
		{
			if (CanUncuff())
			{
				LocalMind.PlayerMove.Uncuff();
				Chat.AddActionMsgToChat(LocalMind, "You have successfully removed the cuffs",
					LocalMind.ExpensiveName() + " has removed their cuffs");
			}
		}

		private bool CanUncuff()
		{
			var playerHealth = LocalMind.LivingHealthMasterBase;

			if (playerHealth == null ||
				playerHealth.ConsciousState == ConsciousState.DEAD ||
				playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS ||
				playerHealth.OverallHealth != healthCache ||
				(LocalMind.registerTile as RegisterPlayer).IsSlippingServer ||
				positionCache != LocalMind.registerTile.LocalPositionServer)
			{
				return false;
			}

			return true;
		}

		public void CallActionClient()
		{
			LocalPlayerManager.CurrentMind.playerNetworkActions.CmdTryUncuff();
		}
	}
}
