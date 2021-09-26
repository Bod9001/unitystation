using System;
using System.Collections.Generic;
using System.Linq;
using Antagonists;
using Messages.Server;
using UnityEngine;
using Mirror;
using UI.CharacterCreator;

/// Comfy place to get players and their info (preferably via their connection)
/// Has limited scope for clients (ClientConnectedPlayers only), sweet things are mostly for server
public partial class PlayersManager : NetworkBehaviour
{
	//ConnectedPlayer list, server only
	private List<ConnectedPlayer> loggedIn = new List<ConnectedPlayer>();
	public List<ConnectedPlayer> loggedOff = new List<ConnectedPlayer>();

	//For client needs: updated via UpdateConnectedPlayersMessage, useless for server
	public List<ClientConnectedPlayer> ClientConnectedPlayers = new List<ClientConnectedPlayer>();

	public static PlayersManager Instance;

	public ConnectedPlayer invalidPlayer;

	public static ConnectedPlayer InvalidPlayer => Instance.invalidPlayer;

	public int ConnectionCount => loggedIn.Count;
	public int OfflineConnCount => loggedOff.Count;
	public List<ConnectedPlayer> InGamePlayers => loggedIn.FindAll(player => player.Connection != null);

	public List<ConnectedPlayer> AllPlayers =>
		loggedIn.FindAll(player => (player.CurrentMind != null || player.ViewerScript != null));

	/// <summary>
	/// Players in the pre-round lobby who have clicked the ready button and have up to date CharacterSettings
	/// </summary>
	public List<ConnectedPlayer> ReadyPlayers { get; } = new List<ConnectedPlayer>();

