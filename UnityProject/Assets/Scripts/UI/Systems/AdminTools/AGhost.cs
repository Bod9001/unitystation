using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AGhost : MonoBehaviour
{
	public void OnClick()
	{
		if (LocalPlayerManager.CurrentMind == null) return;
		var adminId = DatabaseAPI.ServerData.UserID;
		var adminToken = PlayersManager.Instance.AdminToken;
		LocalPlayerManager.CurrentMind.playerNetworkActions.CmdAGhost(adminId, adminToken);
	}
}
