using System;
using UnityEngine;
using System.Threading.Tasks;
using HealthV2;
using Items;

namespace Items.Food
{
	[RequireComponent(typeof(RegisterItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(Edible))]
	public class XenomorphFood : Edible
	{
		[SerializeField]
		private int killTime = 400;
		[SerializeField]
		private GameObject larvae = null;

		private string Name => itemAttributes.ArticleName;
		private static readonly StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

		public override void TryConsume(Mind feeder, Mind eater)
		{
			if (eater == null)
			{
				// TODO: implement non-player eating
				//SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos);
				_ = Despawn.ServerSingle(gameObject);
				return;
			}

			// Show eater message
			var eaterHungerState = eater.LivingHealthMasterBase.HungerState;
			ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, eaterHungerState, Name, "eat");

			// Check if eater can eat anything
			if (feeder != eater)  //If you're feeding it to someone else.
			{
				//Wait 3 seconds before you can feed
				StandardProgressAction.Create(ProgressConfig, () =>
				{
					ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
					Eat(eater, feeder);
				}).ServerStartProgress(eater.registerTile, 3f, feeder);
				return;
			}
			else
			{
				Eat(eater, feeder);
			}
		}

		public override void Eat(Mind eater, Mind feeder)
		{
			// TODO: missing sound?
			//SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

			var stomachs = eater.LivingHealthMasterBase.GetStomachs();
			if (stomachs.Count == 0)
			{
				//No stomachs?!
				return;
			}
			FoodContents.Divide(stomachs.Count);
			foreach (var stomach in stomachs)
			{
				stomach.StomachContents.Add(FoodContents.CurrentReagentMix.Clone());
			}

			_ = Pregnancy(eater);
			var feederSlot = feeder.GetActiveHandSlot();
			Inventory.ServerDespawn(feederSlot);
		}

		private async Task Pregnancy(Mind player)
		{
			await Task.Delay(TimeSpan.FromSeconds(killTime - (killTime / 8)));
			Chat.AddActionMsgToChat(player, "Your stomach gurgles uncomfortably...",
				$"A dangerous sounding gurgle emanates from " + player.name + "!");
			await Task.Delay(TimeSpan.FromSeconds(killTime / 8));
			player.LivingHealthMasterBase.ApplyDamageToBodyPart(
				gameObject,
				200,
				AttackType.Internal,
				DamageType.Brute,
				BodyPartType.Chest);
			Spawn.ServerPrefab(larvae, player.RegisterTile().WorldPositionServer);
		}
	}
}
