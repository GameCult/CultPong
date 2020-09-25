using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SettingsMenu : MonoBehaviour {

	void Start ()
	{
		var panel = GetComponentInChildren<PropertiesPanel>();
		panel.Inspect("Left Name", () => PlayerPrefs.GetString("LeftName", GameManager.RandomName), s =>
		{
			PlayerPrefs.SetString("LeftName", s);
			if(CloudManager.Connected && PlayerPrefs.GetInt("PreferredSide", 0)==0)
				CloudManager.Send("Name", s);
		});
		panel.Inspect("Right Name", () => PlayerPrefs.GetString("RightName", GameManager.RandomName), s =>
		{
			PlayerPrefs.SetString("RightName", s);
			if(CloudManager.Connected && PlayerPrefs.GetInt("PreferredSide", 0)==1)
				CloudManager.Send("Name", s);
		});
		panel.Inspect("Preferred Side", () => PlayerPrefs.GetInt("PreferredSide", 0), s => PlayerPrefs.SetInt("PreferredSide", s), Enum.GetNames(typeof(Side)));
		panel.Inspect("Left Paddle", () => PlayerPrefs.GetInt("LeftPaddle", 0),
			p =>
			{
				PlayerPrefs.SetInt("LeftPaddle", p);
				GameManager.Instance.Left.PaddleSettings = GameManager.Instance.Paddles[Mathf.Clamp(p,0,GameManager.Instance.Paddles.Length)];
				GameManager.Instance.RefreshPaddle(GameManager.Instance.Left);
			}, GameManager.Instance.Paddles.Select(pad=>pad.name).ToArray());
		panel.Inspect("Right Paddle", () => PlayerPrefs.GetInt("RightPaddle", 0),
			p =>
			{
				PlayerPrefs.SetInt("RightPaddle", p);
				GameManager.Instance.Right.PaddleSettings = GameManager.Instance.Paddles[Mathf.Clamp(p,0,GameManager.Instance.Paddles.Length)];
				GameManager.Instance.RefreshPaddle(GameManager.Instance.Right);
			}, GameManager.Instance.Paddles.Select(pad=>pad.name).ToArray());
		panel.RefreshValues();
	}
	
	void Update () {
		
	}
}
