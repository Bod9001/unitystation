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

		private Action CompleteReturn;

		public void LoadAddressableReference(Action OnCompleteAction)
		{
			CompleteReturn = OnCompleteAction;
			AddressableSpritesHandler.LoadSprite(this, LoadSprite);
		}

		private void LoadSprite(Sprite obj)
		{
			RuntimeSprite = obj;
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
	}

	public void RegisterCompletion()
	{
		CompletedSoFar++;
		if (CompletedSoFar >= neededToComplete)
		{
			FinishLoading.Invoke();
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
#endif
}