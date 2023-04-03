using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurnUIManager : MonoBehaviour
{
    public GameObject turnUI;
    private TMP_Text turnUIText;
    
    public float alphaFadeAmount;
    public float delay = 1f;
    public float delayTimer;

    public string playerTurnText = "Your Turn";
    public string enemyTurnText = "Enemy's Turn";

    void Start() {
        TurnManager.instance.OnTurnChange += ChangeTurn;

        turnUIText = turnUI.GetComponentInChildren<TMP_Text>();

        turnUIText.alpha = 0f;
        turnUI.SetActive(false);
    }

    public void ChangeTurn(Team team) {
        if (team.isPlayer) {
            turnUIText.text = playerTurnText;
        } else {
            turnUIText.text = enemyTurnText;
        }

        turnUIText.alpha = 1f;
        turnUI.SetActive(true);
    }

    void Update() {
        if (turnUI.activeSelf) {
            delayTimer += Time.deltaTime;

            if (delayTimer >= delay) {
                turnUIText.alpha -= alphaFadeAmount * Time.deltaTime;

                if (turnUIText.alpha <= 0f) {
                    turnUI.SetActive(false);
                    delayTimer = 0f;
                }
            }
        }
    }
}
