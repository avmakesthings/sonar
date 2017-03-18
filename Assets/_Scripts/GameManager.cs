using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {


    protected int targetSelectCounter = 0 ;
    protected GameEvents roundAllSelectedEvent = GameEvents.ROUND_0_ALL_SELECTED;



    void OnEnable ()
    {

        EventManager.StartListening (GameEvents.ROUND_0_TARGET_SELECTED.ToString(),targetSelectHandler);
        EventManager.StartListening (GameEvents.ROUND_0_COMPLETED.ToString(),onRound0Completed);
        EventManager.StartListening (GameEvents.ROUND_1_TARGET_SELECTED.ToString(),targetSelectHandler);
        EventManager.StartListening (GameEvents.ROUND_1_COMPLETED.ToString(),onRound1Completed);
    }

    void OnDisable ()
    {
        EventManager.StopListening (GameEvents.ROUND_0_TARGET_SELECTED.ToString(),targetSelectHandler);
        EventManager.StopListening (GameEvents.ROUND_0_COMPLETED.ToString(),onRound0Completed);
        EventManager.StartListening (GameEvents.ROUND_1_TARGET_SELECTED.ToString(),targetSelectHandler);
        EventManager.StopListening (GameEvents.ROUND_1_COMPLETED.ToString(),onRound1Completed);
    }


    // TARGET LOGIC
    // increments counter of selected targets and broadcasts all selected when specified limit is reached
    void targetSelectHandler() {
        targetSelectCounter++;
        if(targetSelectCounter==3) {
            EventManager.TriggerEvent (roundAllSelectedEvent.ToString());
        }
    }

    //ROUND LOGIC
    //generates / shows whatever...next round of targets based on completed event
    void onRound0Completed() {
        targetSelectCounter = 0;
        roundAllSelectedEvent = GameEvents.ROUND_1_ALL_SELECTED; 
    }

    void onRound1Completed() {
        SceneManager.LoadScene("Scene-00");
    }



}
