using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTeleport : Target {
    
    public MeshFilter teleportTargetMesh;
    public Vector3 targetOffset;
    public bool teleportPadEnabled = false;
    public Material activatedTeleport;
    public Material deactivatedTeleport;
    public GameEvents targetSelectedEvent = GameEvents.NO_EVENT;
    public GameEvents targetInitEvent = GameEvents.NO_EVENT;
    public GameEvents teleportEvent = GameEvents.NO_EVENT;


    void Awake() {

        MeshRenderer teleportRenderer = GetComponentInChildren<MeshRenderer>();
        teleportRenderer.enabled= teleportPadEnabled;

    }


    void Start() {
        EventManager.StartListening (targetInitEvent.ToString(), init);
    }

        


    void init() {
        print ("now you can see the teleport target");
        teleportPadEnabled = true;
        MeshRenderer teleportMesh = GetComponentInChildren<MeshRenderer>();
        teleportMesh.enabled= true;
        teleportMesh.material = activatedTeleport;
    }


    public override void onSelect() {

        if (selected!=true && teleportPadEnabled == true) {
            selected = true;
            toggleActive();
            EventManager.TriggerEvent (targetSelectedEvent.ToString());
            teleport();
            GetComponentInChildren<MeshRenderer>().material = deactivatedTeleport;
            

        } 
    }

    void teleport() {
        //Vector3 teleportLocation = this.GetComponent<Transform>().position + targetOffset;
        //find the current position of the player

        //TO-DO pass player tag into teleport call
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.transform.position = this.transform.position + Vector3.up*2;
        print("teleported sss");
        //reassign position with coroutine for easing, animation

        //round completed 
        EventManager.TriggerEvent(teleportEvent.ToString());
    }


}
