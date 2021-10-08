using UnityEngine;

/// <summary>
/// A viewer's request to spawn into the game as a new player. Doesn't necessarily guarantee they will actually
/// spawn with what they requested, depending on game mode!
/// </summary>
public class PlayerSpawnRequest
{
	/// <summary>
	/// Occupation requested to spawn as (won't necessarily be what they get if they
	/// end up spawning as an antag)
	/// </summary>
	public readonly Occupation RequestedOccupation;

	/// <summary>
	/// JoinedViewer component of the player attempting to spawn.
	/// </summary>
	public readonly JoinedViewer JoinedViewer;

	/// <summary>
	/// Character settings the viewer is attempting to spawn with.
	/// </summary>
	public readonly CharacterSettings CharacterSettings;


	public readonly ConnectedPlayer ConnectedPlayer;

	/// <summary>
	/// UserID the viewer is attempting to spawn with.
	/// </summary>
	public readonly string UserID;

	public readonly bool showBanner;

	public Mind ExistingMind;

	public Vector3Int? spawnPos = null;

	public readonly bool Ghost = true;

	public GameObject PrefabOverrideGhost = null;

	public readonly bool Brain = true;

	public GameObject PrefabOverrideBrain = null;

	public readonly bool Body = true;

	public GameObject PrefabOverrideBody = null;

	public readonly bool spawnItems = true;

	public PlayerSpawnRequest(Occupation requestedOccupation, ConnectedPlayer InConnectedPlayer,
		JoinedViewer joinedViewer, CharacterSettings characterSettings,
		string userID, Vector3Int? InspawnPos = null, bool InGhost = true, bool InBrain = true,
		bool InshowBanner = true, bool InBody = true,
		GameObject InPrefabOverrideGhost = null, GameObject InPrefabOverrideBrain = null,
		GameObject InPrefabOverrideBody = null, bool InspawnItems = true)
	{
		RequestedOccupation = requestedOccupation;
		JoinedViewer = joinedViewer;
		ConnectedPlayer = InConnectedPlayer;
		CharacterSettings = characterSettings;
		UserID = userID;
		showBanner = InshowBanner;
		spawnPos = InspawnPos;
		Ghost = InGhost;
		Brain = InBrain;
		Body = InBody;

		PrefabOverrideGhost = InPrefabOverrideGhost;
		PrefabOverrideBrain = InPrefabOverrideBrain;
		PrefabOverrideBody = InPrefabOverrideBody;

		spawnItems = InspawnItems;
	}

	/// <summary>
	/// Create a new player spawn info indicating a request to spawn with the
	/// selected occupation and settings.
	/// </summary>
	/// <returns></returns>
	public static PlayerSpawnRequest RequestOccupation(JoinedViewer requestedBy, ConnectedPlayer InConnectedPlayer,
		Occupation requestedOccupation, CharacterSettings characterSettings, string userID)
	{
		return new PlayerSpawnRequest(requestedOccupation, InConnectedPlayer, requestedBy, characterSettings, userID);
	}

	public static PlayerSpawnRequest RequestOccupation(ConnectedPlayer requestedBy, Occupation requestedOccupation)
	{
		return new PlayerSpawnRequest(requestedOccupation, requestedBy, requestedBy.ViewerScript,
			requestedBy.PreRoundCharacterSettings, requestedBy.UserId);
	}

	public override string ToString()
	{
		return
			$"{nameof(RequestedOccupation)}: {RequestedOccupation}, {nameof(JoinedViewer)}: {JoinedViewer}, {nameof(CharacterSettings)}: {CharacterSettings}";
	}
}