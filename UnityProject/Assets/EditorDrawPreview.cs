using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public class MyProjectViewExtentions
{
	static MyProjectViewExtentions()
	{
		EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
	}

	public static Dictionary<string, Texture2D> Dictionaryguid = new Dictionary<string, Texture2D>();

	public class DatabaseEntry
	{
		public DatabaseEntry(string _guid, GameObject _gameObject, SpriteHandler _spriteHandler,
			SpriteDataSO _spriteDataSO,
			Texture2D _generatedTexture2D)
		{
			guid = _guid;
			gameObject = _gameObject;
			spriteHandler = _spriteHandler;
			spriteDataSO = _spriteDataSO;
			generatedTexture2D = _generatedTexture2D;
		}

		public string guid;
		public GameObject gameObject;
		public SpriteHandler spriteHandler;
		public SpriteDataSO spriteDataSO;
		public Texture2D generatedTexture2D;
	}

	private static void DrawProjectItem(string guid, Rect selectionRect)
	{
		var sTing = AssetDatabase.GUIDToAssetPath(guid);
		if (sTing.Contains(".prefab"))
		{
			Texture2D mainTex;
			if (Dictionaryguid.ContainsKey(guid))
			{
				mainTex = Dictionaryguid[guid];
			}
			else
			{
				var GameO = AssetDatabase.LoadAssetAtPath<GameObject>(sTing);
				mainTex = GetAddressableReferences(GameO);
				if (mainTex == null)
				{
					mainTex = GetSpriteRenderer(GameO);
				}

				Dictionaryguid[guid] = mainTex;
			}

			selectionRect.height = selectionRect.width;
			Texture2D icon = mainTex;
			if (icon != null)
			{
				GUI.DrawTexture(selectionRect, icon);
			}
		}
	}

	public static Texture2D GetSpriteRenderer(GameObject GameO)
	{
		var SRs = GameO.GetComponentsInChildren<SpriteRenderer>();
		if (SRs.Length == 0) return null;
		var T2D = GenerateNewTexture2D();
		foreach (var SR in SRs)
		{
			if (SR.enabled && SR.sprite != null)
			{
				T2D = CopySprite(T2D, SR.sprite);
			}
		}

		return T2D;
	}

	public static Texture2D GetAddressableReferences(GameObject GameO)
	{
		var SH = GameO.GetComponentInChildren<SpriteHandler>();
		if (SH == null) return null;
		var SO = SH.spriteDataSOAddressable.editorAsset as SpriteDataSO;

		if (SO == null) return null;
		return GetAddressableReferencesTexture2D(GenerateNewTexture2D(), SO, SH);
	}


	private static Texture2D GetAddressableReferencesTexture2D(Texture2D mainTex, SpriteDataSO SO, SpriteHandler SH)
	{
		SO.Variance[0].Frames[0].singleSpriteReference.editorAsset
			.GetSprite(SO.Variance[SH.GetVariantindex()].Frames[0].singleSpriteReference.SubObjectName);

		var srtings = AssetDatabase.FindAssets(
			SO.Variance[SH.GetVariantindex()].Frames[0].singleSpriteReference.SubObjectName +
			" t:Sprite");
		string String = "";
		if (srtings.Length > 0)
		{
			String = srtings[0];
		}
		else
		{
			return null;
		}
		var path = AssetDatabase.GUIDToAssetPath(String);


		Sprite NewSprite = null;
		var NewSprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
		if (NewSprites.Count() > 1)
		{
			foreach (var Sprit in NewSprites)
			{
				if (Sprit.name.Contains("_" + SH.GetVariantindex()))
				{
					NewSprite = Sprit;
				}
			}
		}
		else
		{
			NewSprite = NewSprites.First();
		}

		return (CopySprite(mainTex, NewSprite));
	}

	public static Texture2D GenerateNewTexture2D()
	{
		var mainTex = new Texture2D(32, 32, TextureFormat.ARGB32, false);
		mainTex.filterMode = FilterMode.Point;
		mainTex.alphaIsTransparency = true;
		Unity.Collections.NativeArray<Color32> data = mainTex.GetRawTextureData<Color32>();
		for (int xy = 0; xy < data.Length; xy++)
		{
			data[xy] = new Color32(30, 30, 30, 255);
			//data[xy] = new Color(0.15f, 0.15f, 0.15f, 1f);
		}

		mainTex.Apply();
		return mainTex;
	}

	public static Texture2D CopySprite(Texture2D mainTex, Sprite NewSprite)
	{
		int xx = 0;
		int yy = 0;

		for (int x = (int) NewSprite.textureRect.position.x;
			x < (int) NewSprite.textureRect.position.x + NewSprite.rect.width;
			x++)
		{
			for (int y = (int) NewSprite.textureRect.position.y;
				y < NewSprite.textureRect.position.y + NewSprite.rect.height;
				y++)
			{
				var Pix = NewSprite.texture.GetPixel(x, y);
				if (Pix.a > 0f)
				{
					//Logger.Log(yy + " <XX YY> " + xx + "   " +  x + " <X Y> " + y  );
					mainTex.SetPixel(xx, yy, Pix);
				}
				else
				{
					mainTex.SetPixel(xx, yy, new Color32(30, 30, 30, 255));
				}

				yy = yy + 1;
			}

			yy = 0;
			xx = xx + 1;
		}

		mainTex.Apply();

		return mainTex;
	}
}
#endif