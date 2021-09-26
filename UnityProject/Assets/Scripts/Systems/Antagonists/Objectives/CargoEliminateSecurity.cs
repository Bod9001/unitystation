using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/CargoEliminateSecurity")]
	public class CargoEliminateSecurity : Objective
	{
		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{
			var transform = GameManager.Instance.PrimaryEscapeShuttle.OrNull()?.MatrixInfo?.Objects.OrNull()?.transform;

			// If the primary shuttle doesn't exist in some form, should this return true?
			if (transform == null) return true;

			foreach (Transform t in transform)
			{
				var player = MindManager.StaticGet(t.gameObject, true);
				if (player != null)
				{
					if (player.JobType == JobType.SECURITY_OFFICER || player.JobType == JobType.HOS
					                                           || player.JobType == JobType.DETECTIVE
					                                           || player.JobType == JobType.WARDEN)
					{
						if(player == null || player.LivingHealthMasterBase == null) continue;
						if (!player.LivingHealthMasterBase.IsDead)
						{
							return false;
						}
					}
				}
			}

			return true;
		}
	}
}