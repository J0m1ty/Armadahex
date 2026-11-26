using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// the turn clock: counts the current side's seconds and drains its bar on the hud
public class Countdown : MonoBehaviour
{
    // the seconds left this turn
    public int currentTime { get; private set; }

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

    void Awake() {
        resetTime = normalResetTime;

        isPaused = false;
        botTurn = false;
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

    private void Show(int time) {
        currentTime = time;
        var full = Mathf.Max(1, normalResetTime);
        MatchHud.instance?.SetClock((float)time / full, time, isUnlimited);
    }
    
    private IEnumerator CountdownCoroutine(float? overrideTime = null) {
        var time = (int)(overrideTime ?? resetTime);

        // the bot plays after a moment, its clock just stays full
        var bot = botTurn;
        if (bot) {
            time = singlePlayerAITime;
        }

        while (time > 0 || isUnlimited && !bot) {
            Show(time);

            yield return new WaitForSeconds(1);
            
            if (TurnManager.instance.gameActive && !isPaused && (!isUnlimited || bot)) {
                time--;
            }
        }

        if (bot) {
            botTurn = false;
            attackManager.RandomBotAttack();
            yield break;
        }

        Show(0);
        
        if (TurnManager.instance.currentTeam.isPlayer) {
            attackManager.RandomAttack();
        }
    }
}