	/// <summary>
	/// Records the last round player count
	/// </summary>
	public static int LastRoundPlayerCount = 0;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void OnEnable()
	{
		EventManager.AddHandler(Event.RoundEnded, SetEndOfRoundPlayerCount);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundEnded, SetEndOfRoundPlayerCount);
	}

	private void SetEndOfRoundPlayerCount()
	{
		LastRoundPlayerCount = Instance.ConnectionCount;
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InitAdminController();
	}


	/// Don't do this unless you realize the consequences
	[Server]
	public void Clear()
	{
		loggedIn.Clear();
	}

	public ConnectedPlayer ConnectedPlayerPrefab;

	public ConnectedPlayer GetNew(NetworkConnection connectionToClient, JoinedViewer joinedViewer,  string unverifiedUsername, string ClientId, string UserId)
	{
		var NewPlayer = Spawn.ServerPrefab(ConnectedPlayerPrefab.gameObject, Vector3.zero, this.gameObject.transform);
		var NewConnectedPlayer = NewPlayer.GameObject.GetComponent<ConnectedPlayer>();
		NewConnectedPlayer.Connection = connectionToClient;
		NewConnectedPlayer.ViewerScript = joinedViewer;
		NewConnectedPlayer.ClientId = ClientId;
		NewConnectedPlayer.Username = unverifiedUsername;
		NewConnectedPlayer.UserId = UserId;
		return NewConnectedPlayer;
	}


	/// <summary>
	/// Adds this connected player to the list, or updates an existing entry if there's already one for
	/// this player's networkconnection. Returns the ConnectedPlayer that was added or updated.
	/// </summary>
	/// <param name="player"></param>
	[Server]
	public void Add(ConnectedPlayer player)
	{
		if (player.Equals(PlayersManager.InvalidPlayer))
		{
			Logger.Log("Refused to add invalid connected player to this server's player list", Category.Connections);
			return;
		}

		Logger.Log($"Player {player.Username}'s client ID is: {player.ClientId} User ID: {player.UserId}.", Category.Connections);

		loggedIn.Add(player);
		Logger.LogFormat("Added to this server's PlayerList {0}. Total:{1}; {2}", Category.Connections, player,
			loggedIn.Count, string.Join(";", loggedIn));
		CheckRcon();
		return;
	}

	[Server]
	private void TryMoveClientToOfflineList(ConnectedPlayer player)
	{
		if (!loggedIn.Contains(player))
		{
			Logger.Log($"Player with name {player.Username} was not found in online player list. " +
					$"Verifying player lists for integrity...", Category.Connections);
			ValidatePlayerListRecords();
			return;
		}

		Logger.Log($"Added {player.Username} to offline player list.", Category.Connections);
		loggedOff.Add(player);
		loggedIn.Remove(player);
		UpdateConnectedPlayersMessage.Send();
		CheckRcon();
	}

	[Server]
	public bool ContainsConnection(NetworkConnection connection)
	{
		return !Get(connection).Equals(PlayersManager.InvalidPlayer);
	}

	[Server]
	public ConnectedPlayer GetLoggedOffClient(string clientID, string userId)
	{
		var index = loggedOff.FindIndex(x => x.ClientId == clientID || x.UserId == userId);
		if (index != -1)
		{
			return loggedOff[index];
		}

		return null;
	}

	[Server]
	public ConnectedPlayer Get(NetworkConnection byConnection)
	{
		return GetInternalLoggedIn(player => player.Connection == byConnection);
	}

	[Server]
	public ConnectedPlayer Get(Mind byMind)
	{
		return GetInternalLoggedIn(player => player.CurrentMind == byMind);
	}


	[Server]
	public ConnectedPlayer Get(GameObject byGameObject, bool includeOffline = false)
	{
		if (includeOffline)
		{
			return GetInternalAll(player => player.MindHasThisBody(byGameObject));
		}

		return GetInternalLoggedIn(player => player.MindHasThisBody(byGameObject));
	}

	[Server]
	public ConnectedPlayer GetByUserID(string byUserID)
	{
		return GetInternalLoggedIn(player => player.UserId == byUserID);
	}

	[Server]
	public ConnectedPlayer GetByConnection(NetworkConnection connection)
	{
		return GetInternalLoggedIn(player => player.Connection == connection);
	}

	[Server]
	public List<ConnectedPlayer> GetAllByUserID(string byUserID, bool includeOffline = false)
	{
		var newone = loggedIn.ToList();
		if (includeOffline)
		{
			newone.AddRange(loggedOff);
		}

 		return newone.FindAll(player => player.UserId == byUserID);
	}

	/// <summary>
	/// Get all in game players, logged in and logged off
	/// </summary>
	[Server]
	public List<ConnectedPlayer> GetAllPlayersIncludingOffLine() //TODO Use mind manager and then loop through those to find the AI via antagonist settings
	{
		var players = InGamePlayers;
		players.AddRange(loggedOff.FindAll(player => player.CurrentMind != null));

		return players.ToList();
	}

	/// <summary>
	/// Check logged in and logged off players
	/// </summary>
	/// <param name="condition"></param>
	/// <returns></returns>
	private ConnectedPlayer GetInternalAll(Func<ConnectedPlayer, bool> condition)
	{
		var connectedPlayer = GetInternalLoggedIn(condition);

		if(connectedPlayer.Equals(PlayersManager.InvalidPlayer))
		{
			connectedPlayer = GetInternalLoggedOff(condition);
		}

		return connectedPlayer;
	}

	/// <summary>
	/// Check logged in players
	/// </summary>
	/// <param name="condition"></param>
	/// <returns></returns>
	private ConnectedPlayer GetInternalLoggedIn(Func<ConnectedPlayer, bool> condition)
	{
		for (var i = 0; i < loggedIn.Count; i++)
		{
			if (condition(loggedIn[i]))
			{
				return loggedIn[i];
			}
		}

		return PlayersManager.InvalidPlayer;
	}

	/// <summary>
	/// Check logged off players
	/// </summary>
	/// <param name="condition"></param>
	/// <returns></returns>
	private ConnectedPlayer GetInternalLoggedOff(Func<ConnectedPlayer, bool> condition)
	{
		for (var i = 0; i < loggedOff.Count; i++)
		{
			if (condition(loggedOff[i]))
			{
				return loggedOff[i];
			}
		}

		return PlayersManager.InvalidPlayer;
	}

	[Server]
	public void RemoveByConnection(NetworkConnection connection)
	{
		if (connection?.address == null || connection.identity == null)
		{
			Logger.Log($"Unknown player disconnected: verifying playerlists for integrity - connection, its address and identity was null.", Category.Connections);
			ValidatePlayerListRecords();
			return;
		}

		var player = Get(connection);
		if (player.Equals(PlayersManager.InvalidPlayer))
		{
			Logger.Log($"Unknown player disconnected: verifying playerlists for integrity - connected player was invalid. " +
					$"IP: {connection.address}. Name: {connection.identity.name}.", Category.Connections);
			ValidatePlayerListRecords();
			return;
		}

		SetPlayerReady(player, false);
		CheckForLoggedOffAdmin(player.UserId, player.Username);
		TryMoveClientToOfflineList(player);
	}

	/// <summary>
	/// Verify the data of the player lists
	/// This is good to do if something unexpected has happened
	/// </summary>
	void ValidatePlayerListRecords()
	{
		//verify loggedIn clients:
		for (int i = loggedIn.Count - 1; i >= 0; i--)
		{
			if (loggedIn[i].Connection == null || loggedIn[i].Equals(PlayersManager.InvalidPlayer))
			{
				TryMoveClientToOfflineList(loggedIn[i]);
			}
		}

		//verify loggedOff clients:
		for (int i = loggedOff.Count - 1; i >= 0; i--)
		{
			if (loggedOff[i].Equals(PlayersManager.InvalidPlayer))
			{
				loggedOff.RemoveAt(i);
				continue;
			}

			if (loggedOff[i].ViewerScript.gameObject == null)
			{
				loggedOff.RemoveAt(i);
				continue;
			}
		}
	}

	[Server]
	private void CheckRcon()
	{
		if (RconManager.Instance != null)
		{
			RconManager.UpdatePlayerListRcon();
		}
	}


	[Server]
	public ConnectedPlayer RemovePlayerbyClientId(string unverifiedClientId, string userId)
	{
		//note It's impossible to verifi ClientId Since it is the mac address tho, It is only available on the same network or on the same computer
		//Looking through how hard it is to get a Mac address I think it's reasonable enough to use it,
		// since it would require a targeted attack that would require access to your network or computer (or If you've been numpty and posted your MAC address everywhere)
		// And also the consequences is that
		// A logged of players could be claimed by a different account ( Highly unlikely you get a player that you want to login as )
		// B Kick the player currently logged in, but they can log back in so It will be a constant battle between the two of you ( Can be annoying but not the end of the world )
		// from this I think it's a reasonable Step to take to prevent
		// People from multi-accounting with a non-modified client

		Logger.LogTraceFormat("Searching for players with userId: {0} clientId: {1}", Category.Connections, userId, unverifiedClientId);
		foreach (var player in loggedOff)
		{
			if ((player.ClientId == unverifiedClientId || player.UserId == userId))
			{
				Logger.LogTraceFormat("Found player with userId {0} clientId: {1}", Category.Connections, player.UserId, player.ClientId);
				loggedOff.Remove(player);
				return player;
			}
		}
		foreach (var player in loggedIn)
		{
			if (LocalPlayerManager.LocalViewerScript && LocalPlayerManager.LocalViewerScript == player.ViewerScript ||
			    LocalPlayerManager.LocalPlayer == player)
			{
				continue; //server player
			}

			if ((player.ClientId == unverifiedClientId || player.UserId == userId))
			{
				Logger.LogTraceFormat("Found player with userId {0} clientId: {1}", Category.Connections, player.UserId, player.ClientId);
				player.Connection.Disconnect(); //new client while online or dc timer not triggering yet
				loggedIn.Remove(player);
				return player;
			}
		}

		return null;
	}


	private void OnDestroy()
	{
		if (adminListWatcher != null)
		{
			adminListWatcher.Changed -= LoadCurrentAdmins;
			adminListWatcher.Dispose();
		}
	}

	/// <summary>
	/// Makes a player ready/unready for job allocations
	/// </summary>
	public void SetPlayerReady(ConnectedPlayer player, bool isReady, CharacterSettings charSettings = null)
	{
		if (isReady)
		{
			// Update connection with locked in job prefs
			if (charSettings != null)
			{
				player.PreRoundCharacterSettings = charSettings;
			}
			else
			{
				Logger.LogError($"{player.Username} was set to ready with NULL character settings:\n{player}", Category.Round);
			}
			ReadyPlayers.Add(player);
			Logger.Log($"Set {player.Username} to ready with these character settings:\n{charSettings}", Category.Round);
		}
		else
		{
			ReadyPlayers.Remove(player);
			Logger.Log($"Set {player.Username} to NOT ready!", Category.Round);
		}
	}

	/// <summary>
	/// Clears the list of ready players
	/// </summary>
	[Server]
	public void ClearReadyPlayers()
	{
		ReadyPlayers.Clear();
	}

	public static bool HasAntagEnabled(AntagPrefsDict antagPrefs, Antagonist antag)
	{
		return !antag.ShowInPreferences ||
		       (antagPrefs.ContainsKey(antag.AntagName) && antagPrefs[antag.AntagName]);
	}

	public static bool HasAntagEnabled(ConnectedPlayer connectedPlayer, Antagonist antag)
	{
		return !antag.ShowInPreferences ||
		       (connectedPlayer.CurrentMind.OriginalCharacter.AntagPreferences.ContainsKey(antag.AntagName)
		        && connectedPlayer.CurrentMind.OriginalCharacter.AntagPreferences[antag.AntagName]);
	}
}

[Serializable]/// Minimalistic connected player information that all clients can posess
public struct ClientConnectedPlayer
{
	public string UserName;
	public string Tag;

	//Used to make this ClientConnectedPlayer unique even if UserName and Tags are the same
	public int Index;
}
