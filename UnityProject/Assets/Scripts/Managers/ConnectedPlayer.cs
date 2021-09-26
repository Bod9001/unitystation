using System;
using Messages.Server;
using UnityEngine;
using Mirror;

/// <summary>
/// Server-only full player information, Stuff that is independent of Character
/// </summary>
public class ConnectedPlayer : NetworkBehaviour
{
	public string Username;

	public string ClientId;

	public string UserId;

	public NetworkConnection Connection;

	public JoinedViewer ViewerScript;

	public Mind CurrentMind;

	public CharacterSettings PreRoundCharacterSettings;

	public bool MindHasThisBody(GameObject Body)
	{
		if (CurrentMind == null) return false;
		return CurrentMind.HasThisBody(Body);
	}

	public override string ToString()
	{
		if (this == PlayersManager.InvalidPlayer)
		{
			return "Invalid player";
		}
		return $"ConnectedPlayer {nameof(Username)}: {Username}, {nameof(ClientId)}: {ClientId}, {nameof(UserId)}: {UserId}, {nameof(Connection)}: {Connection}";
	}

	public void Start()
	{
		Logger.Log("o3o");
	}
}
