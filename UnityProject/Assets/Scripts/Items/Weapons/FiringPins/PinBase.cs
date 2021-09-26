using UnityEngine;

namespace Weapons
{
	public abstract class PinBase: MonoBehaviour
	{
		[HideInInspector]
		public Gun gunComp;

		public abstract void ServerBehaviour(AimApply interaction, bool isSuicide);
		public abstract void ClientBehaviour(AimApply interaction, bool isSuicide);

		protected void CallShotServer(AimApply interaction, bool isSuicide)
		{
			gunComp.ServerShoot(interaction.Performer.GameObjectBody, interaction.TargetVector.normalized, UIManager.DamageZone, isSuicide);
		}

		protected void CallShotClient(AimApply interaction, bool isSuicide)
		{
			var dir = gunComp.ApplyRecoil(interaction.TargetVector.normalized);
			gunComp.DisplayShot(LocalPlayerManager.CurrentMind.GameObjectBody, dir, UIManager.DamageZone, isSuicide, gunComp.CurrentMagazine.containedBullets[0].name, gunComp.CurrentMagazine.containedProjectilesFired[0]);
		}

		protected JobType GetJobServer(Mind player)
		{
			//Should probably be changed to implants?
			return player.JobType;
		}

		protected void ClumsyShotServer(AimApply interaction, bool isSuicide)
		{
			//shooting a non-clusmy weapon as a clusmy person
			if (DMMath.Prob(50))
			{
				CallShotServer(interaction, true);

				Chat.AddActionMsgToChat(interaction.Performer,
				"You fumble up and shoot yourself!",
				$"{interaction.Performer.ExpensiveName()} fumbles up and shoots themself!");
			}
			else
			{
				CallShotServer(interaction, isSuicide);
			}
		}
	}
}