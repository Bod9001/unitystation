using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Button that pass player that pressed this button
/// Useful for testing player ID access
/// </summary>
[RequireComponent(typeof(Button))]
[Serializable]
public class NetButtonAuth : NetUIStringElement
{
	public MindEvent ServerMethod;

	public override void ExecuteServer(Mind subject)
	{
		ServerMethod.Invoke(subject);
	}

}