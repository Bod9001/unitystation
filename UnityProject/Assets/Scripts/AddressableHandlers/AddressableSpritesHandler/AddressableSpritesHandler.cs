using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public class AddressableSpritesHandler : MonoBehaviour
{
	public static Dictionary<Atlas, Dictionary<string, Sprite>> LoadedSprites =
		new Dictionary<Atlas, Dictionary<string, Sprite>>();


	public static async Task LoadSprite(SpriteDataSO.Frame Frame, Action<Sprite> OnCompleteAction)
	{
		if (Frame.TestAddress.Asset != null){
			OnCompleteAction.Invoke(Frame.TestAddress.Asset as Sprite);
			return;
		}

		var asyncOperationHandle  = Frame.TestAddress.LoadAssetAsync();
		// var asyncOperationHandle =
			// Addressables.LoadAssetAsync<Sprite>(Frame.AtlasUsing + "[" + Frame.spriteName + "]");
		//You ask why strings and my responses because they need to
		//optimise the UI because it's a laggy piece of and Is buggy as well
		await asyncOperationHandle.Task; // wait for the task to complete before we try to get the result

		OnCompleteAction.Invoke(asyncOperationHandle.Result);
	}


	public static Atlas FindAtlasContaining(Sprite inSprite)
	{
		foreach (var Atlase in AtlasReference.Instance.Atlases)
		{
			//if (Atlase.Value.GetSprite(inSprite.name) != null)
			if (Atlase.Value.CanBindTo(inSprite))
			{
				return Atlase.Key;
			}
		}

		//Logger.Log(" The corresponding atlas for" + inSprite.name + " Could not be found, are you sure you added it to AtlasReference Singleton or is it covered under a atlas");
		return Atlas.None;
	}


	public static SpriteAtlas FindAtlasContainingSpriteAtlas(Sprite inSprite)
	{
		foreach (var Atlase in AtlasReference.Instance.Atlases)
		{
			//if (Atlase.Value.GetSprite(inSprite.name) != null)
			if (Atlase.Value.CanBindTo(inSprite))
			{
				return Atlase.Value;
			}
		}

		//Logger.Log(" The corresponding atlas for" + inSprite.name + " Could not be found, are you sure you added it to AtlasReference Singleton or is it covered under a atlas");
		return null;
	}

	public enum Atlas
	{
		None,
		BigBoss,
		Clothes,
		Cutscenes,
		InHands,
		Items,
		Mobs,
		MobsShared,
		Objects,
		Other,
		Station,
		Test
	}
}