using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBasicNode : Target {


    public Material ActiveMaterial;
    public Material InActiveMaterial;
    [HideInInspector] public MeshRenderer targetObject;
    public GameEvents targetSelectedEvent = GameEvents.NO_EVENT;
    public GameEvents targetInitEvent = GameEvents.NO_EVENT;
    public AudioClip targetSound;



    void Awake() {
        targetObject = GetComponentInChildren<MeshRenderer>();
        targetObject.material = InActiveMaterial;
        MeshRenderer teleportRenderer = GetComponentInChildren<MeshRenderer>();
        teleportRenderer.enabled= targetEnabled;

    }


    void Start() {
        //print("start");
        //print(nodeTargetMaterialNotActive);

        EventManager.StartListening (targetInitEvent.ToString(), init);
    }


    public override void onScan() {
        if(!active  && targetEnabled == true) {
            StartCoroutine("Scan");
        }
    }



    public override void onSelect() {

        if (selected!=true  && targetEnabled == true) {
            selected = true;
            toggleActive();
            
            EventManager.TriggerEvent (targetSelectedEvent.ToString());
        } else {
        //TO-DO: add logic for deselection i.e. timer and hold finger position
            // selected = !true;
            // targetObject.material = InActiveMaterial;
            // toggleActive();
        }
    }

    public override void toggleActive() {
        //check if target is active
        if (active == true) {
            //change material back to inactive
            targetObject.material = InActiveMaterial;
        }else {
            //play target sound
            AudioSource.PlayClipAtPoint(targetSound,GetComponent<Transform>().position);
            //change target material to active
            targetObject.material = ActiveMaterial;
        }
        //toggle active state
        active = !active;
    }


    IEnumerator Scan() {
        toggleActive();
        yield return new WaitForSeconds(3);
        toggleActive();
    }


    public void init() {
        //makes target appear
        print("round 1 target appears");
        targetEnabled = true;
        GetComponentInChildren<MeshRenderer>().enabled= true;
    }
}
