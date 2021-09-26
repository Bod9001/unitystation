using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems;
using Systems.Spawns;
using Items;
using Managers;
using Messages.Server;
using Messages.Server.LocalGuiMessages;

/// <summary>
/// Main API for dealing with spawning players and related things.
/// For spawning of non-player things, see Spawn.
/// </summary>
public static class PlayerSpawn
{
	public class SpawnEventArgs : EventArgs { public Mind player; }
	public delegate void SpawnHandler(object sender, SpawnEventArgs args);
	public static event SpawnHandler SpawnEvent;

	/// <summary>
	/// Server-side only. For use when a player has only joined (as a JoinedViewer) and
	/// is not in control of any mobs. Spawns the joined viewer as the indicated occupation and transfers control to it.
	/// Note that this doesn't take into account game mode or antags, it just spawns whatever is requested.
	/// </summary>
	/// <param name="request">holds the request data</param>
	/// <param name="joinedViewer">viewer who should control the player</param>
	/// <param name="occupation">occupation to spawn as</param>
	/// <param name="characterSettings">settings to use for the character</param>
	/// <returns>the game object of the spawned player</returns>
	public static GameObject ServerSpawnNewPlayer(PlayerSpawnRequest request, JoinedViewer joinedViewer, Occupation occupation, CharacterSettings characterSettings, bool showBanner = true)
	{
		if(ValidateCharacter(request) == false)
		{
			return null;
		}

		var  player = PlayersManager.Instance.GetByConnection(joinedViewer.connectionToClient);
		if (player == null) return null;

		var Mind = MindManager.Instance.GetNewMindAndSetOccupation(occupation, characterSettings);
		player.CurrentMind = Mind;
		// TODO: add a nice cutscene/animation for the respawn transition
		var newPlayer = ServerSpawnInternal(Mind, occupation, characterSettings, null, showBanner: showBanner);
		if (newPlayer != null && occupation.IsCrewmember)
		{
			CrewManifestManager.Instance.AddMember(Mind, occupation.JobType);
		}

		if (SpawnEvent != null)
		{
			SpawnEventArgs args = new SpawnEventArgs() { player = Mind };
			SpawnEvent.Invoke(null, args);
		}

		return newPlayer;
	}

	private static bool ValidateCharacter(PlayerSpawnRequest request)
	{
		var isOk = true;
		var message = "";

		//Disable this until we fix skin tone checks.
		/*
		if(ServerValidations.HasIllegalSkinTone(request.CharacterSettings))
		{
			message += " Invalid player skin tone.";
			isOk = false;
		}


		if(ServerValidations.HasIllegalCharacterName(request.CharacterSettings.Name))
		{
			message += " Invalid player character name.";
			isOk = false;
		}
		*/
		if(ServerValidations.HasIllegalCharacterAge(request.CharacterSettings.Age))
		{
			message += " Invalid character age.";
			isOk = false;
		}

		if (isOk == false)
		{
			message += " Please change and resave character.";
			ValidateFail(request.JoinedViewer, request.UserID, message);
		}

		return isOk;
	}

	private static void ValidateFail(JoinedViewer joinedViewer, string userId, string message)
	{
		PlayersManager.Instance.ServerKickPlayer(userId, message, false, 1, false);
		if(joinedViewer.isServer || joinedViewer.isLocalPlayer)
		{
			joinedViewer.Spectate();
		}
	}

	/// <summary>
	/// Server-side only. For use when a player has only joined (as a JoinedViewer) and
	/// is not in control of any mobs. Spawns the joined viewer as the indicated occupation and transfers control to it.
	/// Note that this doesn't take into account game mode or antags, it just spawns whatever is requested.
	/// </summary>
	/// <param name="spawnRequest">details of the requested spawn</param>
	/// <returns>the game object of the spawned player</returns>
	public static GameObject ServerSpawnPlayer(PlayerSpawnRequest spawnRequest)
	{
		return ServerSpawnNewPlayer(spawnRequest, spawnRequest.JoinedViewer, spawnRequest.RequestedOccupation,
			spawnRequest.CharacterSettings);
	}

	/// <summary>
	/// For use when player is connected and dead.
	/// Respawns the mind's character and transfers their control to it.
	/// </summary>
	/// <param name="forMind"></param>
	public static void ServerRespawnPlayer(Mind forMind)
	{
		//get the settings from the mind
		var occupation = forMind.occupation;
		var settings = forMind.OriginalCharacter;

		ServerSpawnInternal(forMind, occupation, settings);
	}

