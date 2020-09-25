using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
/*
public class SpriteProcessor : AssetPostprocessor
{
	void OnPreprocessTexture ()
	{
		TextureImporter textureImporter = (TextureImporter)assetImporter;
		textureImporter.textureType = TextureImporterType.Sprite;
		textureImporter.spriteImportMode = SpriteImportMode.Multiple;
		textureImporter.mipmapEnabled = false;
		textureImporter.filterMode = FilterMode.Point;
 
	}
 
	public void OnPostprocessTexture (Texture2D texture)
	{
		Debug.Log("Texture2D: (" + texture.width + "x" + texture.height + ")");
 
     
 
		int spriteSize = 32;
		int colCount = texture.width / spriteSize;
		int rowCount = texture.height / spriteSize;
 
		List<SpriteMetaData> metas = new List<SpriteMetaData>();
 
		for (int r = 0; r < rowCount; ++r)
		{
			for (int c = 0; c < colCount; ++c)
			{
				SpriteMetaData meta = new SpriteMetaData();
				meta.rect = new Rect(c * spriteSize, r * spriteSize, spriteSize, spriteSize);
				meta.name = c + "-" + (rowCount-r-1);
				metas.Add(meta);
			}
		}
 
		TextureImporter textureImporter = (TextureImporter)assetImporter;
		textureImporter.spritesheet = metas.ToArray();
	}
 
	public void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
	{
		Debug.Log("Sprites: " + sprites.Length);
	}
}*/