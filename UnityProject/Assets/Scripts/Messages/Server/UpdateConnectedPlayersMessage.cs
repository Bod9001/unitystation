using System.Collections.Generic;
using Mirror;
using UI;

namespace Messages.Server
{
	/// <summary>
	///     Message that tells clients what their ConnectedPlayers list should contain
	/// </summary>
	public class UpdateConnectedPlayersMessage : ServerMessage<UpdateConnectedPlayersMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ClientConnectedPlayer[] Players;
		}

		public override void Process(NetMessage msg)
		{
			if (PlayersManager.Instance == null || PlayersManager.Instance.ClientConnectedPlayers == null) return;

			if (msg.Players != null)
			{
				Logger.LogFormat("This client got an updated PlayerList state: {0}", Category.Connections, string.Join(",", msg.Players));
				PlayersManager.Instance.ClientConnectedPlayers.Clear();
				for (var i = 0; i < msg.Players.Length; i++)
				{
					PlayersManager.Instance.ClientConnectedPlayers.Add(msg.Players[i]);
				}
			}

			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
			UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().UpdatePlayerCount(msg.Players?.Length ?? 0);
		}

		public static NetMessage Send()
		{
			Logger.LogFormat("This server informing all clients of the new PlayerList state: {0}", Category.Connections,
				string.Join(",", PlayersManager.Instance.AllPlayers));

			var prepareConnectedPlayers = new List<ClientConnectedPlayer>();
			var count = 0;
			foreach (ConnectedPlayer c in PlayersManager.Instance.AllPlayers)
			{
				var tag = "";

				if (PlayersManager.Instance.IsAdmin(c.UserId))
				{
					tag = "<color=red>[Admin]</color>";
				}
				else if (PlayersManager.Instance.IsMentor(c.UserId))
				{
					tag = "<color=#6400ff>[Mentor]</color>";
				}

				prepareConnectedPlayers.Add(new ClientConnectedPlayer
				{
					UserName = c.Username,
					Tag = tag,
					Index = count
				});

				count++;
			}

			NetMessage msg = new NetMessage();
			msg.Players = prepareConnectedPlayers.ToArray();

			SendToAll(msg);
			return msg;
		}
	}
}
