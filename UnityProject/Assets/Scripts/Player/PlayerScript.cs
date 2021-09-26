using System.Text;
using Systems.Ai;
using UnityEngine;
using Mirror;
using Blob;
using HealthV2;
using UI;
using Player;
using Player.Movement;
using UI.Action;

public class PlayerScript : NetworkBehaviour, IMatrixRotation, IAdminInfo
{
	/// maximum distance the player needs to be to an object to interact with it
	public const float interactionDistance = 1.5f;

	public Mind mind;

	[HideInInspector, SyncVar(hook = nameof(SyncPlayerName))] public string playerName = " ";


	public PlayerNetworkActions playerNetworkActions { get; set; }

	public WeaponNetworkActions weaponNetworkActions { get; set; }

	public Orientation CurrentDirection => playerDirectional.CurrentDirection;
	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public PlayerHealthV2 playerHealth { get; set; }

	public PlayerMove playerMove { get; set; }
	public PlayerSprites playerSprites { get; set; }

	/// <summary>
	/// Will be null if player is a ghost.
	/// </summary>
	public ObjectBehaviour pushPull { get; set; }

	public Directional playerDirectional { get; set; }

	private PlayerSync _playerSync; //Example of good on-demand reference init
	public PlayerSync PlayerSync => _playerSync ? _playerSync : (_playerSync = GetComponent<PlayerSync>());

	public Equipment Equipment { get; private set; }

	public RegisterPlayer registerTile { get; set; }

	public PlayerOnlySyncValues PlayerOnlySyncValues { get; private set; }

	public HasCooldowns Cooldowns { get; set; }

	public MouseInputController mouseInputController { get; set; }

	public ChatIcon chatIcon { get; private set; }

	/// <summary>
	/// Serverside world position.
	/// Outputs correct world position even if you're hidden (e.g. in a locker)
	/// </summary>
	public Vector3Int AssumedWorldPos => pushPull.AssumedWorldPositionServer();

	/// <summary>
	/// Serverside world position.
	/// Returns InvalidPos if you're hidden (e.g. in a locker)
	/// </summary>
	public Vector3Int WorldPos => registerTile.WorldPositionServer;

	/// <summary>
	/// This player's item storage.
	/// </summary>
	public DynamicItemStorage DynamicItemStorage { get; private set; }

	private static bool verified;
	private static ulong SteamID;

	private Vector3IntEvent onTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnTileReached() => onTileReached;

	public float RTT;

	[HideInInspector]
	[SyncVar]
	public bool RcsMode;
	public MatrixMove RcsMatrixMove { get; set; }

	private bool isUpdateRTT;
	private float waitTimeForRTTUpdate = 0f;

	/// <summary>
	/// Whether a player is connected in the game object this script is on, valid serverside only
	/// </summary>
	public bool HasSoul => connectionToClient != null;

	[SerializeField]
	private PlayerStates playerState = PlayerStates.Normal;
	public PlayerStates PlayerState => playerState;

	public enum PlayerStates
	{
		Normal,
		Ghost,
		Blob,
		Ai
	}



	//The object the player will receive chat and send chat from.
	//E.g. usually same object as this script but for Ai it will be their core object
	//Serverside only
	[SerializeField]
	private GameObject playerChatLocation = null;
	public GameObject PlayerChatLocation => playerChatLocation;

	#region Lifecycle

	private void Awake()
	{
		playerSprites = GetComponent<PlayerSprites>();
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		registerTile = GetComponent<RegisterPlayer>();
		playerHealth = GetComponent<PlayerHealthV2>();
		pushPull = GetComponent<ObjectBehaviour>();
		weaponNetworkActions = GetComponent<WeaponNetworkActions>();
		mouseInputController = GetComponent<MouseInputController>();
		chatIcon = GetComponentInChildren<ChatIcon>(true);
		playerMove = GetComponent<PlayerMove>();
		playerDirectional = GetComponent<Directional>();
		DynamicItemStorage = GetComponent<DynamicItemStorage>();
		Equipment = GetComponent<Equipment>();
		Cooldowns = GetComponent<HasCooldowns>();
		PlayerOnlySyncValues = GetComponent<PlayerOnlySyncValues>();

	}

	public override void OnStartClient()
	{
		Init();
		SyncPlayerName(playerName, playerName);
	}

	// isLocalPlayer is always called after OnStartClient
	public override void OnStartLocalPlayer()
	{
		Init();
		waitTimeForRTTUpdate = 0f;

		if (IsGhost == false)
		{
			UIManager.Internals.SetupListeners();
			UIManager.Instance.panelHudBottomController.SetupListeners();
		}

		isUpdateRTT = true;
	}

	// You know the drill
	public override void OnStartServer()
	{
		Init();
	}

	private void OnEnable()
	{
		EventManager.AddHandler(Event.PlayerRejoined, Init);
		EventManager.AddHandler(Event.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.AddHandler(Event.PlayerRejoined, OnPlayerReturnedToBody);

		//Client and Local host only
		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.PlayerRejoined, Init);
		EventManager.RemoveHandler(Event.GhostSpawned, OnPlayerBecomeGhost);
		EventManager.RemoveHandler(Event.PlayerRejoined, OnPlayerReturnedToBody);

		if(CustomNetworkManager.IsHeadless) return;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}


