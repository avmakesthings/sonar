
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;
using Leap.Unity;

public class TeleportSelector : MonoBehaviour {

    HandModel hand_model;

    

    Hand leap_hand;
    private LeapProvider provider = null;
    
    

    // Use this for initialization
    void Start () {
        hand_model = GetComponent<HandModel>();
        print(hand_model);
        provider = GetComponent<LeapServiceProvider>();
        leap_hand = hand_model.GetLeapHand();
        print(leap_hand);
        if (leap_hand == null) Debug.LogError("Error: No leap_hand found");
    }
	
	// Update is called once per frame
	void Update () {
        //FingerModel finger = hand_model.fingers[1];
        //Debug.DrawRay (finger.GetTipPosition(), finger.GetRay().direction, Color.red);
    }
}
