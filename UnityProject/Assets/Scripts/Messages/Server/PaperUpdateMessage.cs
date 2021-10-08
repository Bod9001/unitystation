using Mirror;
using UnityEngine;
using UI;

namespace Messages.Server
{
	public class PaperUpdateMessage : ServerMessage<PaperUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint PaperToUpdate;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.PaperToUpdate);
			var paper = NetworkObject.GetComponent<Paper>();
			paper.PaperString = msg.Message;
			ControlTabs.RefreshTabs();
		}

		public static NetMessage Send(Mind recipient, GameObject paperToUpdate, string message)
		{
			NetMessage msg = new NetMessage
			{
				PaperToUpdate = paperToUpdate.GetComponent<NetworkIdentity>().netId,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
