using Chemistry.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items;
using ScriptableObjects;
using UnityEngine;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Random = UnityEngine.Random;
using WebSocketSharp;

[RequireComponent(typeof(ItemAttributesV2))]
[RequireComponent(typeof(ReagentContainer))]
public class DrinkableContainer : Consumable
{
	/// <summary>
	/// The name of the sound the player makes when drinking
	/// </summary>
	[Tooltip("The name of the sound the player makes when drinking (must be in soundmanager")]
	[SerializeField] private AddressableAudioSource drinkSound = null;

	private float RandomPitch => Random.Range( 0.7f, 1.3f );

	private ReagentContainer container;
	private ItemAttributesV2 itemAttributes;
	private RegisterItem item;

	private static readonly StandardProgressActionConfig ProgressConfig
		= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	private void Awake()
	{
		container = GetComponent<ReagentContainer>();
		itemAttributes = GetComponent<ItemAttributesV2>();
		item = GetComponent<RegisterItem>();
	}

	public override void TryConsume(Mind feeder, Mind eater)
	{
		if (!container)
			return;

		// todo: make seperate logic for NPC
		if (eater == null || feeder == null)
			return;

		// Check if container is empty
		var reagentUnits = container.ReagentMixTotal;
		if (reagentUnits <= 0f)
		{
			Chat.AddExamineMsgFromServer(eater, $"The {gameObject.ExpensiveName()} is empty.");
			return;
		}

		// Get current container name
		var name = itemAttributes ? itemAttributes.ArticleName : gameObject.ExpensiveName();
		// Generate message to player
		ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, HungerState.Hungry, name, "drink");

		if (feeder != eater)  //If you're feeding it to someone else.
		{
			//Wait 3 seconds before you can feed
			StandardProgressAction.Create(ProgressConfig, () =>
			{
				ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, HungerState.Hungry, name, "drink");
				Drink(eater, feeder);
			}).ServerStartProgress(eater.registerTile, 3f, feeder);
			return;
		}
		else
		{
			Drink(eater, feeder);
		}
	}

	private void Drink(Mind eater, Mind feeder)
	{
		// Start drinking reagent mix
		// todo: actually transfer reagent mix inside player stomach
		var drinkAmount = container.TransferAmount;
		container.TakeReagents(drinkAmount);

		DoDrinkEffects(eater, drinkAmount);

		// Play sound
		if (item && drinkSound != null)
		{
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(RandomPitch, spatialBlend: 1f);
			SoundManager.PlayNetworkedAtPos(drinkSound, eater.BodyWorldPosition, audioSourceParameters, sourceObj: eater.gameObject);
		}
	}

	private void DoDrinkEffects(Mind eater, float drinkAmount)
	{
		var playerEatDrinkEffects = eater.GetComponent<PlayerEatDrinkEffects>();

		if(playerEatDrinkEffects == null) return;

		if ((int) drinkAmount == 0) return;

		foreach (var reagent in container.CurrentReagentMix.reagents.m_dict)
		{
			//if its not alcoholic skip
			if (!AlcoholicDrinksSOScript.Instance.AlcoholicReagents.Contains(reagent.Key)) continue;

			//The more different types of alcohol in a drink the longer you get drunk for each sip.
			playerEatDrinkEffects.ServerSendMessageToClient(eater, (int)drinkAmount);
		}
	}
}
