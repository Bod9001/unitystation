using System.Collections;
using UnityEngine;

namespace Items.Magical
{
	public class InstantSummonsPunishment : SpellBookPunishment
	{
		public override void Punish(Mind player)
		{
			Chat.AddActionMsgToChat(player,
					"<color='red'>The book disappears from your hand!</color>",
					$"<color='red'>The book disappears from {player.ExpensiveName()}'s hand!</color>");

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
