using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBasicNode : Target {


    public Material ActiveMaterial;
    public Material InActiveMaterial;
    public MeshRenderer targetObject;
    public GameEvents targetSelectedEvent = GameEvents.NO_EVENT;
    public GameEvents targetInitEvent = GameEvents.NO_EVENT;


    void Start() {
        //print("start");
        //print(nodeTargetMaterialNotActive);
        targetObject = GetComponentInChildren<MeshRenderer>();
        targetObject.material = InActiveMaterial;

        EventManager.StartListening (targetInitEvent.ToString(), init);
    }

    

    public override void onSelect() {

        if (selected!=true) {
            selected = true;
            targetObject.material = ActiveMaterial;
            toggleActive();
            EventManager.TriggerEvent (targetSelectedEvent.ToString());
        } else {
        //TO-DO: add logic for deselection i.e. timer and hold finger position
        }
    }


    public void init() {
        //makes target appear
        print("round 1 target appears");
    }
}
