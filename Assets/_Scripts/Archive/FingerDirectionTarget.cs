using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerDirectionTarget : MonoBehaviour {

  public AudioClip myClip;
  
  public string objectName;
  public void testMethod(){
  //SfxrSynth synth = new SfxrSynth();

  


  //synth.parameters.SetSettingsString("2,.1,.379,.311,.202,.736,.291,.252,.238,,,,,,,,.012,,,,,,,,,1,,.155,,,,");
  //synth.SetParentTransform(gameObject.transform);
  //print(gameObject.transform.position);
  //synth.Play();

  
  AudioSource.PlayClipAtPoint(myClip, gameObject.transform.position);


  }


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
