using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class Countdown : MonoBehaviour
{
    private TMP_Text countdownText;

    [SerializeField]
    private bool doCountdown = true;
    public bool isPaused;
    private bool botTurn;
    public bool isUnlimited;
    
    [SerializeField]
    private int singlePlayerAITime = 5;
    [SerializeField]
    private int normalResetTime = 25;
    [SerializeField]
    private int firstTurnBonusTime = 5;
    [SerializeField]
    private int continueAddTime = 15;
    private int resetTime;

    [SerializeField]
    private AttackUIManager attackManager;

    void Awake() {
        countdownText = GetComponent<TMP_Text>();

        resetTime = normalResetTime + firstTurnBonusTime;

        isPaused = false;
        botTurn = false;
    }

    void Start() {
        normalResetTime = (int)GameModeInfo.instance.TurnTimeLimit;
        
        isUnlimited = normalResetTime <= 0 || GameModeInfo.instance.IsSingleplayer;
    }
    
    // we need to have method which starts countdown
    // method that stops countdown
    // method that restarts countdown
    // and ienumerator that counts down every second

    /// <summary> Adds time and restarts the countdown. </summary>
    public void AddTime() {
        if (isUnlimited) {
            StartCountdown();
            return;
        }

        var currentResetTime = resetTime;
        var currentTime = int.Parse(countdownText.text.Replace("s", ""));
        currentTime += continueAddTime;
        if (currentTime > resetTime) {
            currentTime = resetTime;
        }
        resetTime = currentTime;
        StartCountdown();
        resetTime = currentResetTime;
    }

    public void StartCountdown() {
        if (!doCountdown) return;

        StopAllCoroutines();
        
        if (isUnlimited) {
            countdownText.text = "∞";
        }
        else {
            countdownText.text = resetTime.ToString() + "s";
            countdownText.color = Color.black;
        }

        StartCoroutine(CountdownCoroutine());

        resetTime = normalResetTime;
    }

    public void StartCountdownForBot() {
        botTurn = true;
        StartCountdown();
    }

    public void StopCountdown() {
        if (!doCountdown) return;

        StopAllCoroutines();
    }
    
    private IEnumerator CountdownCoroutine(float? overrideTime = null, bool doColor = true) {
        var time = overrideTime ?? resetTime;
        
        if (isUnlimited) {
            time = 9999;
        }

        // just for display... will only go down by singlePlayerAITime if it's the bot's turn
        if (botTurn) {
            time = 10;
        }

        var initialTime = time;

        while (time > 0) {
            var timeElapsed = initialTime - time;

            if (botTurn && timeElapsed >= singlePlayerAITime) {
                botTurn = false;
                attackManager.RandomBotAttack();
                yield break;
            }

            if (isUnlimited && !botTurn) {
                countdownText.text = "∞";
            }
            else {
                countdownText.text = time.ToString() + "s";
                if (doColor) {
                    countdownText.color = time < 5 ? Color.Lerp(Color.red, Color.black, 0.5f) : Color.black;
                }
            }

            yield return new WaitForSeconds(1);
            
            if (TurnManager.instance.gameActive && !isPaused && (!isUnlimited || botTurn)) {
                time--;
            }
        }
        
        countdownText.text = "0s";
        
        if (TurnManager.instance.currentTeam.isPlayer) {
            attackManager.RandomAttack();
        }
    }
}
