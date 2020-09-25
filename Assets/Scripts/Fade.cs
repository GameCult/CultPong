using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
	public float Duration = 1;
	public float Speed = 5;
	public Transform CameraTarget;
	
	public UnityEvent OnComplete = new UnityEvent();
	
	private TextMeshProUGUI[] _texts;
	private Image[] _images;
	private float _startTime;
	private Vector3 _startPosition;
	private Quaternion _startRotation;

	void Awake ()
	{
//		_buttons = GetComponentsInChildren<MenuButton>();
	}

	IEnumerator FadeInCoroutine()
	{
		//Debug.Log("Fade In");
		//yield return null;
		
		_texts = GetComponentsInChildren<TextMeshProUGUI>();
		_images = GetComponentsInChildren<Image>();
		
		foreach (var text in _texts)
		{
			var textColor = text.color;
			textColor.a = 0;
			text.color = textColor;
		}
		foreach (var image in _images)
		{
			var color = image.color;
			color.a = 0;
			image.color = color;
		}

		var duration = (_startPosition - CameraTarget.position).magnitude / Speed;
		
		// Move Camera
		_startTime = Time.time;
		_startPosition = Camera.main.transform.position;
		_startRotation = Camera.main.transform.rotation;
		while (Time.time - _startTime < duration)
		{
			var tween = (Time.time - _startTime) / duration;
			Camera.main.transform.position = Vector3.Lerp(_startPosition,CameraTarget.position, tween);
			Camera.main.transform.rotation = Quaternion.Slerp(_startRotation, CameraTarget.rotation, tween);
			yield return null;
		}
		Camera.main.transform.position = CameraTarget.position;
		Camera.main.transform.rotation = CameraTarget.rotation;
		
		// Fade UI in
		_startTime = Time.time;
		while (Time.time - _startTime < Duration)
		{
			var tween = (Time.time - _startTime) / Duration;
			foreach (var text in _texts)
			{
				var textColor = text.color;
				textColor.a = tween;
				text.color = textColor;
			}
			foreach (var image in _images)
			{
				var color = image.color;
				color.a = tween;
				image.color = color;
			}
			yield return null;
		}

		OnComplete.Invoke();
	}

	IEnumerator FadeOutCoroutine()
	{
		
		// Fade UI out
		_startTime = Time.time;
		while (Time.time - _startTime < Duration)
		{
			var tween = (Time.time - _startTime) / Duration;
			foreach (var text in _texts)
			{
				var textColor = text.color;
				textColor.a = 1 - tween;
				text.color = textColor;
			}
			foreach (var image in _images)
			{
				var color = image.color;
				color.a = 1 - tween;
				image.color = color;
			}
			yield return null;
		}
		
		//yield return new WaitForSeconds(.5f);
		
		//Debug.Log("Fade out end");
		
		OnComplete.Invoke();
		
		gameObject.SetActive(false);
	}

	public void FadeOut()
	{
		StartCoroutine(FadeOutCoroutine());
	}

	private void OnEnable()
	{
		Observable.NextFrame().Subscribe(_ => StartCoroutine(FadeInCoroutine()));
	}
}
