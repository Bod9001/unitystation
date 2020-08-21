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
		if (LoadedSprites.ContainsKey(Frame.AtlasUsing) &&
		    LoadedSprites[Frame.AtlasUsing].ContainsKey(Frame.spriteName))
		{
			OnCompleteAction.Invoke(LoadedSprites[Frame.AtlasUsing][Frame.spriteName]);
			return;
		}


		var asyncOperationHandle =
			Addressables.LoadAssetAsync<Sprite>(Frame.AtlasUsing + "[" + Frame.spriteName + "]");
		//You ask why strings and my responses because they need to
		//optimise the UI because it's a laggy piece of and Is buggy as well
		await asyncOperationHandle.Task; // wait for the task to complete before we try to get the result
		if (LoadedSprites.ContainsKey(Frame.AtlasUsing) == false)
		{
			LoadedSprites[Frame.AtlasUsing] = new Dictionary<string, Sprite>();
		}

		LoadedSprites[Frame.AtlasUsing][Frame.spriteName] = asyncOperationHandle.Result;

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