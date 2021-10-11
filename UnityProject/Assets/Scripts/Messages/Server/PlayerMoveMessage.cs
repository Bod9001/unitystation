using Mirror;
using UnityEngine;
using UI;

namespace Messages.Server
{
	/// <summary>
	///Tells client to apply PlayerState (update his position, flight direction etc) to the given player
	/// </summary>
	public class PlayerMoveMessage : ServerMessage<PlayerMoveMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public PlayerState State;
			/// Player to be moved
			public PlayerSync PlayerSync;

			public override string ToString()
			{
				return $"[PlayerMoveMessage State={State} Subject={PlayerSync}]";
			}
		}

		/// To be run on client
		public override void Process(NetMessage msg)
		{
			if (msg.PlayerSync == null) return; //People moving while map is loading type of thing
			msg.PlayerSync.UpdateClientState(msg.State);

			if (  LocalPlayerManager.HasThisBody(NetworkObject) ) {
				if (msg.State.ResetClientQueue)
				{
					msg.PlayerSync.ClearQueueClient();
					msg.PlayerSync.RollbackPrediction();
				}
				if (msg.State.MoveNumber == 0 ) {
					msg.PlayerSync.ClearQueueClient();
					msg.PlayerSync.RollbackPrediction();
				}

				ControlTabs.CheckTabClose();
			}
		}

		public static NetMessage Send(NetworkConnection recipient, PlayerSync PlayerSync, PlayerState state)
		{
			var msg = new NetMessage
			{
				PlayerSync = PlayerSync,
				State = state,
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static void SendToAll(PlayerSync PlayerSync, PlayerState state)
		{
			if (PlayerSync.playerScript.PlayerState == PlayerScript.PlayerStates.Ghost)
			{
				// Send ghost positions only to ghosts
				foreach (var connectedPlayer in PlayersManager.Instance.InGamePlayers)
				{
					if (PlayerUtils.IsGhost(connectedPlayer.CurrentMind))
					{
						Send(connectedPlayer.Connection, PlayerSync, state);
					}
				}
			}
			else
			{
				var msg = new NetMessage
				{
					PlayerSync = PlayerSync,
					State = state,
				};

				SendToAll(msg);
			}
		}
	}
}

public static partial class CustomReadWriteFunctions
{
	public static void WritePlayerSync(this NetworkWriter writer, PlayerSync value)
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

	public static PlayerSync ReadPlayerSync(this NetworkReader reader)
	{
		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		PlayerSync PlayerSync = networkIdentity != null ? networkIdentity.GetComponent<PlayerSync>() : null;
		return PlayerSync;
	}
}