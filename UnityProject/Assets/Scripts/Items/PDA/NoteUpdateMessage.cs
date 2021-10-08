using Messages.Server;
using Mirror;
using UnityEngine;
using UI;

namespace Items.PDA
{
	public class NoteUpdateMessage : ServerMessage<NoteUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint PDAToUpdate;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.PDAToUpdate);
			var notes = NetworkObject.GetComponent<PDANotesNetworkHandler>();
			notes.NoteString = msg.Message;
			ControlTabs.RefreshTabs();
		}

		/// <summary>
		/// Sends the new string to the gameobject
		/// </summary>
		public static NetMessage Send(Mind recipient, GameObject noteToUpdate, string message)
		{
			NetMessage msg = new NetMessage
			{
				PDAToUpdate = noteToUpdate.GetComponent<NetworkIdentity>().netId,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
