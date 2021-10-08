using UnityEngine;
using Blob;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Blob")]
	public class Blob : Antagonist
	{
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}

		public override void AfterSpawn(Mind player)
		{
			//Add blob player to game object
			player.GameObjectBody.AddComponent<BlobStarter>();
		}
	}
}
