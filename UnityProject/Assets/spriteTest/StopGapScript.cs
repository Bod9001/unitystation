using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "StopGapScript", menuName = "ScriptableObjects/StopGapScript")]
public class StopGapScript : ScriptableObject
{
	public AssetReferenceAtlasedSprite BigBoss;
	public AssetReferenceAtlasedSprite Clothes;
	public AssetReferenceAtlasedSprite Cutscenes;
	public AssetReferenceAtlasedSprite InHands;
	public AssetReferenceAtlasedSprite Items;
	public AssetReferenceAtlasedSprite Mobs;
	public AssetReferenceAtlasedSprite MobsShared;
	public AssetReferenceAtlasedSprite Objects;
	public AssetReferenceAtlasedSprite Station;
	public AssetReferenceAtlasedSprite Other;
}
