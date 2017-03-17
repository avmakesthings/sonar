using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TriggerSoundsAtIntervals : MonoBehaviour {

	public List<AudioSource> sounds = new List<AudioSource>();


	void Start(){
	
		StartCoroutine ("PlaySounds");
	
	}

	private IEnumerator PlaySounds()
	{
		int soundIndex = 0;
		int soundCount = sounds.Count;

		while(soundCount>0) ///size greated than 0
		{
			sounds [soundIndex].Play ();
			if (soundIndex == soundCount - 1) {
				soundIndex = 0;
			} else {
				soundIndex++;
			}

			yield return new WaitForSeconds(5f); // wait 5 seconds
			


		}
	}




}
