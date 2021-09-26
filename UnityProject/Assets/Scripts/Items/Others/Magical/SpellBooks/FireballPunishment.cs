using System.Collections;
using UnityEngine;
using Systems.Explosions;

namespace Items.Magical
{
	/// <summary>
	/// Creates an explosion centered on the player.
	/// </summary>
	public class FireballPunishment : SpellBookPunishment
	{
		[SerializeField]
		private GameObject explosionPrefab = default;

		public override void Punish(Mind player)
		{
			GameObject explosionObject = Spawn.ServerPrefab(explosionPrefab, player.BodyWorldPosition).GameObject;
			if (explosionObject.TryGetComponent<ExplosionComponent>(out var explosion))
			{
				explosion.Explode(MatrixManager.AtPoint(player.BodyWorldPosition.RoundToInt(), true).Matrix);
			}
			else
			{
				Logger.LogError($"No explosion component found on {explosionObject}! Was the right prefab assigned?", Category.Spells);
			}
		}
	}
}
