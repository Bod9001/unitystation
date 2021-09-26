using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to assassinate someone on the station
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/Assassinate")]
	public class Assassinate : Objective
	{
		/// <summary>
		/// The person to assassinate
		/// </summary>
		private Mind Target;

		/// <summary>
		/// Make sure there's at least one player which hasn't been targeted, not including the candidate
		/// </summary>
		protected override bool IsPossibleInternal(Mind candidate)
		{
			int targetCount = PlayersManager.Instance.InGamePlayers.Count(p =>
				(p.CurrentMind != candidate) && !AntagManager.Instance.TargetedPlayers.Contains(p.CurrentMind));
			return (targetCount > 0);
		}

		/// <summary>
		/// Select the target randomly (not including Owner or other targeted players)
		/// </summary>
		protected override void Setup()
		{
			// Get all ingame players except the one who owns this objective and players who have already been targeted and the ones who cant be targeted
			List<ConnectedPlayer> playerPool = PlayersManager.Instance.InGamePlayers.Where(p =>
				(p.CurrentMind != Owner) && !AntagManager.Instance.TargetedPlayers.Contains(p.CurrentMind) &&
				p.CurrentMind.occupation != null && p.CurrentMind.occupation.IsTargeteable
			).ToList();

			if (playerPool.Count == 0)
			{
				FreeObjective();
				return;
			}

			// Pick a random target and add them to the targeted list
			Target = playerPool.PickRandom().CurrentMind;

			//If still null then its a free objective
			if (Target == null || Target.occupation == null)
			{
				FreeObjective();
				return;
			}

			AntagManager.Instance.TargetedPlayers.Add(Target);
			description = $"Assassinate {Target.CharactersName}, the {Target.occupation.DisplayName}";
		}

		private void FreeObjective()
		{
			Logger.LogWarning("Unable to find any suitable assassination targets! Giving free objective",
				Category.Antags);
			description = "Free objective";
			Complete = true;
		}

		protected override bool CheckCompletion()
		{
			return (Target.LivingHealthMasterBase == null || Target.LivingHealthMasterBase.IsDead);
		}
	}
}