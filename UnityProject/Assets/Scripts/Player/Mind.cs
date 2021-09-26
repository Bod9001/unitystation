using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Mirror;
using Antagonists;
using Systems.Spells;
using HealthV2;
using Player;
using Player.Movement;
using ScriptableObjects.Audio;
using UI.Action;
using ScriptableObjects.Systems.Spells;

/// <summary>
/// IC character information (job role, antag info, real name, etc). A body and their ghost link to the same mind
/// SERVER SIDE VALID ONLY, is not sync'd
/// </summary>
public class Mind : MonoBehaviour, IActionGUI
{
	public ConnectedPlayer AssignedPlayer;

	public Occupation occupation;
	public GameObject ghost;

	public Brain PhysicalBrain;
	public GameObject PossessingObject;

	public DynamicItemStorage DynamicItemStorage => GameObjectBody.GetComponent<DynamicItemStorage>();

	public Equipment Equipment => GameObjectBody.GetComponent<Equipment>();
	public IProvideConsciousness GetConsciousness => GameObjectBody.GetComponent<IProvideConsciousness>();

	public PlayerSync PlayerSync =>  GameObjectBody.GetComponent<PlayerSync>();
	public Orientation CurrentDirection => GameObjectBody.GetComponent<Directional>().CurrentDirection;

	public PlayerNetworkActions playerNetworkActions; //Present on the mind game object

	public GameObject GameObjectBody
	{
		get
		{
			if (this.PhysicalBrain != null)
			{
				return PhysicalBrain.PhysicalBody;
			}

			if (PossessingObject != null)
			{
				return PossessingObject;
			}

			if (ghost != null)
			{
				return ghost.gameObject;
			}

			Logger.LogError(OriginalCharacter.Name + " Does not have a ghost body ");
			return null;
		}
	}

	public PushPull PushPull => GameObjectBody.GetComponent<PushPull>();

	public PushPull pushPull => PushPull;
	public PlayerCrafting PlayerCrafting;

	public LivingHealthMasterBase LivingHealthMasterBase => GameObjectBody.GetComponent<LivingHealthMasterBase>();

	public LivingHealthMasterBase playerHealth => LivingHealthMasterBase;

	public Vector3 BodyWorldPosition => GameObjectBody.AssumedWorldPosServer();

	public Vector3Int BodyWorldPositionInt => GameObjectBody.AssumedWorldPosServer().RoundToInt();

	public bool allowInput //TODO Can't think of better system for this Come up with better system
		//Something that's body diagnostic but also not hardcoded everything
	{
		get
		{
			var PlayerMove = GameObjectBody.GetComponent<PlayerMove>();
			if (PlayerMove != null)
			{
				return PlayerMove.allowInput;
			}
			else
			{
				return true;
			}

		}
	}


	public void SetGhost(GameObject Ghost)
	{
		ghost = Ghost;
	}

	//TODO Need to check for ghost
	public void SetBody(GameObject gameObject)
	{
		if (gameObject.TryGetComponent<Brain>(out var Brain))
		{
			PhysicalBrain = Brain;
			PossessingObject = gameObject;
		}
		else
		{
			PossessingObject = gameObject;
		}
	}

	public HasCooldowns Cooldown;

	public bool IsHidden => GameObjectBody.transform.position == TransformState.HiddenPos;


	public RegisterTile registerTile => GameObjectBody.GetComponent<RegisterTile>();

	public RegisterPlayer RegisterPlayer => registerTile as RegisterPlayer;

	public RegisterTile RegisterTile() { return registerTile; }


	public PlayerMove PlayerMove => GameObjectBody.GetComponent<PlayerMove>();

	public bool CanSpeak;
	public bool IsSilicon; //Speaks in a silicon way
	public bool IsRestrained; //Basically is not allowed to interact, can apply to anything

	public bool IdentityVisible; //Can we see who they are and what their pronouns are

	public Speech Speech;
	public ChatModifier SpeechModifiers = ChatModifier.None;

	public ChatChannel DefaultChatChannel
	{
		get
		{
			// Player is some spooky ghost?
			if (IsGhosting)
			{
				return ChatChannel.Ghost;
			}
			else
			{
				return InternalDefaultChatChannel;
			}
		}
	}

	public ChatChannel InternalDefaultChatChannel;

	public CharacterSettings OriginalCharacter;

	public JobType JobType => occupation.JobType;

	public string CharactersName => OriginalCharacter.Name;

	private SpawnedAntag antag;
	public bool IsAntag => antag != null;
	public bool IsGhosting;
	public bool DenyCloning;
	public int bodyID; //ues Server instance ID on Position game object


	//TODO Change to the body
	public FloorSounds StepSound;
	public FloorSounds SecondaryStepSound;


	// Current way to check if it's not actually a ghost but a spectator, should set this not have it be the below.
	public bool IsSpectator => occupation == null || PossessingObject == null;

	public bool ghostLocked;

