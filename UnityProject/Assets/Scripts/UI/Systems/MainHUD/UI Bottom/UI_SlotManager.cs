using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;

public class UI_SlotManager : MonoBehaviour
{
	public List<UI_DynamicItemSlot> OpenSlots = new List<UI_DynamicItemSlot>();

	public Dictionary<IDynamicItemSlotS, List<GameObject>> BodyPartToSlot =
		new Dictionary<IDynamicItemSlotS, List<GameObject>>();

	public Dictionary<IDynamicItemSlotS, List<BodyPartUISlots.StorageCharacteristics>> PresentUISlots =
		new Dictionary<IDynamicItemSlotS, List<BodyPartUISlots.StorageCharacteristics>>();

	public GameObject Pockets;
	public GameObject SuitStorage;

	public GameObject Hands;

	public GameObject BeltPDABackpack;

	public GameObject Clothing;


	public GameObject SlotPrefab;

	public HandsController HandsController;

	public void Start()
	{
		EventManager.AddHandler(Event.LoggedOut, RemoveAll);
		EventManager.AddHandler(Event.PlayerSpawned, RemoveAll);
		EventManager.AddHandler(Event.RoundEnded, RemoveAll);
		EventManager.AddHandler(Event.PreRoundStarted, RemoveAll);
	}

	public void RemoveSpecifyedUISlot(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		if (PresentUISlots.ContainsKey(bodyPartUISlots) == false)
		{
			PresentUISlots[bodyPartUISlots] = new List<BodyPartUISlots.StorageCharacteristics>();
		}

		if (PresentUISlots[bodyPartUISlots].Contains(StorageCharacteristics))
		{
			PresentUISlots[bodyPartUISlots].Remove(StorageCharacteristics);
		}


		if (BodyPartToSlot.ContainsKey(bodyPartUISlots) == false)
			BodyPartToSlot[bodyPartUISlots] = new List<GameObject>();
		var namedItemSlot = bodyPartUISlots.RelatedStorage.GetNamedItemSlot(StorageCharacteristics.namedSlot);
		for (int i = 0; i < BodyPartToSlot[bodyPartUISlots].Count; i++)
		{
			var slot = BodyPartToSlot[bodyPartUISlots][i].OrNull()?.GetComponentInChildren<UI_DynamicItemSlot>();

			if (slot == null)
			{
				Logger.LogError(
					$"{bodyPartUISlots.RelatedStorage.OrNull()?.gameObject.ExpensiveName()} has null UI_DynamicItemSlot, slot: {StorageCharacteristics.namedSlot}");
				continue;
			}

			if (slot.ItemSlot == namedItemSlot)
			{
				OpenSlots.Remove(BodyPartToSlot[bodyPartUISlots][i].GetComponentInChildren<UI_DynamicItemSlot>());
				BodyPartToSlot[bodyPartUISlots][i].GetComponentInChildren<UI_DynamicItemSlot>().ReSetSlot();
				Destroy(BodyPartToSlot[bodyPartUISlots][i]);
				BodyPartToSlot[bodyPartUISlots].RemoveAt(i);
			}
		}


		if (BodyPartToSlot[bodyPartUISlots].Count == 0)
		{
			BodyPartToSlot.Remove(bodyPartUISlots);
		}

		if (StorageCharacteristics.SlotArea == SlotArea.Hands)
		{
			HandsController.RemoveHand(this,StorageCharacteristics);
		}
	}


	public void RemoveAll()
	{
		if (this == null) return;
		ReCalculate();
		return;
		// foreach (var Inslots in BodyPartToSlot.Keys.ToArray())
		// {
		// 	foreach (var Characteristics in Inslots.Storage)
		// 	{
		// 		RemoveSpecifyedUISlot(Inslots, Characteristics);
		// 	}
		// }
		//
		// BodyPartToSlot.Clear();
		//
		// HandsController.RemoveAllHands();
	}

