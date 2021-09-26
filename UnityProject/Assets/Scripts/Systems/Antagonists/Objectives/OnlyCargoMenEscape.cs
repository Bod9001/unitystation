using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to set off the nuke on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/OnlyCargoMenEscape")]
	public class OnlyCargoMenEscape : Objective
	{
		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{
			int playersFound = 0;
			var primaryEscape = GameManager.Instance.PrimaryEscapeShuttle;
			var objects = primaryEscape.OrNull()?.MatrixInfo?.Objects;
			var objectsTransform = objects.OrNull()?.transform;

			if (objectsTransform == null) return false;

			foreach (Transform t in objectsTransform)
			{
				var playerDetails = MindManager.StaticGet(t.gameObject);
				if (playerDetails != null)
				{
					playersFound++;
					if (playerDetails.JobType != JobType.CARGOTECH && playerDetails.JobType != JobType.MINER
					                                           && playerDetails.JobType != JobType.QUARTERMASTER)
					{
						if(playerDetails == null || playerDetails.LivingHealthMasterBase == null) continue;
						if (!playerDetails.LivingHealthMasterBase.IsDead)
						{
							return false;
						}
					}
				}
			}

			if (playersFound != 0)
			{
				return true;
			}

			return false;
		}
	}
}