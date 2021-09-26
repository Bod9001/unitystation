using System.Collections.Generic;
using System.Text;
using Systems;
using HealthV2;
using Items.PDA;
using Objects.Security;
using UnityEngine;

namespace Player
{
	public class ExaminablePlayer : MonoBehaviour, IExaminable
	{
		private const string LILAC_COLOR = "#b495bf";

		/// <summary>
		/// string that will be returned if value cannot be found while examining other player
		/// </summary>
		private const string UNKNOWN_VALUE = "?";

		private InteractableStorage interactableStorage;
		public InteractableStorage InteractableStorage => interactableStorage;

		public Mind CurrentMind => MindManager.Instance.Get(gameObject);

		/// <summary>
		/// Check if player is wearing a mask
		/// </summary>
		/// <returns>true if player don't wear mask</returns>
		private bool IsFaceVisible
		{
			get
			{
				foreach (var itemSlot in CurrentMind.DynamicItemStorage.GetNamedItemSlots(NamedSlot.mask))
				{
					if (itemSlot.IsEmpty == false)
					{
						return false;
					}
				}

				return true;
			}
		}

		[Tooltip("Slots from which other players can read ID card data")]
		[SerializeField]
		private NamedSlot[] readableIDslots = new NamedSlot[]
		{
			NamedSlot.id,
			NamedSlot.belt,
			NamedSlot.leftHand,
			NamedSlot.rightHand
		};

		[SerializeField] private float maxInteractionDistance = 3;
		private LivingHealthMasterBase Health => CurrentMind.LivingHealthMasterBase;
		private Equipment Equipment => CurrentMind.Equipment;
		private string VisibleName => CurrentMind.ExpensiveName(); //Has validation for what can be seen

		private void Awake()
		{
			interactableStorage = GetComponent<InteractableStorage>();
		}

		/// <summary>
		/// Try to find security record by player ID
		/// </summary>
		/// <param name="id">player ID</param>
		/// <param name="securityRecord">player security record if found</param>
		/// <returns>true if security record exists for provided ID</returns>
		private bool TryFindPlayerSecurityRecord(string id, out SecurityRecord securityRecord)
		{
			var records = CrewManifestManager.Instance.SecurityRecords;
			foreach (var record in records)
			{
				if (record.characterSettings.Name.Equals(id) == false)
				{
					continue;
				}

				securityRecord = record;
				return true;
			}

			securityRecord = null;
			return false;
		}

		/// <summary>
		/// Try to get player IDcard
		/// </summary>
		/// <param name="idCard">player id card if found</param>
		/// <returns>true if player has id card</returns>
		private bool TryFindIDCard(out IDCard idCard)
		{
			foreach (var slot in readableIDslots)
			{
				foreach (var itemSlot in CurrentMind.DynamicItemStorage.GetNamedItemSlots(slot))
				{
					if (itemSlot.IsOccupied == false)
					{
						continue;
					}
					// if item is ID card
					if (itemSlot.ItemObject.TryGetComponent(out idCard))
					{
						return true;
					}

					// if item is PDA and IDCard is not null
					if (itemSlot.ItemObject.TryGetComponent<PDALogic>(out var pdaLogic) == false ||
					    pdaLogic.IDCard == null)
					{
						continue;
					}

					idCard = pdaLogic.IDCard;
					return true;
				}
			}

			idCard = null;
			return false;
		}

