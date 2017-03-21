using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class Target : MonoBehaviour {

    
    protected bool active;
    protected bool selected;
    //public GameEvents GameEvent;
    
    public bool targetEnabled = false;
    //toggles between active state
    public void toggleActive() {
        active = !active;
    }


    // initializes target -triggered by correct selection sequence from player 
    public virtual void initializeTarget() {
        if(active) {
            //show target
        }
    }


    //reveals target objects - triggered by scan
    public virtual void onScan() {
        //print("onScan");
        toggleActive();
        //do something
        toggleActive();
    }

    //select target objects
    public virtual void onSelect() {
        if (selected!=true) {
            selected = true;
            toggleActive();
        }else {
            selected= false;
            toggleActive();
        }
        
    }

    //toggles the material
    void toggleMaterial(Material newMaterial) {
        Material currentMaterial = this.GetComponent<Material>();
        currentMaterial = newMaterial;

    }

    //Helper function to get the material from a mesh object
	  Material getMaterialFromObject(MeshFilter mfilter) {
        GameObject targetMesh = mfilter.gameObject;
        MeshRenderer renderer =  targetMesh.GetComponent<MeshRenderer>();
        return renderer.material;
	  }


    //Helper function to set a material
    void setMaterialOfObject(Material nextMaterial, MeshFilter mfilter ) {
        GameObject targetMesh = mfilter.gameObject;
        MeshRenderer renderer =  targetMesh.GetComponent<MeshRenderer>();
        renderer.material = nextMaterial;
    }


    //Helper function to test whether input is being detected - called by FingerDirectionTrigger
    public virtual void testMethod() {
        //print("test");
    }



  }
