using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Mirror;
using Antagonists;
using Systems.Spells;
using Gateway;
using HealthV2;
using Initialisation;
using Player;
using Player.Movement;
using ScriptableObjects.Audio;
using UI.Action;
using ScriptableObjects.Systems.Spells;
using UnityEngine.Serialization;

/// <summary>
/// IC character information (job role, antag info, real name, etc). A body and their ghost link to the same mind
/// SERVER SIDE VALID ONLY, is not sync'd
/// </summary>
public class Mind : NetworkBehaviour, IActionGUI
{


	// public GameObject gameObject
	// {
	// 	get
	// 	{
	// 		Logger.LogError("Mind gameObject");
	// 		return null;
	// 	}
	// }


	// public Transform transform
	// {
	// 	get
	// 	{
	// 		Logger.LogError("Mind Transform");
	// 		return null;
	// 	}
	// }

	// public T GetComponent<T>()
	// {
	// 	Logger.LogError("Mind GetComponent");
	// 	T bob = new T();
	// 	return bob;
	// }

	// public bool TryGetComponent<T>(out T component)
	// {
	// 	return false;
	// }

	public ConnectedPlayer AssignedPlayer;

	public Occupation occupation;

	[SyncVar]
	public GameObject ghost;

	[SyncVar(hook = nameof(SynchronisePhysicalBrainuint))] public uint BrainID;


	[FormerlySerializedAs("PhysicalBrain")] public PlayerBrain physicalPlayerBrain;

	[SyncVar]
	public GameObject PossessingObject;


	public DynamicItemStorage DynamicItemStorage => GameObjectBody.GetComponent<DynamicItemStorage>();

	public Equipment Equipment => GameObjectBody.GetComponent<Equipment>();
	public IProvideConsciousness GetConsciousness => GameObjectBody.GetComponent<IProvideConsciousness>();

	public PlayerSync PlayerSync => GameObjectBody.GetComponent<PlayerSync>();
	public Orientation CurrentDirection => GameObjectBody.GetComponent<Directional>().CurrentDirection;

	public WeaponNetworkActions WeaponNetworkActions => GameObjectBody.GetComponent<WeaponNetworkActions>();

	public PlayerNetworkActions playerNetworkActions; //Present on the mind game object

	public PlayerNetworkActions PlayerNetworkActions => playerNetworkActions;

	public PlayerEatDrinkEffects PlayerEatDrinkEffects;


	public void SynchronisePhysicalBrainuint(uint old, uint unew)
	{
		BrainID = unew;

		if (NetworkIdentity.spawned.ContainsKey(BrainID) == false) return;

		physicalPlayerBrain = NetworkIdentity.spawned[BrainID].GetComponent<PlayerBrain>();
	}

