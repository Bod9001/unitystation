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
	public int setID = -1;

	[System.Serializable]
	public class Variant
	{
		public List<Frame> Frames = new List<Frame>();
	}


	[System.Serializable]
	public class Frame
	{
		[System.NonSerialized] public Sprite RuntimeSprite;
#if UNITY_EDITOR
		public Sprite sprite;
#endif
		public string spriteName;
		public float secondDelay;
		public AddressableSpritesHandler.Atlas AtlasUsing;
		public UnityEngine.AddressableAssets.AssetReferenceAtlasedSprite TestAddress;


		private Action CompleteReturn;

		public void LoadAddressableReference(Action OnCompleteAction)
		{
			CompleteReturn = OnCompleteAction;
			//AddressableSpritesHandler.LoadSprite(this, LoadSprite);
			if (TestAddress.RuntimeKey == null) return;
			if (TestAddress.Asset != null)
			{
				LoadSprite(TestAddress.Asset as Sprite);
				return;
			}

			TestAddress.LoadAssetAsync().Completed += LoadSprite;
			// var asyncOperationHandle =
			// Addressables.LoadAssetAsync<Sprite>(Frame.AtlasUsing + "[" + Frame.spriteName + "]");
			//You ask why strings and my responses because they need to
			//optimise the UI because it's a laggy piece of and Is buggy as well
			//await asyncOperationHandle.Task; // wait for the task to complete before we try to get the result
		}

		private void LoadSprite(Sprite obj)
		{
			RuntimeSprite = obj;
			CompleteReturn.Invoke();
		}

		private void LoadSprite(AsyncOperationHandle<Sprite> obj)
		{
			RuntimeSprite = obj.Result;
			CompleteReturn.Invoke();
		}
	}

	private List<Action> FinishLoading = new List<Action>();
	private int CompletedSoFar = 0;
	private int neededToComplete = 0;
	private bool IsLoading = false;

	public void LoadAddressableReference(Action OnComplete = null)
	{
		FinishLoading.Add(OnComplete);
		if (IsLoading) return;
		IsLoading = true;
		neededToComplete = 0;
		CompletedSoFar = 0;

		foreach (var Varianc in Variance)
		{
			foreach (var Frames in Varianc.Frames)
			{
				neededToComplete++;
				Frames.LoadAddressableReference(RegisterCompletion);
			}
		}
	}

	public void RegisterCompletion()
	{
		CompletedSoFar++;
		if (CompletedSoFar >= neededToComplete)
		{
			if (FinishLoading != null)
			{
				IsLoading = false;
				foreach (var Callback in FinishLoading)
				{
					Callback?.Invoke();
				}
				FinishLoading.Clear();
			}
		}
	}

#if UNITY_EDITOR
	public void Awake()
	{
		{
			if (setID == -1)
			{
				if (SpriteCatalogue.Instance == null)
				{
					Resources.LoadAll<SpriteCatalogue>("ScriptableObjects/SOs singletons");
				}

				if (!SpriteCatalogue.Instance.Catalogue.Contains(this))
				{
					SpriteCatalogue.Instance.AddToCatalogue(this);
				}

				setID = SpriteCatalogue.Instance.Catalogue.IndexOf(this);
				Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorSave(), this);
			}
		}
	}

	IEnumerator EditorSave()
	{
		yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(3);
		EditorUtility.SetDirty(this);
		EditorUtility.SetDirty(SpriteCatalogue.Instance);
		AssetDatabase.SaveAssets();
	}


	public void StartSetSpriteAtlas()
	{
		Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorSaveSpriteAtlas(), this);
	}

	IEnumerator EditorSaveSpriteAtlas()
	{
		yield return null;
		SetSpriteAtlas();
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}

	public void SetSpriteAtlas()
	{
		foreach (var Varianc in Variance)
		{
			foreach (var Frame in Varianc.Frames)
			{
				if (Frame.sprite == null) continue;
				var Stall = AddressableSpritesHandler.FindAtlasContainingSpriteAtlas(Frame.sprite);
				Frame.TestAddress.SetEditorAsset(Stall);
				Frame.TestAddress.SetEditorSubObject(Frame.sprite);
				//Frame.sprite = null;
			}
		}
	}
#endif
}