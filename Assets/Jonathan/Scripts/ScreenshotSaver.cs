using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ScreenshotCamera {
    public GameObject obj;
    public Camera camera {
        get {
            return obj.GetComponent<Camera>();
        }
    }
    public RenderTexture renderTexture {
        get {
            return camera.targetTexture;
        }
        set {
            camera.targetTexture = value;
        }
    }
}

public class ScreenshotSaver : MonoBehaviour {
    public TeamManager teamManager;
    public MeshRenderer water;
    
    public void TakeScreenshots() {
        for (var i = 0; i < teamManager.teams.Count; i++) {
            var team = teamManager.teams[i];
            Debug.Log("Taking screenshot for " + team.teamType);
            StartCoroutine(TakePicture(team.teamBase, i, i == teamManager.teams.Count - 1));
        }
    }

    IEnumerator TakePicture(TeamBase teamBase, int i = 0, bool last = false) {
        for (int j = 0; j < i; j++) {
            yield return new WaitForEndOfFrame();
        }
        
        if (i == 0) {
            RenderSettings.fog = false;
        }

        var oldcolor = water.material.GetColor("_Base_color");
        water.material.SetColor("_Base_color", new Color(0f, 0f, 0f, 1f));

        var oldpos = teamBase.hexMap.transform.position;
        teamBase.hexMap.transform.position = new Vector3(teamBase.hexMap.transform.position.x, 25f, teamBase.hexMap.transform.position.z);
        teamBase.screenshotCamera.obj.SetActive(true);

        yield return new WaitForEndOfFrame();
        
        var tex = ToTexture2D(teamBase.screenshotCamera.renderTexture);
        // var bytes = tex.EncodeToPNG();

        // var path = Application.dataPath + "/../Screenshots/" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "-" + (teamBase.team.isPlayer ? "player" : "enemy") + ".png";
        // System.IO.File.WriteAllBytes(path, bytes);

        if (teamBase.team.isPlayer) {
            GameOver.instance.playerImage = tex;
        } else {
            GameOver.instance.enemyImage = tex;
        }
        
        teamBase.screenshotCamera.obj.SetActive(false);
        teamBase.hexMap.transform.position = oldpos;

        water.material.SetColor("_Base_color", oldcolor);

        if (last) {
            RenderSettings.fog = true;

            GameOver.instance.ScreenshotsTaken();
        }
    }

    public static Texture2D ToTexture2D(RenderTexture rt) {
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        return tex;
    }
}
