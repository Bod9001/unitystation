using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Items.Bureaucracy
{
	/// <summary>
	/// Allows players to roleplay reading books.
	/// </summary>
	public class SimpleBook : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		[Tooltip("How many pages (or remarks) to read before this book is considered read.")]
		[BoxGroup("Settings"), SerializeField, Range(1, 10)]
		private int pagesToRead = 3;
		[Tooltip("How long each page (or remark) takes to read.")]
		[BoxGroup("Settings"), SerializeField]
		private float timeToReadPage = 5f;
		[Tooltip("Whether this book can be read by the same person multiple times.")]
		[BoxGroup("Settings"), SerializeField]
		private bool canBeReadMultipleTimes = true;
		[Tooltip("Whether only one person (the first to attempt) can read this book.")]
		[BoxGroup("Settings"), SerializeField]
		private bool allowOnlyOneReader = false;

		[Tooltip("The possible strings that could be chosen to display to the reader when a page is considered read.")]
		[SerializeField, ReorderableList]
		private string[] remarks = default;

		private readonly Dictionary<Mind, int> readerProgress = new Dictionary<Mind, int>();
		protected bool hasBeenRead = false;

		protected bool AllowOnlyOneReader => allowOnlyOneReader;

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			var player = interaction.Performer;

			if (TryReading(player))
			{
				StartReading(player);
			}
		}

		/// <summary>
		/// Whether it is possible for the reader to read this book.
		/// </summary>
		/// <returns></returns>
		protected virtual bool TryReading(Mind player)
		{
			if (canBeReadMultipleTimes == false &&
					readerProgress.ContainsKey(player) && readerProgress[player] > pagesToRead)
			{
				Chat.AddExamineMsgFromServer(player, $"You already know all about <b>{gameObject.ExpensiveName()}</b>!");
				return false;
			}
			if (AllowOnlyOneReader && hasBeenRead)
			{
				Chat.AddExamineMsgFromServer(player, $"It seems you can't read this book... has someone claimed it?");
				return false;
			}

			return true;
		}

		private void StartReading(Mind player)
		{
			if (readerProgress.ContainsKey(player) == false)
			{
				readerProgress.Add(player, 0);
				Chat.AddActionMsgToChat(player,
						$"You begin reading {gameObject.ExpensiveName()}...",
						$"{player.ExpensiveName()} begins reading {gameObject.ExpensiveName()}...");
				ReadBook(player);
			}
			else
			{
				Chat.AddActionMsgToChat(player,
						$"You resume reading {gameObject.ExpensiveName()}...",
						$"{player.ExpensiveName()} resumes reading {gameObject.ExpensiveName()}...");
				ReadBook(player, readerProgress[player]);
			}
		}

		// Note: this is a recursive method.
		private void ReadBook(Mind player, int pageToRead = 0)
		{
			if (pageToRead >= pagesToRead || pageToRead > 10)
			{
				FinishReading(player);
				return;
			}

			StandardProgressActionConfig cfg = new StandardProgressActionConfig(
				StandardProgressActionType.Construction,
				false,
				false
			);
			StandardProgressAction.Create(cfg, ReadPage).ServerStartProgress(
				player.registerTile,
				timeToReadPage,
				player
			);

			void ReadPage()
			{
				readerProgress[player]++;

				// TODO: play random page-turning sound => pageturn1.ogg || pageturn2.ogg || pageturn3.ogg
				string remark = remarks[Random.Range(0, remarks.Length)];
				Chat.AddExamineMsgFromServer(player, remark);

				ReadBook(player, readerProgress[player]);
			}
		}

		/// <summary>
		/// Triggered when the reader has read all of the pages.
		/// </summary>
		protected virtual void FinishReading(Mind player)
		{
			hasBeenRead = true;

			if (canBeReadMultipleTimes)
			{
				readerProgress[player] = 0;
			}

			Chat.AddActionMsgToChat(player,
					$"You finish reading {gameObject.ExpensiveName()}!",
					$"{player.ExpensiveName()} finishes reading {gameObject.ExpensiveName()}!");
		}
	}
}
