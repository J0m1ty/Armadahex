using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class Countdown : MonoBehaviour
{
    private TMP_Text countdownText;

    public int currentTime => int.Parse(countdownText.text.Replace("s", ""));

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
    private int continueAddTime = 15;
    [SerializeField]
    private int continueAddTimeSalvo = 5;
    private int resetTime;

    [SerializeField]
    private AttackUIManager attackManager;

    private Color normalColor;
    [SerializeField]
    private Color lowColor;

    void Awake() {
        countdownText = GetComponent<TMP_Text>();

        resetTime = normalResetTime;

        isPaused = false;
        botTurn = false;

        normalColor = countdownText.color;
    }

    void Start() {
        normalResetTime = (int)GameModeInfo.instance.TurnTimeLimit;
        
        isUnlimited = normalResetTime <= 0 || GameModeInfo.instance.IsSingleplayer;
    }

    public void SetTurnTime(int turnTime) {
        normalResetTime = turnTime;
        resetTime = normalResetTime;

        isUnlimited = normalResetTime <= 0 || GameModeInfo.instance.IsSingleplayer;
    }
    
    // we need to have method which starts countdown
    // method that stops countdown
    // method that restarts countdown
    // and ienumerator that counts down every second

    /// <summary> Adds time and restarts the countdown. </summary>
    public void AddTime(int? setTimeBeforeAdd = null) {
        if (isUnlimited) {
            StartCountdown();
            return;
        }

        int time = setTimeBeforeAdd != null ? (int)setTimeBeforeAdd : currentTime;
        time += GameModeInfo.instance.IsSalvo? continueAddTimeSalvo : continueAddTime;
        if (time > resetTime) {
            time = resetTime;
        }

        StartCountdown(time);
    }

    public void StartCountdown(float? overrideTime = null) {
        if (!doCountdown) return;

        StopAllCoroutines();
        
        if (isUnlimited) {
            countdownText.text = "∞";
        }
        else {
            countdownText.text = resetTime.ToString() + "s";
            countdownText.color = normalColor;
        }

        StartCoroutine(CountdownCoroutine(overrideTime));

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
            time = 99999;
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
                    countdownText.color = time < 5 ? lowColor : normalColor;
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
