using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Initialisation;
using Mirror;
using UnityEngine;

public class PlayerBrain : NetworkBehaviour
{
	[SyncVar]
	public GameObject ConnectedBody;


	public Mind RelatedMind;

	//if there is a body connected return that but If not then return this
	public GameObject PhysicalBody
	{
		get
		{
			if (ConnectedBody != null)
			{
				return ConnectedBody;
			}
			else
			{
				return this.gameObject;
			}
		}
	}


	public Transform CameraFollowTarget()
	{
		if (ConnectedBody != null)
		{
			return ConnectedBody.transform;
		}
		else
		{
			return this.gameObject.transform;
		}
	}

	public virtual void ReEnterBody()
	{
		if (ConnectedBody != null) //TODO Is a simple Check to more advanced
		{
			UIManager.Display.hudBottomHuman.gameObject.SetActive(true);
			SetPlayerControl(RelatedMind.AssignedPlayer.Connection, ConnectedBody);
			ServerSetupPlayer();
		}
	}

	public virtual void AssignedToBody(GameObject Body)
	{
		PlayerSpawn.ServerAssignPlayerAuthorityBody(RelatedMind, Body);
		ConnectedBody = Body;
		RelatedMind.IsGhosting = false;
		ReEnterBody();

		LoadManager.RegisterActionDelayed(() => ImplantSelf(ConnectedBody),1 );
	}

	[TargetRpc]
	public virtual void SetPlayerControl(NetworkConnection target,  GameObject Body)
	{
		LocalPlayerManager.SetPlayerForControl( Body.GetComponent<IPlayerControllable>());
		LoadManager.RegisterActionDelayed(ClientSetupPlayer,2);
	}


	public virtual void ServerSetupPlayer() //All the controls and UI and jizz is on the Client
	{
		UIManager.LinkUISlots(ItemStorageLinkOrigin.localPlayer);
	}


	public virtual void ClientSetupPlayer() //All the controls and UI and jizz is on the Client
	{
		UIManager.Internals.SetupListeners();
		UIManager.Instance.panelHudBottomController.SetupListeners();
		UIManager.LinkUISlots(ItemStorageLinkOrigin.localPlayer);
		EventManager.Broadcast( Event.PlayerRejoined);
		EventManager.Broadcast( Event.PlayerSpawned);
		// Hide ghosts
		var mask = Camera2DFollow.followControl.cam.cullingMask;
		mask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
		Camera2DFollow.followControl.cam.cullingMask = mask;
	}

	public virtual void ImplantSelf(GameObject Body)
	{
		var Health = Body.GetComponent<LivingHealthMasterBase>();
		foreach (var BodyPart in Health.BodyPartList)
		{
			foreach (var BodyPart2 in BodyPart.OrganList)  //Grumble grumble grumble
			{
				if (BodyPart2.TryGetComponent<Brain>(out var Bring))
				{
					BodyPart.OrganStorage.ServerSwap(this.gameObject, BodyPart2.gameObject, this);
					return;
				}
			}
		}

	}

	public virtual void RemovalFromBody()
	{
		ConnectedBody = null;
		//TODO UI stuff
		//Body transfer stuff
	}

	public virtual void UpdateClientAuthority(ConnectedPlayer ConnectedPlayer,Mind Mind)
	{
		Logger.LogError("Assign player control ( connection stuff identity )");
	}
}