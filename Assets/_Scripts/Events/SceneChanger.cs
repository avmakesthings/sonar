using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour {
    public void LoadStart() {
        print("starting");
        new WaitForSeconds(2);

        SceneManager.LoadScene (1);
    }

    public void EndGame() {
        print("quitting");
        //new WaitForSeconds(2);
        Application.Quit();
        UnityEditor.EditorApplication.isPlaying = false;
    }
    

}
