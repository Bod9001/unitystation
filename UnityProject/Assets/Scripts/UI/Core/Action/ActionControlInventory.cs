using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Action
{
	public class ActionControlInventory : MonoBehaviour, IClientInventoryMove
	{
		public ActionController ActionControllerType = ActionController.Inventory;

		public List<IActionGUI> ControllingActions = new List<IActionGUI>();

		public void OnInventoryMoveClient(ClientInventoryMove info)
		{
			if (LocalPlayerManager.CurrentMind == null) return;
			var pna = LocalPlayerManager.CurrentMind.playerNetworkActions;
			bool showAlert = false;
			foreach (var itemSlot in pna.itemStorage.GetHandSlots())
			{
				if (itemSlot.ItemObject == gameObject)
				{
					showAlert = true;
				}
			}

			foreach (var _IActionGUI in ControllingActions)
			{
				UIActionManager.ToggleLocal(_IActionGUI, showAlert);
			}
		}

		void Start()
		{
			var ActionGUIs = this.GetComponents<IActionGUI>();
			foreach (var ActionGUI in ActionGUIs)
			{
				if (ActionGUI.ActionData.PreventBeingControlledBy.Contains(ActionControllerType) == false)
				{
					ControllingActions.Add(ActionGUI);
				}
			}
		}
	}
}
