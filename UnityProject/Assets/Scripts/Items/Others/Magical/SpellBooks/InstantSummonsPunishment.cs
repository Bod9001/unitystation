using UnityEngine;
using AddressableReferences;


namespace Items.Magical
{
	public class InstantSummonsPunishment : SpellBookPunishment
	{
		[SerializeField]
		private AddressableAudioSource punishSfx = default;

		public override void Punish(Mind player)
		{
			SoundManager.PlayNetworkedAtPos(punishSfx, player.BodyWorldPosition, sourceObj: player.GameObjectBody);
			Chat.AddActionMsgToChat(player,
				"<color=red>The book disappears from your hand!</color>",
				$"<color=red>The book disappears from {player.ExpensiveName()}'s hand!</color>");


			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
