using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DragField : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
	public TMP_InputField InputField;
	public bool AllowNegative = true;
	public float Sensitivity = 1;
	public Texture2D cursorTexture;
	public CursorMode cursorMode = CursorMode.Auto;
	public Vector2 hotSpot = Vector2.zero;

	private float _startValue;
	private Vector2 _startPosition;
	private bool _dragging = false;
	private bool _pointerOver = false;

	public void SetValue(float val)
	{
		if(!AllowNegative)
			val = Mathf.Abs(val);
		InputField.text = val.ToString(CultureInfo.InvariantCulture);
	}

	private void Start()
	{
		if (!InputField.text.Any())
			InputField.text = 0.0f.ToString(CultureInfo.InvariantCulture);
		if(!AllowNegative)
			InputField.onValueChanged.AddListener(v=>InputField.text = Mathf.Abs(float.Parse(v)).ToString(CultureInfo.InvariantCulture));
	}

/*	public void SetValue(string val)
	{
		float parsed;
		if (float.TryParse(val, out parsed))
		{
			//SetValue(parsed);
			Debug.Log($"Property Value set to \"{parsed}\"");
		}
		else
		{
			Debug.Log($"Property Value set to \"{val}\", which is not a valid float.");
		}
	}*/

	public void OnBeginDrag(PointerEventData eventData)
	{
		_startPosition = eventData.position;
		_startValue = InputField.text.Length > 0 ? Convert.ToSingle(InputField.text) : 0;
		_dragging = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		var diff = eventData.position - _startPosition;
		SetValue(_startValue + diff.magnitude * Mathf.Max(Mathf.Abs(_startValue), 1) * Sensitivity * Mathf.Sign(Vector2.Dot(Vector2.right, diff)));
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		_dragging = false;
		if(!_pointerOver)
			Cursor.SetCursor(null, Vector2.zero, cursorMode);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
		_pointerOver = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if(!_dragging)
			Cursor.SetCursor(null, Vector2.zero, cursorMode);
		_pointerOver = false;
	}
}
