using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public class AnimateGifTextures : MonoBehaviour {

	public string animationFolder;
	protected string pathToSequences = "_Textures/Sequences/";
	public float duration = 0.5f;

	protected Texture[] gifTextures;

	void Start( )
	{
		string animationPath = pathToSequences + animationFolder;
		string fullPath = Application.dataPath.ToString()+"/" + animationPath;
		DirectoryInfo dir = new DirectoryInfo(fullPath);
		FileInfo[] gifFrame = dir.GetFiles("*.png");

		gifTextures = new Texture[gifFrame.Length];



		for (int i = 0; i < gifFrame.Length; i++) {
			string frameName = gifFrame[i].Name.Substring(0, gifFrame[i].Name.Length - 4);
			string framePath = animationPath+"/"+frameName;

			print (framePath);

//			Texture frameTexture = Resources.Load (framePath) as Texture;
//			Texture frameTexture = Resources.Load<Texture>(framePath);
//			Texture frameTexture = Instantiate(Resources.Load(framePath)) as Texture;
			byte[] fileData = File.ReadAllBytes(fullPath+"/"+gifFrame[i].Name);
			Texture2D frameTexture = new Texture2D(2, 2);
			frameTexture.LoadImage(fileData);


			print (frameTexture);
			gifTextures[i] = frameTexture;
		}
			
		StartCoroutine(DoTextureLoop());
	}

	public IEnumerator DoTextureLoop(){
		int i = 0;
		print (gifTextures.Length);

		while (true){
//			print (gifTextures [i]);
//			renderer.material.mainTexture = gifTextures[i];
			GetComponent<Renderer>().material.SetTexture("_MainTex", gifTextures[i]);
			i = (i+1)%gifTextures.Length;
			yield return new WaitForSeconds(duration);
		}
	}

}
