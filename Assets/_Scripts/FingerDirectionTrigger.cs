﻿using UnityEngine;
using System.Collections;
using Leap.Unity.Attributes;

namespace Leap.Unity {

    
  /**
   * Detects when specified fingers are pointing at all objects with a specified tag
   * 
   * Calls target object activate method when detected.
   */
  public class FingerDirectionTrigger : Detector {
    /**
     * The interval at which to check finger state.
     * @since 4.1.2
     */
    [Units("seconds")]
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    [MinValue(0)]
    public float Period = .1f; //seconds

    /**
     * The IHandModel instance to observe. 
     * Set automatically if not explicitly set in the editor.
     * @since 4.1.2
     */
    [AutoFind(AutoFindLocations.Parents)]
    [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
    public IHandModel HandModel = null;  

    /**
     * The finger to compare to the specified direction.
     * @since 4.1.2
     */
    [Tooltip("The finger to observe.")]
    public Finger.FingerType FingerName = Finger.FingerType.TYPE_INDEX;



    /**
     * The turn-on angle. The detector activates when the specified finger points within this
     * many degrees of the target direction.
     * @since 4.1.2
     */
    [Tooltip("The angle in degrees from the target direction at which to turn on.")]
    [Range(0, 180)]
    public float OnAngle = 15f; //degrees

    /**
    * The turn-off angle. The detector deactivates when the specified finger points more than this
    * many degrees away from the target direction. The off angle must be larger than the on angle.
    * @since 4.1.2
    */
    [Tooltip("The angle in degrees from the target direction at which to turn off.")]
    [Range(0, 180)]
    public float OffAngle = 25f; //degrees
    /** Whether to draw the detector's Gizmos for debugging. (Not every detector provides gizmos.)
     * @since 4.1.2 
     */
    [Header("")]
    [Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
    public bool ShowGizmos = true;

    public string tag;

    private IEnumerator watcherCoroutine;
    private IEnumerator targetFinderCoroutine;
    private GameObject[] taggedObjects;

    private void OnValidate(){
      if( OffAngle < OnAngle){
        OffAngle = OnAngle;
      }
    }

    private void Awake () {
      watcherCoroutine = fingerPointingWatcher();
      targetFinderCoroutine = FindTaggedGameObjects();
    }

    private void OnEnable () {
      StartCoroutine(watcherCoroutine);
      StartCoroutine(targetFinderCoroutine);
    }
  
    private void OnDisable () {
      StopCoroutine(watcherCoroutine);
      StopCoroutine(targetFinderCoroutine);
      Deactivate();
     

    }

    //A coroutine to find all objects tagged 'tag' in a scene
    private IEnumerator FindTaggedGameObjects()
        {
            while(true) { 
              taggedObjects = GameObject.FindGameObjectsWithTag(tag);
              yield return new WaitForSeconds(Period);
            }
            
        }


        
    //A function to run fingerpointing... for all tag objects



    private IEnumerator fingerPointingWatcher() {
      Hand hand;
      Vector3 fingerDirection;
      Vector3 targetDirection;
      int selectedFinger = selectedFingerOrdinal();
      while(true){
        if(HandModel != null && HandModel.IsTracked){
          hand = HandModel.GetLeapHand();
          if(hand != null){


            foreach (GameObject obj in taggedObjects){

              targetDirection = selectedDirection(hand.Fingers[selectedFinger].TipPosition.ToVector3(), obj);
              fingerDirection = hand.Fingers[selectedFinger].Bone(Bone.BoneType.TYPE_DISTAL).Direction.ToVector3();
              float angleTo = Vector3.Angle(fingerDirection, targetDirection);
              if(HandModel.IsTracked && angleTo <= OnAngle){
                //Activate();
                FingerDirectionTarget thisTarget = obj.GetComponent("FingerDirectionTarget") as FingerDirectionTarget; //Access scripts attached to target objects
                thisTarget.testMethod();
              } else if (!HandModel.IsTracked || angleTo >= OffAngle) {
                //Deactivate();
              }
            }


          }
        }
        yield return new WaitForSeconds(Period);
      }
    }

    private Vector3 selectedDirection(Vector3 tipPosition, GameObject obj ){
    //Pass in target object

        return obj.transform.position - tipPosition;

    }

    private int selectedFingerOrdinal(){
      switch(FingerName){
        case Finger.FingerType.TYPE_INDEX:
          return 1;
        case Finger.FingerType.TYPE_MIDDLE:
          return 2;
        case Finger.FingerType.TYPE_PINKY:
          return 4;
        case Finger.FingerType.TYPE_RING:
          return 3;
        case Finger.FingerType.TYPE_THUMB:
          return 0;
        default:
          return 1;
      }
    }

  #if UNITY_EDITOR
    private void OnDrawGizmos () {
      if (ShowGizmos && HandModel != null && HandModel.IsTracked) {
        Color innerColor;
        if (IsActive) {
          innerColor = OnColor;
        } else {
          innerColor = OffColor;
        }
        Finger finger = HandModel.GetLeapHand().Fingers[selectedFingerOrdinal()];
        Vector3 fingerDirection = finger.Bone(Bone.BoneType.TYPE_DISTAL).Direction.ToVector3();
        Utils.DrawCone(finger.TipPosition.ToVector3(), fingerDirection, OnAngle, finger.Length, innerColor);
        Utils.DrawCone(finger.TipPosition.ToVector3(), fingerDirection, OffAngle, finger.Length, LimitColor);
        Gizmos.color = DirectionColor;
        //Gizmos.DrawRay(finger.TipPosition.ToVector3(), selectedDirection(finger.TipPosition.ToVector3()));
      }
    }
  #endif
  }
}
