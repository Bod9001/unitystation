using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Items;
using Messages.Server.HealthMessages;

namespace Items.Medical
{
	public class Gauze : HealsTheLiving
	{

		public override void ServerPerformInteraction(HandApply interaction)
		{
			var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if(CheckForBleedingLimbs(LHB, interaction))
			{
				RemoveLimbExternalBleeding(LHB, interaction);
				stackable.ServerConsume(1);
			}
			else if(CheckForBleedingBodyContainers(LHB, interaction))
			{
				RemoveLimbLossBleed(LHB, interaction);
				stackable.ServerConsume(1);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
				$"{MindManager.StaticGet(interaction.TargetObject).ExpensiveName()}'s {interaction.TargetBodyPart} doesn't seem to be bleeding.");
			}
		}

		private void RemoveLimbExternalBleeding(LivingHealthMasterBase livingHealth, HandApply interaction)
		{
			foreach(var bodyPart in livingHealth.BodyPartList)
			{
				if(bodyPart.BodyPartType == interaction.TargetBodyPart)
				{
					if(bodyPart.IsBleedingExternally)
					{
						bodyPart.StopExternalBleeding();
						if(interaction.Performer == MindManager.StaticGet(interaction.TargetObject))
						{
							Chat.AddActionMsgToChat(interaction.Performer,
							$"You stopped your {MindManager.StaticGet(interaction.TargetObject).ExpensiveName()}'s bleeding.",
							$"{interaction.Performer.ExpensiveName()} stopped their own bleeding from their {MindManager.StaticGet(interaction.TargetObject).ExpensiveName()}.");
						}
						else
						{
							Chat.AddActionMsgToChat(interaction.Performer,
							$"You stopped {MindManager.StaticGet(interaction.TargetObject).ExpensiveName()}'s bleeding.",
							$"{interaction.Performer.ExpensiveName()} stopped {MindManager.StaticGet(interaction.TargetObject).ExpensiveName()}'s bleeding.");
						}
					}
				}
			}
		}

		private bool CheckForBleedingLimbs(LivingHealthMasterBase livingHealth, HandApply interaction)
		{
			foreach(var bodyPart in livingHealth.BodyPartList)
			{
				if(bodyPart.BodyPartType == interaction.TargetBodyPart)
				{
					if(bodyPart.IsBleedingExternally)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}