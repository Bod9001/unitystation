using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "AtlasReference", menuName = "ScriptableObjects/AtlasReference")]
public class AtlasReference : ScriptableObject
{
	public UnityEngine.AddressableAssets.AssetReference Atlas;
}
