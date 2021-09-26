using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MindManager : MonoBehaviour
{
	public static MindManager Instance;

	//TODO Clean on around end
	public List<Mind> PresentMinds = new List<Mind>();

	public GameObject MindPrefab;

	public List<Mind> NonAntagMinds =>
		PresentMinds.FindAll(mind => mind.IsAntag == false);

	public List<Mind> AntagMinds =>
		PresentMinds.FindAll(mind => mind.IsAntag);

	public void Start()
	{
		Instance = this;
	}

	[Server]
	public Mind Get(GameObject byGameObject, bool includeOffline = false)
	{

		foreach (var mind in PresentMinds)
		{
			if (mind.AssignedPlayer.OrNull()?.Connection != null || includeOffline)
			{
				if (mind.HasThisBody(byGameObject))
				{
					return mind;
				}
			}
		}

		return null;
	}

	[Server]
	public static Mind StaticGet(GameObject byGameObject, bool includeOffline = false)
	{
		return Instance.Get(byGameObject, includeOffline );
	}


	public Mind GetNewMind()
	{
		var Mindspawn =  Spawn.ServerPrefab(MindPrefab, Vector3.zero, this.transform).GameObject;
		var outMind = Mindspawn.GetComponent<Mind>();
		PresentMinds.Add(outMind);
		return outMind;
	}

	public Mind GetNewMindAndSetOccupation(Occupation occupation, CharacterSettings characterSettings)
	{
		var outMind = GetNewMind();
		outMind.OriginalCharacter = characterSettings;
		outMind.occupation = occupation;
		outMind.gameObject.name = characterSettings.Name;
		return outMind;
	}

	public Mind GetNewMindForGhost(CharacterSettings characterSettings)
	{
		var outMind = GetNewMind();
		outMind.gameObject.name = characterSettings.Name;
		outMind.OriginalCharacter = characterSettings;

		return outMind;
	}
}
