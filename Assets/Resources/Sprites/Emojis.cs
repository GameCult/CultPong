using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using TMPro;
using UniRx;
using UnityEngine;

[CreateAssetMenu(fileName = "RenameEmojis", menuName = "CultPong/RenameEmojis", order = 1)]
public class Emojis : ScriptableObject
{
	public TMP_SpriteAsset SpriteAsset;
	public TextAsset EmojisJSON;
	public EmojiData EmojiData;

	[Button("Set Emoji Data")]
	public void SetEmojiData()
	{
		Observable.NextFrame().Subscribe(_ =>
		{
			EmojiData = EmojiData.CreateFromJSON(EmojisJSON.text);
			foreach (var s in SpriteAsset.spriteInfoList)
			{
				var dims = s.name.Split('-');
				if (dims.Length == 2)
				{
					var x = int.Parse(dims[0]);
					var y = int.Parse(dims[1]);
					var emoji = EmojiData.emojis.FirstOrDefault(e => e.sheet_x == x && e.sheet_y == y);
					if (emoji != null)
						s.name = emoji.short_name;
					else Debug.LogError($"Emoji ({x},{y}) not found");
				}
			}
		});
	}
}

[Serializable]
public class EmojiData
{
	public Emoji[] emojis;

	public static EmojiData CreateFromJSON(string jsonString)
	{
		return JsonUtility.FromJson<EmojiData>(jsonString);
	}
}

[Serializable]
public class Emoji
{
	public string name;
	public string short_name;
	public int sheet_x;
	public int sheet_y;
}