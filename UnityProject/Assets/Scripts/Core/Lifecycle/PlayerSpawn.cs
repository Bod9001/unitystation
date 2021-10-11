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
	public class SpawnEventArgs : EventArgs
	{
		public Mind player;
	}

	public delegate void SpawnHandler(object sender, SpawnEventArgs args);

	public static event SpawnHandler SpawnEvent;

	/// <summary>
	/// Note that this doesn't take into account game mode or antags, it just spawns whatever is requested. TODO Handle spawning NPCs through this
	/// </summary>
	/// <param name="request">holds the request data</param>
	public static Mind ServerSpawnNewPlayer(PlayerSpawnRequest request)
	{
		if (request.ConnectedPlayer == null) return null;

		if (request.ExistingMind == null)
		{
			request.ExistingMind =
				MindManager.Instance.GetNewMindAndSetOccupation(request.RequestedOccupation, request.CharacterSettings);
			request.ExistingMind.AssignedPlayer = request.ConnectedPlayer;
			request.ConnectedPlayer.CurrentMind = request.ExistingMind;
		}

		ServerForceAssignPlayerAuthority(request.ConnectedPlayer.Connection, request.ExistingMind.gameObject);

		request.ConnectedPlayer.TargetUpdateMindClient(request.ConnectedPlayer.Connection, request.ExistingMind);


		//determine where to spawn them
		if (request.spawnPos == null)
		{
			Transform spawnTransform;
			//Spawn normal location for special jobs or if less than 2 minutes passed
			if (GameManager.Instance.stationTime < ARRIVALS_SPAWN_TIME ||
			    request.RequestedOccupation.LateSpawnIsArrivals == false)
			{
				spawnTransform = SpawnPoint.GetRandomPointForJob(request.RequestedOccupation.JobType);
			}
			else
			{
				spawnTransform = SpawnPoint.GetRandomPointForLateSpawn();
				//Fallback to assistant spawn location if none found for late join
				if (spawnTransform == null && request.RequestedOccupation.JobType != JobType.NULL)
				{
					spawnTransform = SpawnPoint.GetRandomPointForJob(JobType.ASSISTANT);
				}
			}

			if (spawnTransform == null)
			{
				Logger.LogErrorFormat(
					"Unable to determine spawn position for connection {0} occupation {1}. Cannot spawn player.",
					Category.EntitySpawn,
					request.ExistingMind.AssignedPlayer.Connection.address, request.RequestedOccupation.DisplayName);
				return null;
			}

			request.spawnPos = spawnTransform.transform.position.CutToInt();
		}

		TriggerEventMessage.SendTo(request.ExistingMind, Event.PlayerSpawned);

		if (request.Brain)
		{
			if (request.PrefabOverrideBrain == null)
			{
				request.PrefabOverrideBrain = CustomNetworkManager.Instance.brainPrefab;
			}

			//create the player object
			var newPlayerBrain = ServerSpawnSubject(request.spawnPos.GetValueOrDefault(),
				request.PrefabOverrideBrain);

			//transfer the mind to the new body
			request.ExistingMind.PossessNewObject(newPlayerBrain);
		}


		if (request.Ghost)
		{
			if (request.PrefabOverrideGhost == null)
			{
				request.PrefabOverrideGhost = CustomNetworkManager.Instance.ghostPrefab;
			}

			var newPlayerGhost = ServerSpawnSubject(request.spawnPos.GetValueOrDefault(),
				request.PrefabOverrideGhost);

			request.ExistingMind.SetGhost(newPlayerGhost);

			newPlayerGhost.name = request.ExistingMind.CharactersName;

			if (PlayersManager.Instance.IsAdmin(PlayersManager.Instance.Get(request.JoinedViewer.connectionToClient)))
			{
				newPlayerGhost.GetComponent<GhostSprites>().SetAdminGhost();
			}
		}


		if (request.Body)
		{
			if (request.PrefabOverrideBody == null)
			{
				request.PrefabOverrideBody = CustomNetworkManager.Instance.humanPlayerPrefab;
				if (request.RequestedOccupation.SpecialPlayerPrefab != null)
				{
					request.PrefabOverrideBody =
						request.RequestedOccupation.SpecialPlayerPrefab; //For stuff like cyborg
				}
			}

			var newPlayerBody = ServerSpawnSubject(request.spawnPos.GetValueOrDefault(), request.PrefabOverrideBody);

			request.ExistingMind.physicalPlayerBrain.AssignedToBody(newPlayerBody);

			newPlayerBody.GetComponent<PlayerSprites>()?.OnCharacterSettingsChange(request.CharacterSettings);
			newPlayerBody.GetComponent<Equipment>()?.RefreshVisibleName();


			foreach (var storage in newPlayerBody.GetComponents<ItemStorage>())
			{
				storage.SetPlayerMind(request.ExistingMind);
			}


			if (request.spawnItems)
			{
				newPlayerBody.GetComponent<DynamicItemStorage>()
					?.SetUpOccupation(request.RequestedOccupation, request.ExistingMind);
			}




		}


		if (request.RequestedOccupation != null && request.showBanner)
		{
			SpawnBannerMessage.Send(
				request.ExistingMind,
				request.RequestedOccupation.DisplayName,
				request.RequestedOccupation.SpawnSound.AssetAddress,
				request.RequestedOccupation.TextColor,
				request.RequestedOccupation.BackgroundColor,
				request.RequestedOccupation.PlaySound);
		}


		if (request.RequestedOccupation.IsCrewmember)
		{
			CrewManifestManager.Instance.AddMember(request.ExistingMind, request.RequestedOccupation.JobType);
		}

		if (SpawnEvent != null)
		{
			SpawnEventArgs args = new SpawnEventArgs() {player = request.ExistingMind};
			SpawnEvent.Invoke(null, args);
		}


		return request.ExistingMind;
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
		if (ServerValidations.HasIllegalCharacterAge(request.CharacterSettings.Age))
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
		if (joinedViewer.isServer || joinedViewer.isLocalPlayer)
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
	public static Mind ServerSpawnPlayer(PlayerSpawnRequest spawnRequest)
	{
		return ServerSpawnNewPlayer(spawnRequest);
	}

	/// <summary>
	/// For use when player is connected and dead.
	/// Respawns the mind's character and transfers their control to it.
	/// </summary>
	/// <param name="forMind"></param>
	public static Mind ServerRespawnPlayer(Mind forMind, Vector3Int? InspawnPos = null )
	{
		if (forMind.AssignedPlayer == null)
		{
			Logger.LogError("Tried to Respawn Mind With no assigned player TODO");
			return null; //Cannot Respawn play that is disconnected?
		}

		//get the settings from the mind
		var occupation = forMind.occupation;
		var settings = forMind.OriginalCharacter;


		PlayerSpawnRequest newOne = new PlayerSpawnRequest(occupation, forMind.AssignedPlayer,
			forMind.AssignedPlayer.ViewerScript, settings, forMind.AssignedPlayer.UserId, InspawnPos, InGhost: false,
			InshowBanner: false
		);
		return ServerSpawnNewPlayer(newOne);
	}

	//Time to start spawning players at arrivals
	private static readonly DateTime ARRIVALS_SPAWN_TIME = new DateTime().AddHours(12).AddMinutes(2);


	/// <summary>
	/// Use this when a player rejoins the game and already has a logged-out body in the game.
	/// Transfers their control back to the body.
	/// </summary>
	/// <param name="Mind"></param>
	public static void ServerAssignMind(ConnectedPlayer ConnectedPlayer, Mind Mind)
	{
		ServerForceAssignPlayerAuthority(ConnectedPlayer.Connection, Mind.gameObject);

		ConnectedPlayer.TargetUpdateMindClient(ConnectedPlayer.Connection, Mind);


		ServerForceAssignPlayerAuthority(Mind, Mind.ghost);
		ServerForceAssignPlayerAuthority(Mind, Mind.physicalPlayerBrain.OrNull()?.gameObject);

		if (Mind.physicalPlayerBrain != null)
		{
			Mind.physicalPlayerBrain.UpdateClientAuthority(ConnectedPlayer, Mind);
		}

		Mind.ResendSpellActions();
		Mind.OnPlayerRegister();
	}


	/// <summary>
	/// Server-side only. Creates the player object on the server side and fires server-side
	/// spawn hooks. Doesn't transfer control to the client yet. Client side hooks should be fired after client has been
	/// informed of the spawn
	/// </summary>
	/// <param name="spawnWorldPosition">world pos to spawn at</param>
	/// <param name="playerPrefab">prefab to spawn for the player</param>
	/// <returns></returns>
	private static GameObject ServerSpawnSubject(Vector3Int spawnWorldPosition, GameObject playerPrefab)
	{
		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = spawnWorldPosition;
		var matrixInfo = MatrixManager.AtPoint(spawnPosition, true);
		var parentTransform = matrixInfo.Objects;

		//using parentTransform.rotation rather than Quaternion.identity because objects should always
		//be upright w.r.t.  localRotation, NOT world rotation
		var player = Spawn.ServerPrefab(playerPrefab, spawnPosition, parentTransform, parentTransform.rotation);

		return player.GameObject;
	}


	/// <summary>
	/// Server-side only. Transfers control of a player object to the indicated connection.
	/// </summary>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	/// thus we shouldn't send any network message which reference's the old body's ID since it won't exist.</param>
	public static void ServerForceAssignPlayerAuthority(Mind Mind, GameObject newBody)
	{
		if (newBody == null) return;
		if (Mind.AssignedPlayer.OrNull()?.Connection == null) return;

		var NetworkIdentity = newBody.GetComponent<NetworkIdentity>();
		if (NetworkIdentity != null)
		{
			if (NetworkIdentity.connectionToClient != null)
			{
				NetworkIdentity.RemoveClientAuthority();
			}

			NetworkIdentity.AssignClientAuthority(Mind.AssignedPlayer.Connection);
		}
	}

	public static void ServerForceAssignPlayerAuthority(NetworkConnection Connection, GameObject newBody)
	{
		if (newBody == null) return;
		if (Connection == null) return;

		var NetworkIdentity = newBody.GetComponent<NetworkIdentity>();
		if (NetworkIdentity != null)
		{
			if (NetworkIdentity.connectionToClient != Connection)
			{
				NetworkIdentity.RemoveClientAuthority();
			}

			NetworkIdentity.AssignClientAuthority(Connection);
		}
	}

}