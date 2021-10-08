using Messages.Client;
using Mirror;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class RequestAdminTeleport : ClientMessage<RequestAdminTeleport.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string UserToTeleport;
			public string UserToTeleportTo;
			public OpperationList OpperationNumber;
			public bool IsAghost;
			public float vectorX;
			public float vectorY;
			public float vectorZ;
		}

		public override void Process(NetMessage msg)
		{
			switch (msg.OpperationNumber)
			{
				case OpperationList.AdminToPlayer:
					DoAdminToPlayerTeleport(msg);
					return;
				case OpperationList.PlayerToAdmin:
					DoPlayerToAdminTeleport(msg);
					return;
				case OpperationList.AllPlayersToPlayer:
					DoAllPlayersToPlayerTeleport(msg);
					return;
			}
		}

		private void DoPlayerToAdminTeleport(NetMessage msg)
		{
			var admin = PlayersManager.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (admin == null) return;

			Mind userToTeleport = null;

			foreach (var player in PlayersManager.Instance.AllPlayers)
			{
				if (player.UserId == msg.UserToTeleport)
				{
					userToTeleport = player.CurrentMind;

					break;
				}
			}

			if (userToTeleport == null) return;

			var coord = new Vector3 {x = msg.vectorX, y = msg.vectorY, z = msg.vectorZ };

			userToTeleport.PlayerSync.SetPosition(coord, true);

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{SentByPlayer.Username} teleported {userToTeleport.AssignedPlayer.Username} to themselves", msg.Userid);
		}

		private void DoAdminToPlayerTeleport(NetMessage msg)
		{
			var admin = PlayersManager.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (admin == null) return;

			Mind userToTeleportTo = null;

			foreach (var player in PlayersManager.Instance.AllPlayers)
			{
				if (player.UserId == msg.UserToTeleportTo)
				{
					userToTeleportTo = player.CurrentMind;

					break;
				}
			}

			if (userToTeleportTo == null) return;

			var playerScript = SentByPlayer.CurrentMind;

			if (playerScript == null) return;

			playerScript.PlayerSync.SetPosition(userToTeleportTo.BodyWorldPosition, true);

			string message;

			if (msg.IsAghost)
			{
				message = $"{SentByPlayer.Username} teleported to {userToTeleportTo.AssignedPlayer.Username} as a ghost";
			}
			else
			{
				message = $"{SentByPlayer.Username} teleported to {userToTeleportTo.AssignedPlayer.Username} as a player";
			}

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(message, msg.Userid);
		}

		private void DoAllPlayersToPlayerTeleport(NetMessage msg)
		{
			var admin = PlayersManager.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (admin == null) return;

			Mind destinationPlayer = null;

			foreach (var player in PlayersManager.Instance.AllPlayers)
			{
				if (player.UserId == msg.UserToTeleportTo)
				{
					destinationPlayer = player.CurrentMind;

					break;
				}
			}

			if (destinationPlayer == null) return;

			foreach (var player in PlayersManager.Instance.AllPlayers)
			{
				Mind userToTeleport = player.CurrentMind;

				if (userToTeleport == null) continue;

				if (msg.IsAghost)
				{
					var coord = new Vector3 { x = msg.vectorX, y = msg.vectorY, z = msg.vectorZ };

					userToTeleport.PlayerSync.SetPosition(coord, true);
				}
				else
				{
					userToTeleport.PlayerSync.SetPosition(destinationPlayer.BodyWorldPosition, true);
				}
			}

			var stringMsg = $"{SentByPlayer.Username} teleported all players to {destinationPlayer.AssignedPlayer.Username}";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(stringMsg, msg.Userid);
		}

		public static NetMessage Send(string userId, string adminToken, string userToTeleport, string userToTelportTo, OpperationList opperation, bool isAghost, Vector3 Coord)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToTeleport = userToTeleport,
				UserToTeleportTo = userToTelportTo,
				OpperationNumber = opperation,
				IsAghost = isAghost,
				vectorX = Coord.x,
				vectorY = Coord.y,
				vectorZ = Coord.z
			};

			Send(msg);
			return msg;
		}

		public enum OpperationList
		{
			AdminToPlayer = 1,
			PlayerToAdmin = 2,
			AllPlayersToPlayer = 3
		}
	}
}
