using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brain : MonoBehaviour
{
	public GameObject ConnectedBody;
	public Mind RelatedMind;

	//if there is a body connected return that but If not then return this
	public GameObject PhysicalBody => this.ConnectedBody != null ? this.ConnectedBody : this.gameObject;
}