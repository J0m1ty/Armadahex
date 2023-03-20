using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HistoryTracker : MonoBehaviour
{
    public static HistoryTracker instance = null;

    public List<string> sceneHistory = new List<string>();

    void Awake() {
        if (!instance) {
            instance = this;

            SceneManager.sceneLoaded += OnSceneLoaded;

        } else if (instance != this) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        sceneHistory.Add(scene.name);
    }

    public void GoBack() {
        if (sceneHistory.Count > 1) {
            sceneHistory.RemoveAt(sceneHistory.Count - 1);
            SceneManager.LoadScene(sceneHistory[sceneHistory.Count - 1]);
        }
    }
}
