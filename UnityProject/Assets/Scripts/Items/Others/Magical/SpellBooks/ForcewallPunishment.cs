using System.Collections;
using UnityEngine;
using AddressableReferences;


namespace Items.Magical
{
	// TODO: make the player a statue when petrification is added.

	/// <summary>
	/// Punishes the player by temporarily preventing movement input and removing player speech.
	/// </summary>
	public class ForcewallPunishment : SpellBookPunishment
	{
		[SerializeField, Range(1, 300)] private int petrifyTime = 60;

		[SerializeField]
		private AddressableAudioSource punishSfx = default;

		public override void Punish(Mind player)
		{
			Chat.AddCombatMsgToChat(player,
				"You suddenly feel very solid!",
				$"{player.ExpensiveName()} goes very still! {player.TheyPronoun()}'s been petrified!");

			player.PlayerMove.allowInput = false;
			// Piggy-back off IsMiming property to prevent the player from speaking.
			// TODO: convert to player trait when we have that system.
			player.IsMiming = true;

			StartCoroutine(Unpetrify(player));
			SoundManager.PlayNetworkedAtPos(punishSfx, player.BodyWorldPosition, sourceObj: player.GameObjectBody);
			Chat.AddCombatMsgToChat(player,
				"<size=60><b>Your body freezes up! Can't... move... can't... think...</b></size>",
				$"{player.ExpensiveName()}'s skin rapidly turns to marble!");

		}

		private IEnumerator Unpetrify(Mind script)
		{
			yield return WaitFor.Seconds(petrifyTime);
			if (script == null) yield break;

			script.PlayerMove.allowInput = true;
			script.IsMiming = false;

			Chat.AddExamineMsgFromServer(script, "You feel yourself again.");
		}
	}
}
