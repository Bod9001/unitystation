using System;
using System.Collections;
using System.Net.NetworkInformation;
using Systems;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Messages.Server;
using Messages.Client;
using Messages.Client.NewPlayer;
using UI;

/// <summary>
/// This is the Viewer object for a joined player.
/// Once they join they will have local ownership of this object until a job is determined
/// and then they are spawned as player entity
/// </summary>
public class JoinedViewer : NetworkBehaviour
{
	public override void OnStartLocalPlayer()
	{
		base.OnStartLocalPlayer();
		RequestObserverRefresh.Send(SceneManager.GetActiveScene().name);
		LocalPlayerManager.SetViewerForControl(this);

		CmdServerSetupPlayer(GetNetworkInfo(),
			LocalPlayerManager.CurrentCharacterSettings.Username, DatabaseAPI.ServerData.UserID, GameData.BuildNumber,
			DatabaseAPI.ServerData.IdToken);
	}

	[Command]
	private void CmdServerSetupPlayer(string unverifiedClientId, string unverifiedUsername,
		string unverifiedUserid, int unverifiedClientVersion, string unverifiedToken)
	{
		ServerSetUpPlayer(unverifiedClientId, unverifiedUsername, unverifiedUserid, unverifiedClientVersion,
			unverifiedToken);
	}

	[Server]
	private async void ServerSetUpPlayer(
		string unverifiedClientId,
		string unverifiedUsername,
		string unverifiedUserid,
		int unverifiedClientVersion,
		string unverifiedToken)
	{
		//unverifiedClientId
		//note It's impossible to verifi ClientId Since it is the mac address tho, It is only available on the same network or on the same computer
		//Looking through how hard it is to get a Mac address I think it's reasonable enough to use it,
		// since it would require a targeted attack that would require access to your network or computer (or If you've been numpty and posted your MAC address everywhere)
		// And also the consequences is that
		// A logged of players could be claimed by a different account ( Highly unlikely you get a player that you want to login as )
		// B Kick the player currently logged in, but they can log back in so It will be a constant battle between the two of you ( Can be annoying but not the end of the world )
		// from this I think it's a reasonable Step to take to prevent
		// People from multi-accounting with a non-modified client


		//unverifiedUsername
		//Currently not verified but TODO planned Django Upgrade validate username
		Logger.LogFormat(
			"A joinedviewer called CmdServerSetupPlayer on this server, Unverified ClientId: {0} Unverified Username: {1}",
			Category.Connections,
			unverifiedClientId, unverifiedUsername);


		// this validates Userid and Token
		var isValidPlayer = await PlayersManager.Instance.ValidatePlayer(unverifiedClientId, unverifiedUsername,
			unverifiedUserid, unverifiedClientVersion, unverifiedToken, this.connectionToClient);


		if (isValidPlayer == false)
		{
			Logger.LogWarning($"Set up new player: invalid player. For {unverifiedUsername}", Category.Connections);
			return;
		}

		//TODO unverifiedUsername Never validated
		var Userid = unverifiedUserid; //Verified in ValidatePlayer
		var Token = unverifiedToken;


		//Send to client their job ban entries
		var jobBanEntries = PlayersManager.Instance.ClientAskingAboutJobBans(Userid, connectionToClient.address,
			unverifiedClientId, unverifiedUsername);
		PlayersManager.ServerSendsJobBanDataMessage.Send(connectionToClient, jobBanEntries);

		//Send to client the current crew job counts
		if (CrewManifestManager.Instance != null)
		{
			SetJobCountsMessage.SendToPlayer(CrewManifestManager.Instance.Jobs, connectionToClient);
		}

		UpdateConnectedPlayersMessage.Send();


		// Only sync the pre-round countdown if it's already started.
		if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
		{
			if (GameManager.Instance.waitForStart)
			{
				TargetSyncCountdown(connectionToClient, GameManager.Instance.waitForStart,
					GameManager.Instance.CountdownEndTime);
			}
			else
			{
				GameManager.Instance.CheckPlayerCount();
			}
		}

		// Check if they have a player to rejoin before creating a new ConnectedPlayer
		var RelatedConnectedPlayer = PlayersManager.Instance.RemovePlayerbyClientId(unverifiedClientId, unverifiedUserid);
		if (RelatedConnectedPlayer == null)
		{
			// Register player to player list (logging code exists in PlayerList so no need for extra logging here)
			RelatedConnectedPlayer = PlayersManager.Instance.GetNew(connectionToClient, this,unverifiedUsername, unverifiedClientId,Userid);
			PlayersManager.Instance.Add(RelatedConnectedPlayer);
			TargetLocalPlayerSetupNewPlayer(connectionToClient, GameManager.Instance.CurrentRoundState);
		}
		else
		{
			PlayersManager.Instance.Add(RelatedConnectedPlayer);
			StartCoroutine(WaitForLoggedOffObserver(RelatedConnectedPlayer.CurrentMind));
		}


		PlayersManager.Instance.CheckAdminState(RelatedConnectedPlayer, Userid);
		PlayersManager.Instance.CheckMentorState(RelatedConnectedPlayer, Userid);
	}