	public GameObject GameObjectBody
	{
		get
		{
			if (IsGhosting)
			{
				if (ghost != null)
				{
					return ghost;
				}
				Logger.LogError(OriginalCharacter.Name + " Does not have a ghost body ");
				return null;
			}

			if (this.physicalPlayerBrain != null)
			{
				return physicalPlayerBrain.PhysicalBody;
			}

			if (PossessingObject != null)
			{
				return PossessingObject;
			}


			if (ghost != null)
			{
				return ghost.gameObject;
			}

			if (OriginalCharacter != null)
			{
				Logger.LogError(OriginalCharacter.Name + " Does not have a ghost body ");
			}

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



	private void SetBody(GameObject gameObject)
	{
		if (gameObject.TryGetComponent<PlayerBrain>(out var Brain))
		{
			SynchronisePhysicalBrainuint(0, Brain.netId);
			physicalPlayerBrain = Brain;
			Brain.RelatedMind = this;
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

	public RegisterTile RegisterTile()
	{
		return registerTile;
	}


	public PlayerMove PlayerMove => GameObjectBody.GetComponent<PlayerMove>();

	public bool CanSpeak = true;


	public bool IsSilicon; //Speaks in a silicon way
	public bool IsRestrained; //Basically is not allowed to interact, can apply to anything

	public bool IdentityVisible
	{
		get
		{
			var TEquipment = Equipment;
			if (TEquipment != null)
			{
				return TEquipment.IsIdentityVisible();
			}
			else
			{
				return true;
			}
		}
	}

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

	public string CharactersName => OriginalCharacter?.Name;

	private SpawnedAntag antag;
	public bool IsAntag => antag != null;

	[SyncVar]
	public bool IsGhosting;

	public bool DenyCloning;
	public int bodyID; //ues Server instance ID on Position game object





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
		transform.SetParent(MindManager.Instance.transform);

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

	public ItemSlot GetActiveHandSlot()
	{
		return DynamicItemStorage.OrNull()?.GetActiveHandSlot(this);
	}

	public bool HasThisBody(GameObject Body)
	{
		if (Body == this.gameObject) return true;

		if (physicalPlayerBrain != null)
		{
			if (physicalPlayerBrain.ConnectedBody == Body)
			{
				return true;
			}
			else if (PossessingObject == Body)
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

	[SyncVar]
	public Transform CameraFollowOverride = null;

	public Transform CameraFollowTarget()
	{
		if (CameraFollowOverride != null)
		{
			return CameraFollowOverride;
		}

		if (IsGhosting)
		{
			if (ghost != null)
			{
				return ghost.transform;
			}
			Logger.LogError(OriginalCharacter.Name + " Does not have a ghost body ");
			return null;
		}

		if (this.physicalPlayerBrain != null)
		{
			return physicalPlayerBrain.CameraFollowTarget();
		}

		if (PossessingObject != null)
		{
			return PossessingObject.transform;
		}


		if (ghost != null)
		{
			return ghost.gameObject.transform;
		}

		if (OriginalCharacter != null)
		{
			Logger.LogError(OriginalCharacter.Name + " Does not have a ghost body ");
		}

		return null;
	}

	public void RemoveBody()
	{
		ClearOldBody();
	}



	public void SetGhost(GameObject Ghost)
	{
		//LoadManager.RegisterActionDelayed( () => PlayerSpawn.ServerAssignPlayerAuthorityBody(this, Ghost) ,2  ); ;
		PlayerSpawn.ServerAssignPlayerAuthorityBody(this, Ghost);
		ghost = Ghost;
	}

	public void PossessNewObject(GameObject InPossessingObject, bool addRoleAbilities = false)
	{
		PlayerSpawn.ServerAssignPlayerAuthorityBody(this, InPossessingObject);

		ClearOldBody();
		SetBody(InPossessingObject);

		SetPlayerControl(AssignedPlayer.Connection, InPossessingObject);

		if (PossessingObject.TryGetComponent<PlayerBrain>(out var newBrain))
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



	[TargetRpc]
	public void SetPlayerControl(NetworkConnection target, GameObject InPossessingObject)
	{
		LocalPlayerManager.SetPlayerForControl( InPossessingObject.GetComponent<IPlayerControllable>());
	}



	private void ClearOldBody()
	{
		PossessingObject = null;
		if (physicalPlayerBrain != null)
		{
			physicalPlayerBrain.RelatedMind = null;
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

	public void Ghost(Vector3? NullGhostCoordinates = null)
	{
		if (NullGhostCoordinates != null)
		{
			NullGhostCoordinates = NullGhostCoordinates.Value;
		}
		else
		{
			NullGhostCoordinates = BodyWorldPosition;
		}
		IsGhosting = true;
		if (NullGhostCoordinates.Value != TransformState.HiddenPos)
		{
			var playerSync = GameObjectBody.GetComponent<PlayerSync>();
			if (playerSync != null)
			{
				playerSync.DisappearFromWorldServer();
				playerSync.AppearAtPositionServer(NullGhostCoordinates.Value);
				playerSync.RollbackPrediction();
			}
		}

		RPCGhost(AssignedPlayer.OrNull()?.Connection);
	}

	[TargetRpc]
	public void RPCGhost(NetworkConnection target)
	{
		EventManager.Broadcast( Event.GhostSpawned);
		LocalPlayerManager.SetPlayerForControl( ghost.GetComponent<IPlayerControllable>());

		if (PlayersManager.Instance.IsClientAdmin)
		{
			UIManager.LinkUISlots(ItemStorageLinkOrigin.adminGhost); //TODO
		}
		// stop the crit notification and change overlay to ghost mode
		SoundManager.Stop("Critstate");
		UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
		// show ghosts
		var mask = Camera2DFollow.followControl.cam.cullingMask;
		mask |= 1 << LayerMask.NameToLayer("Ghosts");
		Camera2DFollow.followControl.cam.cullingMask = mask;
	}

	public void StopGhosting()
	{
		if (physicalPlayerBrain == null)
		{
			if (PossessingObject != null)
			{
				IsGhosting = false;
			}
		}
		else
		{
			physicalPlayerBrain.ReEnterBody();
			IsGhosting = false;
		}
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
		var ChatChannels = GameObjectBody.GetComponent<IProvideChatChannel>();
		if (ChatChannels == null) return  ChatChannel.Examine | ChatChannel.System | ChatChannel.Combat |
		                                  ChatChannel.Binary | ChatChannel.Command | ChatChannel.Common | ChatChannel.Engineering |
		                                  ChatChannel.Medical | ChatChannel.Science | ChatChannel.Security | ChatChannel.Service
		                                  | ChatChannel.Supply | ChatChannel.Syndicate | ChatChannel.Ghost | ChatChannel.OOC; //TODO Temporary
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

	[SerializeField] private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		playerNetworkActions.CmdAskforAntagObjectives();
	}

	public void ActivateAntagAction(bool state)
	{
		UIActionManager.ToggleLocal(this, state);
	}

	#region Pronouns

	private CharacterSettings GetCorrectCharacter()
	{
		var Sprites = GameObjectBody.GetComponent<PlayerSprites>(); //TODO Probably better class to handle this
		CharacterSettings ChosenCharacter = null;
		if (Sprites == null) //if the player Customisable sprites, they could be possessing someone else's body
		{
			ChosenCharacter = Sprites.OriginalCharacter;
		}
		else
		{
			ChosenCharacter = this.OriginalCharacter;
		}

		return ChosenCharacter;
	}

	/// <summary>
	/// Returns a possessive string (i.e. "their", "his", "her") for the provided gender enum.
	/// </summary>
	public string TheirPronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.TheirPronoun(IdentityVisible);
	}


	/// <summary>
	/// Returns a personal pronoun string (i.e. "he", "she", "they") for the provided gender enum.
	/// </summary>
	public string TheyPronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.TheyPronoun(IdentityVisible);
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "him", "her", "them") for the provided gender enum.
	/// </summary>
	public string ThemPronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.ThemPronoun(IdentityVisible);
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "he's", "she's", "they're") for the provided gender enum.
	/// </summary>
	public string TheyrePronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.TheyrePronoun(IdentityVisible);
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "himself", "herself", "themself") for the provided gender enum.
	/// </summary>
	public string ThemselfPronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.ThemselfPronoun(IdentityVisible);
	}

	public string IsPronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.IsPronoun(IdentityVisible);
	}

	public string HasPronoun()
	{
		CharacterSettings ChosenCharacter = GetCorrectCharacter();
		return ChosenCharacter.HasPronoun(IdentityVisible);
	}

	#endregion
}

public static partial class CustomReadWriteFunctions
{
	public static void WriteMind(this NetworkWriter writer, Mind value)
	{
		var Net = value.OrNull()?.gameObject.OrNull()?.GetComponent<NetworkIdentity>();
		if (Net == null)
		{
			writer.WriteNetworkIdentity(null);
		}
		else
		{
			writer.WriteNetworkIdentity(Net);
		}

	}

	public static Mind ReadMind(this NetworkReader reader)
	{
		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		Mind mind = networkIdentity != null ? networkIdentity.GetComponent<Mind>() : null;
		return mind;
	}
}