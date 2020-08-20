using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddressableSpritesHandler : MonoBehaviour
{

	public static Atlas FindAtlasContaining(Sprite inSprite)
	{
		foreach (var Atlase in AtlasReference.Instance.Atlases)
		{
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
