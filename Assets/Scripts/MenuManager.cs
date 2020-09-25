using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
	public static MenuManager Instance
	{
		get
		{
			if (_instance != null)
				return _instance;

			_instance = (MenuManager)FindObjectOfType(typeof(MenuManager));
			return _instance;
		}
	}
	private static MenuManager _instance;
	
	[ReorderableList]
	public Fade[] Menus;

	private Fade _current;
	private bool _enabled = true;

	public void ShowMenu(int index)
	{
		if (!_enabled) return;
		
		var temp = _current;
		var target = Menus[Mathf.Clamp(index, 0, Menus.Length)];
		
		HideMenu(() => target.gameObject.SetActive(true));
		_current = target;
		
		//CloudManager.Instance.AddMessageListener("Chat", message => Debug.Log(message.GetString(0)));
	}

	public void HideMenu(Action onComplete)
	{
		if (!_enabled) return;

		_enabled = false;
		
		if (_current == null)
		{
			_enabled = true;
			onComplete();
			return;
		}
		
		var temp = _current;
		temp.OnComplete.AddListener(() =>
		{
			_enabled = true;
			onComplete();
			temp.OnComplete.RemoveAllListeners();
		});
		temp.FadeOut();

		_current = null;
	}
/*
	void Start()
	{
		ShowMenu(0);
	}*/
}