	/// <summary>
	/// Waits for the client to be an observer of the player before continuing
	/// </summary>
	private IEnumerator WaitForLoggedOffObserver(Mind loggedOffPlayer)
	{
		TargetLocalPlayerRejoinUI(connectionToClient);
		// TODO: When we have scene network culling we will need to allow observers
		// for the whole specific scene and the body before doing the logic below:
		var netIdentity = loggedOffPlayer.PhysicalBrain.GetComponent<NetworkIdentity>();
		if (netIdentity == null)
		{
			Logger.LogError($"No {nameof(NetworkIdentity)} component on {loggedOffPlayer}! " +
			                "Cannot rejoin that player. Was original player object improperly created? " +
			                "Did we get runtime error while creating it?", Category.Connections);
			// TODO: if this issue persists, should probably send the poor player a message about failing to rejoin.
			yield break;
		}

		while (!netIdentity.observers.ContainsKey(this.connectionToClient.connectionId))
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
		TargetLocalPlayerRejoinUI(connectionToClient);
		PlayerSpawn.ServerRejoinPlayer(loggedOffPlayer);
	}

	[TargetRpc]
	private void TargetLocalPlayerRejoinUI(NetworkConnection target)
	{
		UIManager.Display.preRoundWindow.ShowRejoiningPanel();
	}

	/// <summary>
	/// Target which tells this joined viewer they are a new player, tells them what their ID is,
	/// and tells them what round state the game is on
	/// </summary>
	/// <param name="target">this connection</param>
	[TargetRpc]
	private void TargetLocalPlayerSetupNewPlayer(NetworkConnection target, RoundState roundState)
	{
		// clear our UI because we're about to change it based on the round state
		UIManager.ResetAllUI();

		// Determine what to do depending on the state of the round
		switch (roundState)
		{
			case RoundState.PreRound:
				// Round hasn't yet started, give players the pre-game screen
				UIManager.Display.SetScreenForPreRound();
				break;
			default:
				// Show the joining screen
				UIManager.Display.SetScreenForJoining();
				break;
		}
	}

	public void RequestJob(JobType job)
	{
		var jsonCharSettings = JsonConvert.SerializeObject(LocalPlayerManager.CurrentCharacterSettings);

		if (PlayersManager.Instance.ClientJobBanCheck(job) == false)
		{
			Logger.LogWarning($"Client failed local job-ban check for {job}.", Category.Jobs);
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().ShowFailMessage(JobRequestError.JobBanned);
			return;
		}

		ClientRequestJobMessage.Send(job, jsonCharSettings, DatabaseAPI.ServerData.UserID);
	}

	public void Spectate()
	{
		var jsonCharSettings = JsonConvert.SerializeObject(LocalPlayerManager.CurrentCharacterSettings);
		CmdSpectate(jsonCharSettings);
	}

	/// <summary>
	/// Command to spectate a round instead of spawning as a player
	/// </summary>
	[Command]
	public void CmdSpectate(string jsonCharSettings)
	{
		var characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(jsonCharSettings);
		PlayerSpawn.ServerSpawnGhostSpectating(this, characterSettings);
	}

	/// <summary>
	/// Tells the client to start the countdown if it's already started
	/// </summary>
	[TargetRpc]
	private void TargetSyncCountdown(NetworkConnection target, bool started, double endTime)
	{
		Logger.Log("Syncing countdown!", Category.Round);
		UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().SyncCountdown(started, endTime);
	}

	private string GetNetworkInfo()
	{
		var nics = NetworkInterface.GetAllNetworkInterfaces();
		foreach (var n in nics)
		{
			if (string.IsNullOrEmpty(n.GetPhysicalAddress().ToString()) == false)
			{
				n.GetPhysicalAddress().ToString();
			}
		}

		return "";
	}

	/// <summary>
	/// Mark this joined viewer as ready for job allocation
	/// </summary>
	public void SetReady(bool isReady)
	{
		var jsonCharSettings = "";
		if (isReady)
		{
			jsonCharSettings = JsonConvert.SerializeObject(LocalPlayerManager.CurrentCharacterSettings);
		}

		CmdPlayerReady(isReady, jsonCharSettings);
	}

	[Command]
	private void CmdPlayerReady(bool isReady, string jsonCharSettings)
	{
		var player = PlayersManager.Instance.GetByConnection(connectionToClient);

		CharacterSettings charSettings = null;
		if (isReady)
		{
			charSettings = JsonConvert.DeserializeObject<CharacterSettings>(jsonCharSettings);
		}

		PlayersManager.Instance.SetPlayerReady(player, isReady, charSettings);
	}
}