using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     Represents a network message sent from the server to clients.
	///     Sending a message will invoke the Process() method on the client.
	///
	///		Channel 0 reliable, 1 unreliable, 2 reliable(For Chat Messages) and 3 reliable(For VV to not cause blockages)
	/// </summary>
	public abstract class ServerMessage<T> : GameMessageBase<T> where T : struct, NetworkMessage
	{
		public static void SendToAll(T msg, int channel = 0)
		{
			NetworkServer.SendToAll(msg, channel);
			Logger.LogTraceFormat("SentToAll {0}", Category.Server, msg.GetType());
		}

		public static void SendToAllExcept(T msg, GameObject excluded, int channel = 0)
		{
			if (excluded == null)
			{
				SendToAll(msg);
				return;
			}

			var excludedConnection = excluded.GetComponent<NetworkIdentity>().connectionToClient;

			foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
			{
				if (connection.Value != null && connection.Value != excludedConnection)
				{
					connection.Value.Send(msg, channel);
				}
			}

			Logger.LogTraceFormat("SentToAllExcept {1}: {0}", Category.Server, msg.GetType(), excluded.name);
		}

		public static void SendTo(ConnectedPlayer recipient, T msg, int channel = 0)
		{
			if (recipient == null) return;
			SendTo(recipient.Connection, msg, channel);
		}

		public static void SendTo(Mind recipient, T msg, int channel = 0)
		{
			if (recipient.OrNull()?.AssignedPlayer.OrNull()?.Connection == null) return;
			SendTo(recipient.AssignedPlayer.Connection, msg, channel);
		}

		public static void SendTo(NetworkConnection recipient, T msg, int channel = 0)
		{
			if (recipient == null) return;
			recipient.Send(msg, channel);
		}

		/// <summary>
		/// Sends the network message only to players who are visible from the
		/// worldPosition
		/// </summary>
		public static void SendToVisiblePlayers(Vector2 worldPosition, T msg, int channel = 0)
		{
			//Player script is not null for these players
			var players = PlayersManager.Instance.InGamePlayers;

			LayerMask layerMask = LayerMask.GetMask( "Door Closed");
			for (int i = players.Count - 1; i > 0; i--)
			{
				if (Vector2.Distance(worldPosition,
					players[i].CurrentMind.GameObjectBody.AssumedWorldPosServer()) > 14f)
				{
					//Player in the list is too far away for this message, remove them:
					players.Remove(players[i]);
					continue;
				}

				//within range, but check if they are in another room or hiding behind a wall
				if (MatrixManager.Linecast(worldPosition, LayerTypeSelection.Walls, layerMask,
					players[i].CurrentMind.GameObjectBody.AssumedWorldPosServer()).ItHit)
				{
					//if it hit a wall remove that player
					players.Remove(players[i]);
				}
			}

			//Sends the message only to visible players:
			foreach (ConnectedPlayer player in players)
			{
				if (PlayersManager.Instance.ContainsConnection(player.Connection))
				{
					player.Connection.Send(msg, channel);
				}
			}
		}

		/// <summary>
		/// Sends the network message only to players who are within a 15 tile radius
		/// of the worldPostion. This method disregards if the player is visible or not
		/// </summary>
		public static void SendToNearbyPlayers(Vector2 worldPosition, T msg, int channel = 0)
		{
			var players = PlayersManager.Instance.AllPlayers;

			for (int i = players.Count - 1; i > 0; i--)
			{
				if (Vector2.Distance(worldPosition,
					players[i].CurrentMind.GameObjectBody.transform.position) > 15f)
				{
					//Player in the list is too far away for this message, remove them:
					players.Remove(players[i]);
				}
			}

			foreach (ConnectedPlayer player in players)
			{
				if (PlayersManager.Instance.ContainsConnection(player.Connection))
				{
					player.Connection.Send(msg, channel);
				}
			}
		}

		public static void SendToAdmins(T msg, int channel = 0)
		{
			var admins = PlayersManager.Instance.GetAllAdmins();

			foreach (var admin in admins)
			{
				if (PlayersManager.Instance.ContainsConnection(admin.Connection))
				{
					admin.Connection.Send(msg, channel);
				}
			}
		}

		public static void SendToMentors(T msg, int channel = 0)
		{
			var mentors = PlayersManager.Instance.GetAllMentors();

			foreach (var mentor in mentors)
			{
				if (PlayersManager.Instance.ContainsConnection(mentor.Connection))
				{
					mentor.Connection.Send(msg, channel);
				}
			}
			var admins = PlayersManager.Instance.GetAllAdmins();

			foreach (var admin in admins)
			{
				if (PlayersManager.Instance.ContainsConnection(admin.Connection))
				{
					admin.Connection.Send(msg, channel);
				}
			}
		}
	}
}