		public void Examine(Mind sentByPlayer)
		{

			if (sentByPlayer.IsGhosting == false)
			{
				// if distance is too big or is self-examination, send normal examine message
				if (Vector3.Distance(sentByPlayer.BodyWorldPosition, gameObject.WorldPosServer()) >= maxInteractionDistance || sentByPlayer == gameObject)
				{
					BasicExamine(sentByPlayer);
					return;
				}
			}

			//If youre not normal or ghost then only allow basic examination
			//TODO maybe in future have this be a separate setting for each player type?
			if (sentByPlayer.Equipment == null &&
			    sentByPlayer.IsGhosting == false)
			{
				BasicExamine(sentByPlayer);
				return;
			}

			// start itemslot observation
			this.GetComponent<DynamicItemStorage>().ServerAddObserverPlayer(sentByPlayer);
			// send message to enable examination window
			PlayerExaminationMessage.Send(sentByPlayer.AssignedPlayer, this, true);

			//Allow ghosts to keep the screen open even if player moves away
			if(sentByPlayer.IsGhosting) return;

			//stop observing when target player is too far away
			var relationship = RangeRelationship.Between(
				sentByPlayer.GameObjectBody,
				gameObject,
				maxInteractionDistance,
				ServerOnObservationEnded
			);
			SpatialRelationship.ServerActivate(relationship);
		}

		private void BasicExamine(Mind sentByPlayer)
		{
			Chat.AddExamineMsg(sentByPlayer,
				$"This is <b>{VisibleName}</b>.\n" +
				$"{Equipment.Examine()}" +
				$"<color={LILAC_COLOR}>{Health.GetExamineText(sentByPlayer)}</color>");
		}

		private void ServerOnObservationEnded(RangeRelationship cancelled)
		{
			// stop observing item storage
			this.GetComponent<DynamicItemStorage>().ServerRemoveObserverPlayer( MindManager.StaticGet(cancelled.obj1.gameObject) );
			// send message to disable examination window
			PlayerExaminationMessage.Send(MindManager.StaticGet(cancelled.obj1.gameObject).AssignedPlayer, this, false);
		}

		public string GetPlayerNameString()
		{
			// first try to get name from id
			// if can't get name from id get visible name
			return TryFindIDCard(out var idCard) ? idCard.RegisteredName : VisibleName;
		}

		public string GetPlayerSpeciesString()
		{
			// if face is visible - get species by face
			if (IsFaceVisible) { return CurrentMind.OriginalCharacter.Species; }

			//  try get species from security records
			if (TryFindIDCard(out var idCard))
			{
				string id = idCard.RegisteredName;

				if (TryFindPlayerSecurityRecord(id, out var securityRecord))
				{
					return securityRecord.Species;
				}
			}

			return UNKNOWN_VALUE;
		}

		public string GetPlayerJobString()
		{
			// search for ID identity
			if (TryFindIDCard(out var idCard))
			{
				return idCard.JobType.ToString();
			}

			// search for face identity
			if (IsFaceVisible)
			{
				string id =  CurrentMind.OriginalCharacter.Name;
				if (TryFindPlayerSecurityRecord(id, out var securityRecord))
				{
					return securityRecord.Occupation.JobType.ToString();
				}
			}

			return UNKNOWN_VALUE;
		}

		/// <summary>
		/// Reports back if the player is alive or dead.
		/// </summary>
		/// <returns></returns>
		public string GetPlayerStatusString()
		{
			var healthString = new StringBuilder($"<color={LILAC_COLOR}>");

			if ( CurrentMind.LivingHealthMasterBase.IsConscious == false)
			{
				healthString.Append("Dead");

				if (CurrentMind.AssignedPlayer == null)
				{
					healthString.Append(" and no soul");
				}
			}
			else
			{
				healthString.Append("Alive");

				//Alive but not in body
				if (CurrentMind.AssignedPlayer == null)
				{
					healthString.Append(" but vacant");
				}
			}

			healthString.Append("</color>");

			return healthString.ToString();
		}

		/// <summary>
		/// Extra information to be presented on the extended examine view
		/// </summary>
		public string GetAdditionalInformation()
		{
			var result = new StringBuilder();

			if (IsFaceVisible)
			{
				result.Append("Face is visible.\n");
			}

			// result.Append(Health.GetWoundsDescription());

			return result.ToString();
		}

		//Needed so thr examine system knows this script exists
		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return string.Empty;
		}
	}
}