	public void Init()
	{
		if (isLocalPlayer)
		{
			EnableLighting(true);
			UIManager.ResetAllUI();
			GetComponent<MouseInputController>().enabled = true;

			if (UIManager.Instance.statsTab.window.activeInHierarchy == false)
			{
				UIManager.Instance.statsTab.window.SetActive(true);
			}

			IPlayerControllable input = PlayerSync;

			if (TryGetComponent<AiMouseInputController>(out var aiMouseInputController))
			{
				input = aiMouseInputController;
			}

			LocalPlayerManager.SetPlayerForControl(gameObject, input);

			if (playerState == PlayerStates.Ghost)
			{
				if (PlayersManager.Instance.IsClientAdmin)
				{
					UIManager.LinkUISlots(ItemStorageLinkOrigin.adminGhost);
				}
				// stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				// show ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask |= 1 << LayerMask.NameToLayer("Ghosts");
				Camera2DFollow.followControl.cam.cullingMask = mask;

			}
			//Normal players
			else if (IsPlayerSemiGhost == false)
			{
				UIManager.LinkUISlots(ItemStorageLinkOrigin.localPlayer);
				// Hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}
			//Players like blob or Ai
			else
			{
				// stop the crit notification and change overlay to ghost mode
				SoundManager.Stop("Critstate");
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.death);
				// hide ghosts
				var mask = Camera2DFollow.followControl.cam.cullingMask;
				mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
				Camera2DFollow.followControl.cam.cullingMask = mask;
			}

			EventManager.Broadcast(Event.UpdateChatChannels);
		}
	}

	#endregion

	//Client Side Only
	private void UpdateMe()
	{
		if (isUpdateRTT && hasAuthority)
		{
			RTTUpdate();
		}
	}

	private void RTTUpdate()
	{
		waitTimeForRTTUpdate += Time.deltaTime;
		if (waitTimeForRTTUpdate > 0.5f)
		{
			waitTimeForRTTUpdate = 0f;
			RTT = (float)NetworkTime.rtt;
			if (playerHealth != null)
			{
				playerHealth.RTT = RTT;
			}
			CmdUpdateRTT(RTT);
		}
	}

	[Command]
	private void CmdUpdateRTT(float rtt)
	{
		RTT = rtt;
		if (playerHealth != null)
		{
			playerHealth.RTT = rtt;
		}
	}

	/// <summary>
	/// Sets the game object for where the player can receive and send chat message from
	/// </summary>
	/// <param name="newLocation"></param>
	[Server]
	public void SetPlayerChatLocation(GameObject newLocation)
	{
		playerChatLocation = newLocation;
	}

	/// <summary>
	/// This function enable fov and lighting
	/// </summary>
	/// <param name="enable"></param>
	private void EnableLighting(bool enable)
	{
		// Get the lighting system
		var lighting = Camera.main.GetComponent<LightingSystem>();
		if (!lighting)
		{
			Logger.LogWarning("Local Player can't find lighting system on Camera.main", Category.Lighting);
			return;
		}

		lighting.enabled = enable;
	}

	private void OnPlayerReturnedToBody()
	{
		Logger.Log("Local player become Ghost", Category.Ghosts);
		EnableLighting(true);
	}

	private void OnPlayerBecomeGhost()
	{
		Logger.Log("Local player returned to the body", Category.Ghosts);
		EnableLighting(false);
	}

	public void SyncPlayerName(string oldValue, string value)
	{
		playerName = value;
		gameObject.name = value;
	}

	public bool IsHidden => !PlayerSync.ClientState.Active;

	/// <summary>
	/// True if this player is a ghost, meaning they exist in the ghost layer
	/// </summary>
	public bool IsGhost => PlayerUtils.IsGhost(MindManager.StaticGet(this.gameObject));

	/// <summary>
	/// Same as is ghost, but also true when player inside his dead body
	/// </summary>
	public bool IsDeadOrGhost
	{
		get
		{
			var isDeadOrGhost = IsGhost;
			if (playerHealth != null)
			{
				isDeadOrGhost = playerHealth.IsDead;
			}
			return isDeadOrGhost;
		}
	}

	// If the player acts like a ghost but is still playing ingame, used for blobs and in the future maybe AI.
	public bool IsPlayerSemiGhost => playerState == PlayerStates.Blob || playerState == PlayerStates.Ai;

	public object Chat { get; internal set; }


	/// <summary>
	/// Sets the IC name for this player and refreshes the visible name. Name will be kept if respawned.
	/// </summary>
	/// <param name="newName">The new name to give to the player.</param>
	public void SetPermanentName(string newName)
	{
		// characterSettings.Name = newName;
		playerName = newName;
		// RefreshVisibleName();
	}






	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		//We need to handle lighting stuff for matrix rotations for local player:
		if (LocalPlayerManager.LocalPlayer == gameObject && rotationInfo.IsClientside)
		{
			if (rotationInfo.IsStarting)
			{
				Camera2DFollow.followControl.lightingSystem.matrixRotationMode = true;
			}
			else if (rotationInfo.IsEnding)
			{
				Camera2DFollow.followControl.lightingSystem.matrixRotationMode = false;
			}
		}
	}

	public string AdminInfoString()
	{
		var stringBuilder = new StringBuilder();

		// stringBuilder.AppendLine($"Name: {characterSettings.Name}");
		// stringBuilder.AppendLine($"Acc: {characterSettings.Username}");

		if(connectionToClient == null)
		{
			stringBuilder.AppendLine("Has No Soul");
		}

		if (playerHealth != null)
		{
			stringBuilder.AppendLine($"Is Alive: {playerHealth.IsDead == false} Health: {playerHealth.OverallHealth}");
		}

		if (mind !=null && mind.IsAntag)
		{
			stringBuilder.Insert(0, "<color=yellow>");
			stringBuilder.AppendLine($"Antag: {mind.GetAntag().Antagonist.AntagJobType}");
			stringBuilder.AppendLine($"Objectives : {mind.GetAntag().GetObjectiveSummary()}</color>");
		}

		return stringBuilder.ToString();
	}




}
