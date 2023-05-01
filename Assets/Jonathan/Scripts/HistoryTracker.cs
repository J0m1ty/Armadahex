using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HistoryTracker : MonoBehaviour
{
    public static HistoryTracker instance = null;

    public List<string> sceneHistory = new List<string>();
    
    public List<MyBox.SceneReference> IgnoreScenes;

    [MyBox.Scene]
    public string defaultScene;

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
            
            var newScene = sceneHistory[sceneHistory.Count - 1];
            
            if (IgnoreScenes.Exists(s => s.SceneName == newScene)) {
                newScene = defaultScene;
            }

            SceneManager.LoadScene(newScene);
        }
    }
}
