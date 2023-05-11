using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonClickerRef : MonoBehaviour
{
    public void PlayClick() {
        ButtonClick.instance?.PlayClick();
    }

    public void PlayClickLight() {
        ButtonClick.instance?.PlayClickLight();
    }

    public void PlayClickHeavy() {
        ButtonClick.instance?.PlayClickHeavy();
    }
}
