using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTeleport : Target {
    
    public MeshFilter teleportTargetMesh;
    public Vector3 targetOffset;
    public GameEvents targetSelectedEvent = GameEvents.NO_EVENT;
    public GameEvents targetInitEvent = GameEvents.NO_EVENT;
    public GameEvents teleportEvent = GameEvents.NO_EVENT;



    void Start() {
        EventManager.StartListening (targetInitEvent.ToString(), init);
    }

        


    void init() {
        print ("now you can see the teleport target");
    }


    public override void onSelect() {

        if (selected!=true) {
            selected = true;
            toggleActive();
            EventManager.TriggerEvent (targetSelectedEvent.ToString());
            teleport();
        } 
    }

    void teleport() {
        //Vector3 teleportLocation = this.GetComponent<Transform>().position + targetOffset;
        //find the current position of the player

        //reassign position with coroutine for easing, animation

        //round completed 
        EventManager.TriggerEvent(teleportEvent.ToString());
    }


}
