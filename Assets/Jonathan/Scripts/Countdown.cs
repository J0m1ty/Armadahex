using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class Countdown : MonoBehaviour
{
    private TMP_Text countdownText;
    
    [SerializeField]
    private int firstTurnTime = 30;
    [SerializeField]
    private int normalResetTime = 25;
    private int resetTime;

    [SerializeField]
    private AttackUIManager attackManager;

    void Awake() {
        countdownText = GetComponent<TMP_Text>();

        resetTime = firstTurnTime;
    }
    
    // we need to have method which starts countdown
    // method that stops countdown
    // method that restarts countdown
    // and ienumerator that counts down every second

    public void StartCountdown() {
        StopAllCoroutines();

        countdownText.text = resetTime.ToString() + "s";
        countdownText.color = Color.black;
        StartCoroutine(CountdownCoroutine());
        resetTime = normalResetTime;
    }

    public void StopCountdown() {
        StopAllCoroutines();
    }
    
    private IEnumerator CountdownCoroutine() {
        var time = resetTime;
        while (time > 0) {
            countdownText.text = time.ToString() + "s";
            countdownText.color = time < 5 ? Color.Lerp(Color.red, Color.black, 0.5f) : Color.black;
            yield return new WaitForSeconds(1);
            time--;
        }
        countdownText.text = "0s";
        
        if (TurnManager.instance.currentTeam.isPlayer) {
            attackManager.RandomAttack();
        }
    }
}
