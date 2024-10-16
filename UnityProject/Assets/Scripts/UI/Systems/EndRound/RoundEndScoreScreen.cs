using System;
using System.Collections.Generic;
using System.Text;
using Messages.Server;
using Mirror;
using Systems.Score;
using TMPro;
using UnityEngine;

namespace UI.Systems.EndRound
{
	public class RoundEndScoreScreen : MonoBehaviour
	{
		[SerializeField] private TMP_Text scoreSummary;
		[SerializeField] private TMP_Text scoreResult;
		[SerializeField] private TMP_Text ratingResult;


		public void ShowScore(List<ScoreEntry> entries, int finalScore)
		{
			// NOTE //
			// Right now there is only one page so we can get away with putting all of the code here.
			// In the future when antags and other stuff receive their own page we should move this logic to different components that this class updates
			// Based on the context and content
			// NOTE //
			StringBuilder theGoodList = new StringBuilder();
			theGoodList.AppendLine("<i><u><b>The Good:</b></u></i>");
			StringBuilder theBadList = new StringBuilder();
			theBadList.AppendLine("<i><u><b>The Bad:</b></u></i>");
			StringBuilder theWeirdList = new StringBuilder();
			theWeirdList.AppendLine("<i><u><b>The Weird:</b></u></i>");

			StringBuilder finalResult = new StringBuilder();

			entries.Shuffle(); //Randomize the positions of all entries.

			foreach (var Entry in entries)
			{
				if (Entry.Alignment == ScoreAlignment.Unspecified || Entry.Category == ScoreCategory.MiscScore) continue;
				var result = ScoreMachine.ScoreTypeResultAsString(Entry);
				if (result == null)
				{
					Logger.LogError("[ScoreMachine] - Unidentified score entry type detected while building UI text for round end.");
					continue;
				}
				if (result.ToLower().Contains("true")) result = "<color=green>Success!</color>";
				if (result.ToLower().Contains("false")) result = "<color=red>Failed!</color>";
				switch (Entry.Alignment)
				{
					case ScoreAlignment.Good:
						theGoodList.AppendLine($"<b>{Entry.ScoreName}</b> : {result}");
						break;
					case ScoreAlignment.Bad:
						theBadList.AppendLine($"<b>{Entry.ScoreName}</b> : {result}");
						break;
					case ScoreAlignment.Weird:
						theWeirdList.AppendLine($"<b>{Entry.ScoreName}</b> : {result}");
						break;
					default:
						throw new NotImplementedException();
				}
			}

			finalResult.Append(theGoodList);
			finalResult.AppendLine(" ");
			finalResult.Append(theBadList);
			finalResult.AppendLine(" ");
			finalResult.Append(theWeirdList);
			finalResult.AppendLine(" ");

			scoreSummary.text = finalResult.ToString();
			scoreResult.text = finalScore.ToString();
			this.SetActive(true);
			ServerShowUIToClients.NetMessage msg = new ServerShowUIToClients.NetMessage();
			msg.ScoreResult = scoreResult.text;
			msg.ScoreSummary = scoreSummary.text;
			ratingResult.text = RatePerformance(finalScore);
			ServerShowUIToClients.SendToAll(msg);
		}

		public void SyncScore(string finalResult, string finalScore, string finalRating)
		{
			scoreSummary.text = finalResult;
			scoreResult.text = finalScore;
			ratingResult.text = finalRating;
			this.SetActive(true);
		}

		private string RatePerformance(int finalScore)
		{
			if (finalScore <= -1500) return "Expunge from records";
			if (finalScore <= -1000) return "Singularity Fodder";
			if (finalScore <= -500) return "Clown Station";
			if (finalScore <= 0) return "Disaster";
			if (finalScore.IsBetween(0, 500)) return "Decent Shift";
			if (finalScore.IsBetween(500, 1000)) return "Net Profit";
			return finalScore.IsBetween(1000, 1500) ? "Robust Station" : "N/A";
		}
	}

	public class ServerShowUIToClients : ServerMessage<ServerShowUIToClients.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string ScoreSummary;
			public string ScoreResult;
			public string ScoreRating;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.ScoreScreen.SyncScore(msg.ScoreSummary, msg.ScoreResult, msg.ScoreRating);
		}
	}
}
