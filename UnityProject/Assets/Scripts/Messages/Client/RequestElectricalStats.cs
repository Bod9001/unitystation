using Messages.Server;
using Mirror;
using UnityEngine;
using Systems.Electricity;

namespace Messages.Client
{
	/// <summary>
	///     Request electrical stats from the server
	/// </summary>
	public class RequestElectricalStats : ClientMessage<RequestElectricalStats.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint ElectricalItem;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] { msg.ElectricalItem});

			if (Validations.IsGameObjectReachable(SentByPlayer.CurrentMind.registerTile,NetworkObject, true, context: NetworkObjects[0]))
			{
				//Try powered device first:
				var poweredDevice = NetworkObjects[0].GetComponent<ElectricalOIinheritance>();
				if (poweredDevice != null)
				{
					SendDataToClient(poweredDevice.InData.Data, SentByPlayer);
					return;
				}
			}
		}

		void SendDataToClient(ElectronicData data, ConnectedPlayer recipient)
		{
			string json = JsonUtility.ToJson(data);
			ElectricalStatsMessage.Send(recipient, json);
		}

		public static NetMessage Send(GameObject electricalItem)
		{
			NetMessage msg = new NetMessage
			{
				ElectricalItem = electricalItem.GetComponent<NetworkIdentity>().netId,
			};

			Send(msg);
			return msg;
		}
	}
}
