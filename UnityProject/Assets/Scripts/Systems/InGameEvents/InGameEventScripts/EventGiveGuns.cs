using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using ScriptableObjects;
using Strings;

namespace InGameEvents
{
	public class EventGiveGuns : EventScriptBase
	{
		[SerializeField]
		private GameObjectList gunList = default;

		public override void OnEventStart()
		{
			if (!FakeEvent)
			{
				SpawnGuns();
			}

			base.OnEventStart();
		}

		protected void SpawnGuns()
		{
			if (gunList == null || gunList.GameObjectPrefabs.Length == 0)
			{
				Logger.LogError($"No guns in gun list! Cannot spawn guns for {nameof(EventGiveGuns)}.", Category.Event);
				return;
			}

			foreach (ConnectedPlayer player in PlayersManager.Instance.InGamePlayers)
			{
				if (player.CurrentMind.IsGhosting) continue;

				HandlePlayer(player.CurrentMind);
			}
		}

		protected virtual void HandlePlayer(Mind player)
		{
			GiveGunToPlayer(player);
		}

		protected void GiveGunToPlayer(Mind player)
		{
			GameObject gun = Spawn.ServerPrefab(gunList.GetRandom(),
						player.BodyWorldPosition, player.GameObjectBody.transform.parent, player.GameObjectBody.transform.rotation).GameObject;

			ItemSlot slot = player.DynamicItemStorage.GetBestHandOrSlotFor(gun);
			if (slot != null && slot.IsEmpty)
			{
				Inventory.ServerAdd(gun, slot);
			}
		}

		public override void OnEventEndTimed()
		{
			if (AnnounceEvent)
			{
				var text = "Incoming Report:\nA weapons convoy got caught in a blue space anomaly near your location. ";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}
		}
	}
}
