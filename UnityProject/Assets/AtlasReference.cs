using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

[CreateAssetMenu(fileName = "AtlasReference", menuName = "ScriptableObjects/AtlasReference")]
public class AtlasReference : SingletonScriptableObject<AtlasReference>
{
	public List<SpriteAtlas> SpriteAtlases = new List<SpriteAtlas>();

	public DictionaryAtlasSpriteAtlas Atlases = new DictionaryAtlasSpriteAtlas();

	[Serializable]
	public class DictionaryAtlasSpriteAtlas : SerializableDictionary<AddressableSpritesHandler.Atlas, SpriteAtlas>
	{
		public DictionaryAtlasSpriteAtlas()
		{
		}

		public DictionaryAtlasSpriteAtlas(IDictionary<AddressableSpritesHandler.Atlas, SpriteAtlas> dict) : base(dict)
		{
		}
	}

}
