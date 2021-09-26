using System.Collections.Generic;
using System.Linq;
using AdminTools;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminPlayerListRefreshMessage : ServerMessage<AdminPlayerListRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var listData = JsonUtility.FromJson<AdminPlayersList>(msg.JsonData);

			foreach (var v in UIManager.Instance.adminChatWindows.playerListViews)
			{
				if (v.gameObject.activeInHierarchy)
				{
					v.ReceiveUpdatedPlayerList(listData);
				}
			}
		}

		public static NetMessage Send(ConnectedPlayer recipient, string adminID)
		{
			AdminPlayersList playerList = new AdminPlayersList();
			//Player list info:
			playerList.players = GetAllPlayerStates(adminID);

			var data = JsonUtility.ToJson(playerList);

			NetMessage  msg =
				new NetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

			SendTo(recipient, msg);
			return msg;
		}

		private static List<AdminPlayerEntryData> GetAllPlayerStates(string adminID)
		{
			var playerList = new List<AdminPlayerEntryData>();
			if (string.IsNullOrEmpty(adminID)) return playerList;
			foreach (var player in PlayersManager.Instance.AllPlayers)
			{
				if (player == null) continue;
				if (player.Connection == null) continue;

				var entry = new AdminPlayerEntryData();
				entry.name = player.CurrentMind.CharactersName;
				entry.uid = player.UserId;
				entry.currentJob = player.CurrentMind.JobType.ToString();
				entry.accountName = player.Username;
				if (player.Connection != null)
				{
					entry.ipAddress = player.Connection.address;
					if (player.CurrentMind != null && player.CurrentMind.LivingHealthMasterBase != null)
					{
						entry.isAlive = player.CurrentMind.LivingHealthMasterBase.ConsciousState != ConsciousState.DEAD;
					}
					else
					{
						entry.isAdmin = false;
					}
					entry.isOnline = true;
					entry.isAntag = MindManager.Instance.AntagMinds.Contains(player.CurrentMind);
					entry.isAdmin = PlayersManager.Instance.IsAdmin(player.UserId);
				}
				else
				{
					entry.isOnline = false;
				}

				playerList.Add(entry);
			}

			return playerList.OrderBy(p => p.name).ThenBy(p => p.isOnline).ToList();
		}
	}
}
