using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FPSScript : MonoBehaviour {
	private Text text;

	// Use this for initialization
	void Start () {
		text = GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		float fps = 1.0f / Time.deltaTime;
		text.text = fps + " FPS";
	}
}
