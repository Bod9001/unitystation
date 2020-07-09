using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SpriteData", menuName = "ScriptableObjects/SpriteData")]
public class SpriteDataSO : ScriptableObject
{
	public List<Variant> Variance = new List<Variant>();
	public bool IsPalette = false;

	[System.Serializable]
	public class Variant
	{
		public List<Frame> Frames = new List<Frame>();
	}


	[System.Serializable]
	public class Frame
	{
		public AssetReferenceAtlasedSprite singleSpriteReference;

		public Sprite sprite;

		private Action CompleteReturn;

		public float secondDelay;

		public void LoadAddressableReference(Action OnCompleteAction)
		{
			CompleteReturn = OnCompleteAction;
			if (Application.isPlaying == false)
			{
#if UNITY_EDITOR
				sprite = singleSpriteReference.editorAsset.GetSprite(singleSpriteReference.SubObjectName);
				CompleteReturn.Invoke();
				return;


				//Finds the The same sprite in the asset database By name, then loads it and then pick the correct one
				var path = AssetDatabase.GUIDToAssetPath(
					AssetDatabase.FindAssets(singleSpriteReference.SubObjectName)[0]);
				var Sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
				if (Sprites.Length > 1)
				{
					Sprites = Sprites.OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf('_') + 1))).ToArray();
				}

				sprite = Sprites.First(x => x.name == singleSpriteReference.SubObjectName);
#endif
			}
			else
			{
				singleSpriteReference.LoadAssetAsync<Sprite>().Completed += LoadSprite;
			}
		}


		private void LoadSprite(AsyncOperationHandle<Sprite> obj)
		{
			sprite = obj.Result;
			CompleteReturn.Invoke();
		}
	}

	private Action FinishLoading;
	private int CompletedSoFar = 0;
	private int neededToComplete = 0;
	public void LoadAddressableReference(Action OnComplete)
	{
		neededToComplete = 0;
		CompletedSoFar = 0;
		FinishLoading = OnComplete;
		foreach (var Varianc in Variance)
		{
			foreach (var Frames in Varianc.Frames)
			{
				neededToComplete++;
				Frames.LoadAddressableReference(RegisterCompletion);
			}
		}
		Logger.Log("fin");
	}

	public void RegisterCompletion()
	{
		CompletedSoFar++;
		if (CompletedSoFar >= neededToComplete)
		{
			FinishLoading.Invoke();
		}
	}

}