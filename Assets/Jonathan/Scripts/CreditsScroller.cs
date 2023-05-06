using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class CreditsScroller : MonoBehaviour, IScrollHandler, IDragHandler
{
    private ScrollRect scrollView;
    
    [SerializeField]
    private bool doScrolling;

    [SerializeField]
    private float initialScrollWait = 1f;

    [SerializeField]
    private float interactionScrollWait = 5f;

    [SerializeField]
    private float scrollSpeed = 1f;

    [SerializeField]
    private float scrollWait;

    void Awake() {
        scrollView = GetComponent<ScrollRect>();

        scrollWait = initialScrollWait;
    }

    public void OnScroll(PointerEventData eventData) {
        scrollWait = interactionScrollWait;
    }

    public void OnDrag(PointerEventData eventData) {
        scrollWait = interactionScrollWait;
    }

    void Update() {
        if (!doScrolling) return;

        if (scrollWait > 0f) {
            scrollWait -= Time.deltaTime;
        } else {
            scrollView.verticalNormalizedPosition -= Time.deltaTime * scrollSpeed;
        }
    }
}
