using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UI;
using UnityEngine;

namespace Messages.Server
{
	public class SendSceneList : ServerMessage<SendSceneList.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Data;
		}

		/// To be run on client
		public override void Process(NetMessage msg)
		{
			var Data = JsonConvert.DeserializeObject<List<string>>(msg.Data);
			SubSceneManager.Instance.LoadSpecifiedScenes(Data);
		}

		public static NetMessage
			Send(NetworkConnection recipient, List<string> TheOpenScenes) //TODO In future just sends the saved maps
		{
			NetMessage msg = new NetMessage()
			{
				Data = JsonConvert.SerializeObject(TheOpenScenes)
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}