	public void ReCalculate()
	{
		if (LocalPlayerManager.CurrentMind.OrNull()?.DynamicItemStorage)
		{
			List<KeyValuePair<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>> Removing =
				new List<KeyValuePair<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>>();

			List<KeyValuePair<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>> Adding =
				new List<KeyValuePair<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>>();

			foreach (var Characteristics in LocalPlayerManager.CurrentMind.DynamicItemStorage
				.GetCorrectContainedInventorys())
			{
				foreach (var Characteristic in Characteristics.Storage)
				{
					if (LocalPlayerManager.CurrentMind.DynamicItemStorage.GetNamedItemSlot(Characteristics.GameObject,Characteristic.namedSlot ) == null) continue;

					var Newslot = Characteristics.RelatedStorage.GetNamedItemSlot(Characteristic.namedSlot);
					bool add = true;
					if (Characteristic.NotPresentOnUI == false)
					{
						foreach (var dynamicItemSlot in OpenSlots)
						{

							if (dynamicItemSlot.ItemSlot == Newslot)
							{
								add = false;
								break;
							}
						}
					}
					else
					{
						add = false;
					}


					if (add)
					{
						Adding.Add(new KeyValuePair<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(
							Characteristics, Characteristic));
					}
				}

			}

			foreach (var dynamicItemSlot in OpenSlots)
			{
				bool remove = true;
				foreach (var Characteristics in LocalPlayerManager.CurrentMind.DynamicItemStorage
					.GetCorrectContainedInventorys())
				{
					foreach (var Characteristic in Characteristics.Storage)
					{
						if (dynamicItemSlot.ItemSlot == Characteristics.RelatedStorage.GetNamedItemSlot(Characteristic.namedSlot))
						{
							remove = false;
							break;
						}

					}
				}

				if (remove)
				{
					Removing.Add(new KeyValuePair<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(
						dynamicItemSlot.RelatedBodyPartUISlots, dynamicItemSlot.StorageCharacteristics));
				}

			}

			foreach (var remove in Removing)
			{
				RemoveSpecifyedUISlot(remove.Key, remove.Value);
			}

			foreach (var add in Adding)
			{
				AddIndividual(add.Key, add.Value);
			}
		}


	}

	public void AddIndividual(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics storageCharacteristicse)
	{
		if (PresentUISlots.ContainsKey(bodyPartUISlots) == false)
		{
			PresentUISlots[bodyPartUISlots] = new List<BodyPartUISlots.StorageCharacteristics>();
		}

		PresentUISlots[bodyPartUISlots].Add(storageCharacteristicse);

		if (storageCharacteristicse.SlotArea == SlotArea.Hands)
		{
			HandsController.AddHand(this,bodyPartUISlots, storageCharacteristicse);
		}
		else
		{
			var gameobjt = Instantiate(SlotPrefab);
			var NewSlot = gameobjt.GetComponentInChildren<UI_DynamicItemSlot>();
			NewSlot.SetupSlot(bodyPartUISlots, storageCharacteristicse);
			switch (storageCharacteristicse.SlotArea)
			{
				case SlotArea.Pockets:
					gameobjt.transform.SetParent(Pockets.transform);
					break;
				case SlotArea.SuitStorage:
					gameobjt.transform.SetParent(SuitStorage.transform);
					break;
				case SlotArea.BeltPDABackpack:
					gameobjt.transform.SetParent(BeltPDABackpack.transform);
					break;
				case SlotArea.Clothing:
					gameobjt.transform.SetParent(Clothing.transform);
					break;
			}

			// if (ClientContents.ContainsKey(storageCharacteristicse.SlotArea) == false) ClientContents[storageCharacteristicse.SlotArea] = new List<UI_DynamicItemSlot>();
			// ClientContents[storageCharacteristicse.SlotArea].Add(NewSlot);
			gameobjt.transform.localScale = Vector3.one;
			if (BodyPartToSlot.ContainsKey(bodyPartUISlots) == false)
				BodyPartToSlot[bodyPartUISlots] = new List<GameObject>();
			BodyPartToSlot[bodyPartUISlots].Add(gameobjt);

			OpenSlots.Add(NewSlot);
		}
	}

	public enum SlotArea
	{
		Pockets,
		SuitStorage,
		Hands,
		BeltPDABackpack,
		Clothing
	}
}