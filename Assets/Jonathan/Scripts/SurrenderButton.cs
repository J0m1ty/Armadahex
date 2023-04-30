using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurrenderButton : MonoBehaviour
{
    public void Surrender() {
        GameOver.instance.Surrender();
    }
}