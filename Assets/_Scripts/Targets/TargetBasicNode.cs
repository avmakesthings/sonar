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
        AudioSource.PlayClipAtPoint(targetSound,GetComponent<Transform>().position);
    }



    public override void onSelect() {

        if (selected!=true  && targetEnabled == true) {
            selected = true;
            targetObject.material = ActiveMaterial;
            toggleActive();
            
            EventManager.TriggerEvent (targetSelectedEvent.ToString());
        } else {
        //TO-DO: add logic for deselection i.e. timer and hold finger position
            // selected = !true;
            // targetObject.material = InActiveMaterial;
            // toggleActive();
        }
    }


    public void init() {
        //makes target appear
        print("round 1 target appears");
        targetEnabled = true;
        GetComponentInChildren<MeshRenderer>().enabled= true;
    }
}
