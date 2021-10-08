using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class OrganicHumanBrain : PlayerBrain
{
	public Brain OrganBrain;

	public void Awake()
	{
		OrganBrain = this.GetComponent<Brain>();

	}
}
