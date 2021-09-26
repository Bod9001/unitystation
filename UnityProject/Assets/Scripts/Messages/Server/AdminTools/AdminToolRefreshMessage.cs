using System.Collections.Generic;
using System.Linq;
using AdminTools;
using InGameEvents;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminToolRefreshMessage : ServerMessage<AdminToolRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public NetMessage IgnoreMe;

		public override void Process(NetMessage msg)
		{
			var adminPageData = JsonUtility.FromJson<AdminPageRefreshData>(msg.JsonData);

			var pages = GameObject.FindObjectsOfType<AdminPage>();
			foreach (var g in pages)
			{
				g.GetComponent<AdminPage>().OnPageRefresh(adminPageData);
			}
		}

		public static NetMessage Send(ConnectedPlayer recipient, string adminID)
		{
			//Gather the data:
			var pageData = new AdminPageRefreshData();

			//Game Mode Information:
			pageData.availableGameModes = GameManager.Instance.GetAvailableGameModeNames();
			pageData.isSecret = GameManager.Instance.SecretGameMode;
			pageData.currentGameMode = GameManager.Instance.GetGameModeName(true);
			pageData.nextGameMode = GameManager.Instance.NextGameMode;

			//Event Manager
			pageData.randomEventsAllowed = InGameEventsManager.Instance.RandomEventsAllowed;

			//Round Manager
			pageData.nextMap = SubSceneManager.AdminForcedMainStation;
			pageData.nextAwaySite = SubSceneManager.AdminForcedAwaySite;
			pageData.allowLavaLand = SubSceneManager.AdminAllowLavaland;
			pageData.alertLevel = GameManager.Instance.CentComm.CurrentAlertLevel.ToString();

			//Centcom
			pageData.blockCall = GameManager.Instance.PrimaryEscapeShuttle.blockCall;
			pageData.blockRecall = GameManager.Instance.PrimaryEscapeShuttle.blockRecall;

			//Player list info:
			pageData.players = GetAllPlayerStates(adminID);

			var data = JsonUtility.ToJson(pageData);

			NetMessage  msg =
				new NetMessage  {JsonData = data};

			SendTo(recipient, msg);
			return msg;
		}

		private static List<AdminPlayerEntryData> GetAllPlayerStates(string adminID)
		{
			var playerList = new List<AdminPlayerEntryData>();
			if (string.IsNullOrEmpty(adminID)) return playerList;
			var ToSearchThrough = PlayersManager.Instance.AllPlayers.ToList();
			ToSearchThrough.AddRange(PlayersManager.Instance.loggedOff);
			foreach (var player in ToSearchThrough)
			{
				if (player == null) continue;
				//if (player.Connection == null) continue;

				var entry = new AdminPlayerEntryData();
				entry.name = player.CurrentMind.OrNull()?.CharactersName;
				entry.uid = player.UserId;
				entry.currentJob = player.CurrentMind.JobType.ToString();
				entry.accountName = player.Username;
				if (player.Connection != null)
				{
					entry.ipAddress = player.Connection.address;
				}

				if (player.CurrentMind != null && player.CurrentMind.LivingHealthMasterBase != null)
				{
					entry.isAlive = player.CurrentMind.LivingHealthMasterBase.ConsciousState != ConsciousState.DEAD;
				} else
				{
					entry.isAdmin = false;
				}
				entry.isAntag = MindManager.Instance.AntagMinds.Contains(player.CurrentMind);
				entry.isAdmin = PlayersManager.Instance.IsAdmin(player.UserId);
				entry.isOnline = true;

				playerList.Add(entry);
			}

			return playerList.OrderBy(p => p.name).ThenBy(p => p.isOnline).ToList();
		}
	}
}
