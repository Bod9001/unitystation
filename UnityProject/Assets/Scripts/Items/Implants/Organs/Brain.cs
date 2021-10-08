using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Brain : Organ
	{
		public PlayerBrain BrainScript;

		public void Awake()
		{
			BrainScript = this.GetComponent<PlayerBrain>();
		}

		//stuff in here?
		//nah
		public override void SetUpSystems()
		{
			base.SetUpSystems();
			RelatedPart.HealthMaster.Setbrain(this);
		}


		public void SetUpBrain()
		{
			if (RelatedPart.OrNull()?.HealthMaster == null ) return;
			RelatedPart.HealthMaster.Setbrain(this);
		}

		//Ensure removal of brain

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.brain = null;
			if (BrainScript != null)
			{
				BrainScript.RemovalFromBody();
			}
		}
	}
}