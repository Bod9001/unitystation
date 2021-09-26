using UnityEngine;
using Systems.GhostRoles;
using System.Collections;
using ScriptableObjects;

namespace Items.Others
{
	public class ReinforcementTeleporter : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		private bool WasUsed = false;

		[SerializeField] private GhostRoleData ghostRole = default;

		private uint createdRoleKey;

		private Mind userPlayer;

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			CreateGhostRole(interaction);
		}

		public void CreateGhostRole(HandActivate interaction)
		{
			if (createdRoleKey != default && GhostRoleManager.Instance.serverAvailableRoles.ContainsKey(createdRoleKey)) return;
			else if (WasUsed) return;

			createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(ghostRole);
			GhostRoleServer role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];

			role.OnPlayerAdded += SpawnReinforcement;
			role.OnTimerExpired += ClearGhostRole;

			userPlayer = interaction.Performer;
			Chat.AddExamineMsgFromServer(userPlayer, $"The {gameObject.ExpensiveName()} sends out a reinforcement request!");
		}

		private void SpawnReinforcement(Mind player)
		{
			player.playerNetworkActions.ServerRespawnPlayerAntag(player, "Nuclear Operative");
			Chat.AddExamineMsgFromServer(userPlayer, $"The {gameObject.ExpensiveName()} lets out a chime, reinforcement found!");
			WasUsed = true;
			StartCoroutine(TeleportOnSpawn(player));
		}

		private IEnumerator TeleportOnSpawn(Mind player)
		{
			// Waits until the player is no longer a ghost...
			while (player.IsGhosting)
			{
				yield return WaitFor.EndOfFrame;
			}

			player.PlayerSync.SetPosition(gameObject.AssumedWorldPosServer(), true);
		}

		public void ClearGhostRole()
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
			Chat.AddExamineMsgFromServer(userPlayer, $"The reinforcement request times out.");
		}
	}
}
