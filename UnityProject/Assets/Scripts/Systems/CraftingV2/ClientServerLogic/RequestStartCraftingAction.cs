using System.Collections.Generic;
using Items;
using Messages.Client;
using Mirror;

namespace Systems.CraftingV2.ClientServerLogic
{
	/// <summary>
	/// 	A client asks a server to craft a client's selected recipe. This ClientMessage is designed to handle it.
	/// </summary>
	public class RequestStartCraftingAction : ClientMessage<RequestStartCraftingAction.NetMessage>
	{
		private CraftingActionParameters craftingActionParameters = new CraftingActionParameters(
			true,
			true
		);

		public struct NetMessage : NetworkMessage
		{
			// recipe index in the recipes singleton
			public int CraftingRecipeIndex;
			public bool IsRecipeIndexWrong;
		}

		public override void Process(NetMessage netMessage)
		{
			if (
				Cooldowns.TryStartServer(
					SentByPlayer.CurrentMind,
					CommonCooldowns.Instance.Interaction
				) == false
			)
			{
				return;
			}

			if (netMessage.CraftingRecipeIndex < 0)
			{
				Logger.LogError(
					$"Received the negative recipe index when {SentByPlayer.Username} " +
					"had tried to craft something. Perhaps some recipe is missing from the singleton."
				);
				return;
			}

			if (netMessage.IsRecipeIndexWrong)
			{
				Logger.LogError(
					$"Received the wrong recipe index when {SentByPlayer.Username} had tried to craft something. " +
					"Perhaps some recipe has wrong indexInSingleton that doesn't match a real index in the singleton."
				);
				return;
			}

			// at the moment we already know that there are enough ingredients and
			// tools(checked on the client side), so we'll ignore them.
			SentByPlayer.CurrentMind.PlayerCrafting.TryToStartCrafting(
				CraftingRecipeSingleton.Instance.GetRecipeByIndex(netMessage.CraftingRecipeIndex),
				null,
				null,
				SentByPlayer.CurrentMind.PlayerCrafting.GetReagentContainers(),
				craftingActionParameters
			);
		}

		public static void Send(CraftingRecipe craftingRecipe)
		{
			if (
				Cooldowns.TryStartClient(
					LocalPlayerManager.CurrentMind,
					CommonCooldowns.Instance.Interaction
				) == false
			)
			{
				return;
			}

			// if sending a wrong recipe index...
			if (
				craftingRecipe.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes()
			    || CraftingRecipeSingleton.Instance.GetRecipeByIndex(craftingRecipe.IndexInSingleton)
			    != craftingRecipe
			)
			{
				Send(new NetMessage
				{
					CraftingRecipeIndex = craftingRecipe.IndexInSingleton,
					IsRecipeIndexWrong = true
				});
			}

			Send(new NetMessage
			{
				CraftingRecipeIndex = craftingRecipe.IndexInSingleton,
				IsRecipeIndexWrong = false
			});
		}
	}
}