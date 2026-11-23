using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreditsScroller : MonoBehaviour
{
    [SerializeField]
    private UIDocument document;

    [SerializeField]
    private string element = "credits";

    private ScrollView scrollView;
    
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

    void Start() {
        scrollView = document.rootVisualElement.Q<ScrollView>(element);

        // scrolling or dragging by hand pauses the roll
        scrollView.RegisterCallback<WheelEvent>(_ => scrollWait = interactionScrollWait, TrickleDown.TrickleDown);
        scrollView.RegisterCallback<PointerDownEvent>(_ => scrollWait = interactionScrollWait, TrickleDown.TrickleDown);

        scrollWait = initialScrollWait;
    }

    void Update() {
        if (!doScrolling || scrollView == null) return;

        if (scrollWait > 0f) {
            scrollWait -= Time.deltaTime;
        } else {
            // scrollSpeed is the share of the whole list per second, as with the old normalized position
            var range = scrollView.contentContainer.layout.height - scrollView.contentViewport.layout.height;
            if (range <= 0 || float.IsNaN(range)) return;
            var offset = scrollView.scrollOffset;
            offset.y = Mathf.Min(range, offset.y + Time.deltaTime * scrollSpeed * range);
            scrollView.scrollOffset = offset;
        }
    }
}
