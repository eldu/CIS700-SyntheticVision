using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageScript : MonoBehaviour {
	public List<GameObject> agents;
	public float timeCounter = 0f;
	public bool done = false;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		agents = new List<GameObject>(GameObject.FindGameObjectsWithTag ("Agent"));

		if (!done) {
			if (agents.Count > 0) {
				timeCounter += Time.deltaTime;
			} else {
				print ("all agents exited in " + timeCounter + " seconds");
				done = true;
			}
		}
	}
}
