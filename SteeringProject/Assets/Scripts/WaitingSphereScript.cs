using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaitingSphereScript : MonoBehaviour {
	public List<GameObject> collidingAgents; 

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider other){
		if (other.gameObject.tag == "Agent") {
			collidingAgents.Add (other.gameObject);
		}
	}

	void OnTriggerExit(Collider other){
		if (other.gameObject.tag == "Agent" && collidingAgents.Contains(other.gameObject)) {
			collidingAgents.Remove (other.gameObject);
		}
	}
}
