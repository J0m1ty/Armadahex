using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PregameUIManager : MonoBehaviour
{
    public bool showUI = true;
    public GameObject pregameUI;
    private Image pregameUIImage;
    private GameObject pregameUINotice;

    public Vector2 position;
    private bool doneWithOne;
    private bool doneWithTwo;
    public Vector2 gotoPosition;
    public Vector2 gotoPositionTwo;
    public float lerpAmount;
    public float lerpAmountTwo;
    public float lerpAlphaAmount;

    private float setAlpha;

    private bool show;

    public int delayTime;
    public int delayTimeTwo;
    [SerializeField]
    private float timeCounter;

    void Start() {
        pregameUINotice = pregameUI.transform.GetChild(0).gameObject;
        pregameUINotice.transform.position = position;
        gotoPosition.y = position.y;
        gotoPositionTwo.y = position.y;
        pregameUIImage = pregameUI.GetComponent<Image>();
        
        show = true;
        doneWithOne = false;
        doneWithTwo = false;
        setAlpha = 0;
        timeCounter = 0;

        if (!showUI) {
            pregameUI.SetActive(false);
        }
        else {
            pregameUI.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // first lerp to gotoPosition
        // then lerp to gotoPositionTwo
        // then lerp alpha to 0
        // then set show to false

        if (show && showUI) {
            if (!doneWithOne) {
                // lerp
                position = Vector2.Lerp(position, gotoPosition, Time.deltaTime * lerpAmount);

                // if close enough to target position, set position to target position
                if (Vector2.Distance(position, gotoPosition) < 0.1f) {
                    position = gotoPosition;

                    timeCounter += Time.deltaTime * 1000f;

                    if (timeCounter >= delayTime) {
                        doneWithOne = true;
                        timeCounter = 0;
                    }
                }
            }

            if (doneWithOne && !doneWithTwo) {
                // lerp
                position = Vector2.Lerp(position, gotoPositionTwo, Time.deltaTime * lerpAmountTwo);

                // if close enough to target position, set position to target position
                if (Vector2.Distance(position, gotoPositionTwo) < 0.1f) {
                    position = gotoPositionTwo;

                    timeCounter += Time.deltaTime * 1000f;

                    if (timeCounter >= delayTimeTwo) {
                        doneWithTwo = true;
                        timeCounter = 0;
                    }
                }
            }

            if (doneWithOne && doneWithTwo) {
                // lerp alpha
                var alpha = pregameUIImage.color.a;
                alpha = Mathf.Lerp(alpha, setAlpha, Time.deltaTime * lerpAlphaAmount);
                
                // if close enough to target alpha, set alpha to target alpha
                if (Mathf.Abs(alpha - setAlpha) < 0.01f) {
                    alpha = 0;
                    show = false;
                }

                // set alpha of notice
                pregameUIImage.color = new Color(pregameUIImage.color.r, pregameUIImage.color.g, pregameUIImage.color.b, alpha);
            }

            pregameUINotice.transform.localPosition = position;
        }
    }
}
