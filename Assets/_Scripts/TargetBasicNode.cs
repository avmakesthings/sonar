using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBasicNode : Target {


    public Material nodeTargetActiveMaterial;
    public Material nodeTargetMaterialNotActive;
    public MeshRenderer targetObject;


    void Start() {
        print("start");
        print(nodeTargetMaterialNotActive);
        targetObject = GetComponentInChildren<MeshRenderer>();
        targetObject.material = nodeTargetMaterialNotActive;
    }

    

    public override void onSelect() {
        toggleActive();
        targetObject.material = nodeTargetActiveMaterial;

    }

}