	private ObservableCollection<Spell> spells = new ObservableCollection<Spell>();
	public ObservableCollection<Spell> Spells => spells;

	/// <summary>
	/// General purpose properties storage for misc stuff like job-specific flags
	/// </summary>
	private Dictionary<string, object> properties = new Dictionary<string, object>();

	public PlayerOnlySyncValues PlayerOnlySyncValues;

	public bool IsMiming
	{
		get => GetPropertyOrDefault("vowOfSilence", false);
		set => SetProperty("vowOfSilence", value);
	}

	public void Awake()
	{
		//TODO If client set Parent to the mind manager

		// add spell to the UI bar as soon as they're added to the spell list
		spells.CollectionChanged += (sender, e) =>
		{
			if (e == null)
			{
				return;
			}

			if (e.NewItems != null)
			{
				foreach (Spell x in e.NewItems)
				{
					UIActionManager.Toggle(x, true, this);
				}
			}

			if (e.OldItems != null)
			{
				foreach (Spell y in e.OldItems)
				{
					UIActionManager.Toggle(y, false, this);
				}
			}
		};
	}

	public bool HasThisBody(GameObject Body)
	{
		if (Body == this.gameObject) return true;

		if (PhysicalBrain != null)
		{
			if (PhysicalBrain.ConnectedBody == true)
			{
				return true;
			}
			else if (PossessingObject == Body) //PossessingObject == Brain. Game object
			{
				return true;
			}
		}

		if (ghost == Body)
		{
			return true;
		}

		return false;
	}


	public PlayerPronoun GetPronouns()
	{
		if (IdentityVisible)
		{
			return OriginalCharacter.PlayerPronoun;
		}
		else
		{
			return PlayerPronoun.They_them;
		}
	}

	public string ExpensiveName()
	{
		if (IdentityVisible)
		{
			return CharactersName;
		}
		else
		{
			return GameObjectBody.ExpensiveName(); //Assuming Let the game object name is set up correctly
		}
	}
	public void PossessNewBody(GameObject InPossessingObject, bool addRoleAbilities = false)
	{
		ClearOldBody();
		PossessingObject = InPossessingObject;
		bodyID = InPossessingObject.GetInstanceID();

		if (PossessingObject.TryGetComponent<Brain>(out var newBrain))
		{
			if (occupation != null && addRoleAbilities)
			{
				foreach (var spellData in occupation.Spells)
				{
					var spellScript = spellData.AddToPlayer(this);
					Spells.Add(spellScript);
				}

				foreach (var pair in occupation.CustomProperties)
				{
					SetProperty(pair.Key, pair.Value);
				}
			}
		}

		StopGhosting();
	}

	private void ClearOldBody()
	{
		PossessingObject = null;
		if (PhysicalBrain != null)
		{
			PhysicalBrain.RelatedMind = null;
		}
	}

	/// <summary>
	/// Make this mind a specific spawned antag
	/// </summary>
	public void SetAntag(SpawnedAntag newAntag)
	{
		antag = newAntag;
		ShowObjectives();
		PlayerOnlySyncValues.ServerSetAntag(true);
	}

	/// <summary>
	/// Remove the antag status from this mind
	/// </summary>
	public void RemoveAntag()
	{
		antag = null;
		PlayerOnlySyncValues.ServerSetAntag(true);
	}

	public void Ghost()
	{
		IsGhosting = true;
	}

	public void StopGhosting()
	{
		IsGhosting = false;
	}

	/// <summary>
	/// Get the cloneable status of the player's mind, relative to the passed mob ID.
	/// </summary>
	public CloneableStatus GetCloneableStatus(int recordMobID)
	{
		//TODO Check health
		if (DenyCloning)
		{
			return CloneableStatus.DenyingCloning;
		}

		if (IsOnline() == false)
		{
			return CloneableStatus.Offline;
		}

		return CloneableStatus.Cloneable;
	}

	public bool IsOnline()
	{
		return PlayersManager.Instance.ContainsConnection(AssignedPlayer.Connection);
	}

	/// <summary>
	/// Show the the player their current objectives if they have any
	/// </summary>
	public void ShowObjectives()
	{
		if (IsAntag == false) return;

		Chat.AddExamineMsgFromServer(this, antag.GetObjectivesForPlayer());
	}

	/// <summary>
	/// Simply returns what antag the player is, if any
	/// </summary>
	public SpawnedAntag GetAntag()
	{
		return antag;
	}

	/// <summary>
	/// Returns true if the given mind is of the given Antagonist type.
	/// </summary>
	/// <typeparam name="T">The type of antagonist to check against</typeparam>
	public bool IsOfAntag<T>() where T : Antagonist
	{
		if (IsAntag == false) return false;

		return antag.Antagonist is T;
	}

	public void AddSpell(Spell spell)
	{
		if (spells.Contains(spell))
		{
			return;
		}

		spells.Add(spell);
	}

