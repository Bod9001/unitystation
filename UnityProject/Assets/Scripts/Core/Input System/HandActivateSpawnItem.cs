using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using AddressableReferences;


namespace Items
{
	public class HandActivateSpawnItem : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField, FormerlySerializedAs("SeedPacket")]
		private GameObject seedPacket = default;

		[SerializeField, FormerlySerializedAs("DeleteItemOnUse")]
		private bool deleteItemOnUse = true;

		[SerializeField]
		private AddressableAudioSource spawnSound = default;

		public void ServerPerformInteraction(HandActivate interaction)
		{
			SoundManager.PlayNetworkedAtPos(spawnSound, interaction.Performer.BodyWorldPosition, sourceObj: interaction.Performer.GameObjectBody);

			var obj = Spawn.ServerPrefab(seedPacket, interaction.Performer.BodyWorldPosition, parent: interaction.Performer.GameObjectBody.transform.parent).GameObject;
			var attributes = obj.GetComponent<ItemAttributesV2>();
			if (attributes != null)
			{
				Inventory.ServerAdd(obj, interaction.HandSlot, deleteItemOnUse ? ReplacementStrategy.DespawnOther : ReplacementStrategy.DropOther);
			}
			else if (deleteItemOnUse)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
