using Systems.Ai;
using Blob;
using HealthV2;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	public class SuicideMessage : ClientMessage<SuicideMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			Logger.Log("Player '" + SentByPlayer.Username + "' has committed suicide", Category.Health);
			var livingHealthBehaviour = SentByPlayer.CurrentMind.LivingHealthMasterBase;
			if (livingHealthBehaviour)
			{
				if (livingHealthBehaviour.IsDead)
				{
					Logger.LogWarning("Player '" + SentByPlayer.Username + "' is attempting to commit suicide but is already dead.", Category.Health);
				}
				else
				{
					livingHealthBehaviour.Death();
				}

				return;
			}

			if (SentByPlayer.CurrentMind.GameObjectBody.TryGetComponent<AiPlayer>(out var aiPlayer))
			{
				aiPlayer.Suicide();
				return;
			}

			if (SentByPlayer.CurrentMind.GameObjectBody.TryGetComponent<BlobPlayer>(out var blobPlayer))
			{
				blobPlayer.Death();
			}
		}


		/// <summary>
		/// Tells the server to kill the player that sent this message
		/// </summary>
		/// <param name="obj">Dummy variable that is required to make this signiture different
		/// from the non-static function of the same name. Just pass null. </param>
		/// <returns></returns>
		public static NetMessage Send(Object obj)
		{
			NetMessage msg = new NetMessage();
			Send(msg);
			return msg;
		}


	}
}