	public void RemoveSpell(Spell spell)
	{
		if (spells.Contains(spell))
		{
			spells.Remove(spell);
		}
	}

	public Spell GetSpellInstance(SpellData spellData)
	{
		foreach (Spell spell in Spells)
		{
			if (spell.SpellData == spellData)
			{
				return spell;
			}
		}

		return default;
	}

	public bool HasSpell(SpellData spellData)
	{
		return GetSpellInstance(spellData) != null;
	}

	public void ResendSpellActions()
	{
		foreach (Spell spell in Spells)
		{
			UIActionManager.Toggle(spell, true, this);
		}
	}

	public void SetProperty(string key, object value)
	{
		if (properties.ContainsKey(key))
		{
			properties[key] = value;
		}
		else
		{
			properties.Add(key, value);
		}
	}

	public T GetPropertyOrDefault<T>(string key, T defaultValue)
	{
		return properties.GetOrDefault(key, defaultValue) is T typedProperty ? typedProperty : defaultValue;
	}

	public DynamicItemStorage GetStorage()
	{
		return GameObjectBody.GetComponent<DynamicItemStorage>();
	}


	public ChatChannel GetAvailableChannelsMask(bool transmitOnly = true)
	{
		return GameObjectBody.GetComponent<IProvideChatChannel>().GetAvailableChannelsMask(transmitOnly); //TODO
		/*if (IsDeadOrGhost && !IsPlayerSemiGhost)
		{
			ChatChannel ghostTransmitChannels = ChatChannel.Ghost | ChatChannel.OOC;
			ChatChannel ghostReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat |
				ChatChannel.Binary | ChatChannel.Command | ChatChannel.Common | ChatChannel.Engineering |
				ChatChannel.Medical | ChatChannel.Science | ChatChannel.Security | ChatChannel.Service
				| ChatChannel.Supply | ChatChannel.Syndicate;

			if (transmitOnly)
			{
				return ghostTransmitChannels;
			}
			return ghostTransmitChannels | ghostReceiveChannels;
		}

		if (playerState == PlayerStates.Ai)
		{
			ChatChannel aiTransmitChannels = ChatChannel.OOC | ChatChannel.Local | ChatChannel.Binary | ChatChannel.Command
			                                 | ChatChannel.Common | ChatChannel.Engineering |
			                                 ChatChannel.Medical | ChatChannel.Science | ChatChannel.Security | ChatChannel.Service
			                                 | ChatChannel.Supply;
			ChatChannel aiReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat |
			                                   ChatChannel.Binary | ChatChannel.Command | ChatChannel.Common | ChatChannel.Engineering |
			                                   ChatChannel.Medical | ChatChannel.Science | ChatChannel.Security | ChatChannel.Service
			                                   | ChatChannel.Supply;

			if (GetComponent<AiPlayer>().AllowRadio == false)
			{
				aiTransmitChannels = ChatChannel.OOC | ChatChannel.Local;
				aiReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat;
			}

			if (transmitOnly)
			{
				return aiTransmitChannels;
			}
			return aiTransmitChannels | aiReceiveChannels;
		}

		if (playerState == PlayerStates.Blob)
		{
			ChatChannel blobTransmitChannels = ChatChannel.Blob | ChatChannel.OOC;
			ChatChannel blobReceiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat;

			if (transmitOnly)
			{
				return blobTransmitChannels;
			}

			return blobTransmitChannels | blobReceiveChannels;
		}

		//TODO: Checks if player can speak (is not gagged, unconcious, has no mouth)
		ChatChannel transmitChannels = ChatChannel.OOC | ChatChannel.Local;

		var playerStorage = gameObject.GetComponent<DynamicItemStorage>();
		if (playerStorage != null)
		{
			foreach (var earSlot in playerStorage.GetNamedItemSlots(NamedSlot.ear))
			{
				if(earSlot.IsEmpty) continue;
				if(earSlot.Item.TryGetComponent<Headset>(out var headset) == false) continue;

				EncryptionKeyType key = headset.EncryptionKey;
				transmitChannels = transmitChannels | EncryptionKey.Permissions[key];
			}
		}

		ChatChannel receiveChannels = ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat;

		if (transmitOnly)
		{
			return transmitChannels;
		}

		return transmitChannels | receiveChannels;
		*/
	}

	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		playerNetworkActions.CmdAskforAntagObjectives();
	}

	public void ActivateAntagAction(bool state)
	{
		UIActionManager.ToggleLocal(this, state);
	}
}

public static class CustomReadWriteFunctions
{
	public static void WriteMyCollision(this NetworkWriter writer, Mind value)
	{
		writer.WriteNetworkIdentity(value.gameObject.GetComponent<NetworkIdentity>());
	}

	public static Mind ReadMyCollision(this NetworkReader reader)
	{
		Vector3 force = reader.ReadVector3();

		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		Mind mind = networkIdentity != null
			? networkIdentity.GetComponent<Mind>()
			: null;
		return mind;
	}
}