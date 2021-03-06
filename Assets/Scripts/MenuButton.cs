﻿using UnityEngine;
 using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
 using UnityEngine.UI;
 
 [RequireComponent (typeof(Button))]
 public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
 {
     private TextMeshProUGUI _txt;
     private Color _baseColor;
     private Button _btn;
     private bool _interactableDelay;

     private void Start ()
     {
         _txt = GetComponentInChildren<TextMeshProUGUI>();
         _baseColor = _txt.color;
         _btn = gameObject.GetComponent<Button> ();
         _interactableDelay = _btn.interactable;
         if (!_btn.interactable)
             _txt.color = DisabledColor;
     }
     
     public Color DisabledColor;// => _baseColor * _btn.colors.disabledColor * _btn.colors.colorMultiplier;

     private void Update ()
     {
         if (_btn.interactable != _interactableDelay) {
             if (_btn.interactable) {
                 _txt.color = _baseColor * _btn.colors.normalColor * _btn.colors.colorMultiplier;
             } else
             {
                 _txt.color = DisabledColor;
             }
         }
         _interactableDelay = _btn.interactable;
     }
 
     public void OnPointerEnter (PointerEventData eventData)
     {
         if (_btn.interactable) {
             _txt.color = _baseColor * _btn.colors.highlightedColor * _btn.colors.colorMultiplier;
         } else {
             _txt.color = DisabledColor;
         }
     }
 
     public void OnPointerDown (PointerEventData eventData)
     {
         if (_btn.interactable) {
             _txt.color = _baseColor * _btn.colors.pressedColor * _btn.colors.colorMultiplier;
         } else {
             _txt.color = DisabledColor;
         }
     }
 
     public void OnPointerUp (PointerEventData eventData)
     {
         if (_btn.interactable) {
             _txt.color = _baseColor * _btn.colors.highlightedColor * _btn.colors.colorMultiplier;
         } else {
             _txt.color = DisabledColor;
         }
     }
 
     public void OnPointerExit (PointerEventData eventData)
     {
         if (_btn.interactable) {
             _txt.color = _baseColor * _btn.colors.normalColor * _btn.colors.colorMultiplier;
         } else {
             _txt.color = DisabledColor;
         }
     }
 
 }