	/// <summary>
	/// For use when a player is alive or dead and you want to clone their body and transfer their control to it.
	///
	/// Clones a given mind's current body to a new body and transfers control of the mind's connection to that new body.
	/// </summary>
	/// <param name="forMind"></param>
	/// <param name="worldPosition"></param>
	public static GameObject ServerClonePlayer(Mind forMind, Vector3Int worldPosition) //TODO Cloning record should contain data about What the body should be
	{
		var occupation = forMind.occupation;
		var settings = forMind.OriginalCharacter;
		return ServerSpawnInternal(forMind, occupation, settings, worldPosition);
	}

	//Time to start spawning players at arrivals
	private static readonly DateTime ARRIVALS_SPAWN_TIME = new DateTime().AddHours(12).AddMinutes(2);

	/// <summary>
	/// Spawns a new player character and transfers the connection's control into the new body.
	/// If existingMind is null, creates the new mind and assigns it to the new body.
	///
	/// Fires server and client side player spawn hooks.
	/// </summary>
	/// <param name="connection">connection to give control to the new player character</param>
	/// <param name="occupation">occupation of the new player character</param>
	/// <param name="characterSettings">settings of the new player character</param>
	/// <param name="existingMind">existing mind to transfer to the new player, if null new mind will be created
	/// and assigned to the new player character</param>
	/// <param name="spawnPos">world position to spawn at</param>
	/// <param name="spawnItems">If spawning a player, should the player spawn without the defined initial equipment for their occupation?</param>
	/// <param name="willDestroyOldBody">if true, indicates the old body is going to be destroyed rather than pooled,
	/// thus we shouldn't send any network message which reference's the old body's ID since it won't exist.</param>
	///
	/// <returns>the spawned object</returns>
	private static GameObject ServerSpawnInternal(Mind mind, Occupation occupation, CharacterSettings characterSettings, Vector3Int? spawnPos = null, bool spawnItems = true, bool showBanner = true)
	{
		//determine where to spawn them
		if (spawnPos == null)
		{
			Transform spawnTransform;
			//Spawn normal location for special jobs or if less than 2 minutes passed
			if (GameManager.Instance.stationTime < ARRIVALS_SPAWN_TIME || occupation.LateSpawnIsArrivals == false)
			{
				spawnTransform = SpawnPoint.GetRandomPointForJob(occupation.JobType);
			}
			else
			{
				spawnTransform = SpawnPoint.GetRandomPointForLateSpawn();
				//Fallback to assistant spawn location if none found for late join
				if (spawnTransform == null && occupation.JobType != JobType.NULL)
				{
					spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
				}
			}

			if (spawnTransform == null)
			{
				Logger.LogErrorFormat(
					"Unable to determine spawn position for connection {0} occupation {1}. Cannot spawn player.",
					Category.EntitySpawn,
					mind.AssignedPlayer.Connection.address, occupation.DisplayName);
				return null;
			}

			spawnPos = spawnTransform.transform.position.CutToInt();
		}

		//create the player object
		var newPlayerBrain = ServerCreatePlayerBrain(spawnPos.GetValueOrDefault(), occupation.SpecialPlayerPrefab);

		var newPlayerGhost = ServerCreatePlayerGhost(spawnPos.GetValueOrDefault());

		//transfer control to the player object
		ServerTransferPlayerBody(mind, newPlayerBrain, Event.PlayerSpawned);

		//transfer the mind to the new body
		mind.PossessNewBody(newPlayerBrain);

		mind.SetGhost(newPlayerGhost);

		UpdateConnectedPlayersMessage.Send();

		GameObject ActualPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;
		if (occupation.SpecialPlayerPrefab != null)
		{
			ActualPrefab = occupation.SpecialPlayerPrefab;
		}

		//fire all hooks
		var info = SpawnInfo.Player(occupation, characterSettings,  ActualPrefab,
			SpawnDestination.At(spawnPos), spawnItems: spawnItems);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, newPlayerBrain));

		if (occupation != null && showBanner)
		{
			SpawnBannerMessage.Send(
				mind,
				occupation.DisplayName,
				occupation.SpawnSound.AssetAddress,
				occupation.TextColor,
				occupation.BackgroundColor,
				occupation.PlaySound);
		}
		if (info.SpawnItems)
		{
			newPlayerBrain.GetComponent<DynamicItemStorage>()?.SetUpOccupation(occupation,mind );
		}


		return newPlayerBrain;
	}

	/// <summary>
	/// Use this when a player is currently a ghost and wants to reenter their body.
	/// </summary>
	/// <param name="forConnection">connection to transfer control to</param>
	/// TODO: Remove need for this parameter
	/// <param name="forConnection">object forConnection is currently in control of</param>
	/// <param name="forMind">mind to transfer control back into their body</param>
	public static void ServerGhostReenterBody(NetworkConnection forConnection, GameObject fromObject, Mind forMind)
	{
		//TODO Handle this in mind!
		Logger.LogError("Implement");
		//ServerTransferPlayerBody(forConnection, body, fromObject, Event.PlayerSpawned, settings);
	}

	/// <summary>
	/// Use this when a player rejoins the game and already has a logged-out body in the game.
	/// Transfers their control back to the body.
	/// </summary>
	/// <param name="Mind"></param>
	public static void ServerRejoinPlayer(Mind Mind)
	{
		ServerTransferPlayerBody(Mind, Mind.GameObjectBody, Event.PlayerRejoined);
		Mind.ResendSpellActions();
	}


	/// <summary>
	/// Generates new mind and ghost, meant for people who want to spectate
	/// </summary>
	public static void ServerSpawnGhostSpectating(JoinedViewer joinedViewer, CharacterSettings characterSettings)
	{
		//Hard coding to assistant
		Vector3Int spawnPosition = SpawnPoint.GetRandomSpawnPoint(true).transform.position.CutToInt();

		//Get spawn location
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;
		var newPlayer = UnityEngine.Object.Instantiate(CustomNetworkManager.Instance.ghostPrefab, spawnPosition, parentTransform.rotation, parentTransform);

		var newMind = MindManager.Instance.GetNewMindForGhost(characterSettings);
		ServerTransferPlayerBody(newMind, newPlayer, Event.GhostSpawned);

		newMind.SetGhost(newPlayer);
		if (PlayersManager.Instance.IsAdmin(PlayersManager.Instance.Get(joinedViewer.connectionToClient)))
		{
			newPlayer.GetComponent<GhostSprites>().SetAdminGhost();
		}
	}

	private static GameObject ServerCreatePlayerGhost(Vector3Int spawnWorldPosition, GameObject playerPrefab = null)
	{
		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = spawnWorldPosition;
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;

		if (playerPrefab == null)
		{
			playerPrefab = CustomNetworkManager.Instance.ghostPrefab;
		}

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var player = UnityEngine.Object.Instantiate(playerPrefab, spawnPosition, parentTransform.rotation,
			parentTransform);

		return player;
	}


	/// <summary>
	/// Server-side only. Creates the player object on the server side and fires server-side
	/// spawn hooks. Doesn't transfer control to the client yet. Client side hooks should be fired after client has been
	/// informed of the spawn
	/// </summary>
	/// <param name="spawnWorldPosition">world pos to spawn at</param>
	/// <param name="playerPrefab">prefab to spawn for the player</param>
	/// <returns></returns>
	private static GameObject ServerCreatePlayerBrain(Vector3Int spawnWorldPosition, GameObject playerPrefab = null)
	{
		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = spawnWorldPosition;
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;

		if (playerPrefab == null)
		{
			playerPrefab = CustomNetworkManager.Instance.brainPrefab;
		}

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var player = UnityEngine.Object.Instantiate(playerPrefab, spawnPosition, parentTransform.rotation,
			parentTransform);

		return player;
	}


	public static void ServerTransferPlayerToNewBody(Mind Mind, GameObject newBody, Event eventType)
	{
		ServerTransferPlayerBody(Mind, newBody, eventType);
	}

	/// <summary>
	/// Server-side only. Transfers control of a player object to the indicated connection.
	/// </summary>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	/// thus we shouldn't send any network message which reference's the old body's ID since it won't exist.</param>
	private static void ServerTransferPlayerBody(Mind Mind, GameObject newBody, Event eventType)
	{
		var connectedPlayer = PlayersManager.Instance.Get(Mind);
		if (connectedPlayer == PlayersManager.InvalidPlayer) //this isn't an online player
		{
			NetworkServer.Spawn(newBody);
		}
		else
		{
			NetworkServer.ReplacePlayerForConnection(Mind.AssignedPlayer.Connection, newBody);
			//ReplacePlayerForConnection
			TriggerEventMessage.SendTo(Mind, eventType);
		}

		var ObjectBehavior = newBody.GetComponent<ObjectBehaviour>();
		if (ObjectBehavior  && ObjectBehavior.parentContainer)
		{
			FollowCameraMessage.Send(Mind, ObjectBehavior.parentContainer.gameObject);
		}

		var Pickupable = newBody.GetComponent<Pickupable>();
		if (Pickupable && Pickupable.ItemSlot != null)
		{
			var Player = Pickupable.ItemSlot.GetRootPlayer();
			if (Player == null)
			{
				FollowCameraMessage.Send(Mind,Player.GameObjectBody); //TODO Update if following And dropped from inventory
			}
			else
			{
				FollowCameraMessage.Send(Mind,Pickupable.ItemSlot.GetRootStorage()); //TODO Update if following And dropped from inventory
			}

		}

	}
}
