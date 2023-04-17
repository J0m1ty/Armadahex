using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonListener : MonoBehaviour, IPointerEnterHandler
{
    public int index;

    // on mouse over
    public void OnPointerEnter(PointerEventData eventData) {
        OnMouseEnter?.Invoke();
    }

    // mouse over event with delegate
    public delegate void OnMouseEnterEvent();
    public event OnMouseEnterEvent OnMouseEnter;

    // mouse leave
    public void OnPointerExit(PointerEventData eventData) {
        OnMouseExit?.Invoke();
    }

    // mouse leave event with delegate
    public delegate void OnMouseExitEvent();
    public event OnMouseExitEvent OnMouseExit;
}