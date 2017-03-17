using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTeleport : Target {
    
    public MeshFilter teleportTargetMesh;
    public Vector3 targetOffset;


    void teleport(Transform playerLocation) {
        Vector3 teleportLocation = this.GetComponent<Transform>().position + targetOffset;
        //find the current position of the player

        //reassign position with coroutine for easing, animation

    }


        public override void onScan() {
        print("scanning");
        toggleActive();
        //do something
        toggleActive();
    }


        void OnEnable()
        {

        }

        void activateTelportTarget() {
        print ("you're about to teleport");
    }

}
