using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentScript : MonoBehaviour {
	public List<GameObject> goalQueue;
	private GameObject currentGoal;
	private GoalScript goalscript;
	public Vector3 movingDirection;
	public float velocity;
	public Vector3 forceVector = new Vector3(0,0,0);
	private GameObject waitingSphere;

	public List<GameObject> collidingWalls;
	public List<GameObject> collidingAgents; 
	public List<GameObject> neighborAgents;
	public List<GameObject> neighborWalls; 
	public List<GameObject> neighborFallen;

	public Vector3 agentRepulsionForce;
	public Vector3 wallRepulsionForce;
	public Vector3 agentAvoidanceForce;
	public Vector3 wallAvoidanceForce;
	public Vector3 fallenAvoidanceForce;

	// params
	public int ID;
	private float radius;
	public bool crowded = false;
	public float stoppingRule = 1.0f;
	private float stoppingRuleCounter = 0.0f;
	public float waitingRule = 1.0f;
	private float waitingRuleCounter = 0.0f;

	// weights and stuff
	private float MAX_SPEED = 3.0f;
	private float MAX_ACCELERATION = 1.0f;
	private float QUERY_RADIUS = 3.0f;
	private float DENSITY_THRESHOLD = 5f; // the number of nearby agents that will make this agent reduce its query_radius
	private float PERSONAL_SPACE_THRESHOLD = 0.2f;
	private float ORIENTATION_WEIGHT = 2.4f;
	private float GOAL_ATTRACTION_WEIGHT = 5.0f;
	private float WALL_AVOIDANCE_WEIGHT = 1.0f;
	private float AGENT_AVOIDANCE_WEIGHT = 0.7f;
	private float TOTAL_REPULSION_WEIGHT = 1.0f;
	private float WALL_REPULSION_WEIGHT = 0.2f;
	private float AGENT_REPULSION_WEIGHT = 0.5f;
	private float STOPPINGRULE_TIME = 1.0f;
	private float STOPPINGRULE_THRESHOLD = -0.3f;
	private float WAITINGRULE_TIME = 0.5f;
	private float WAITINGRULE_THRESHOLD = 0.5f;
	private float FALLEN_FORCE_THRESHOLD;
	private float FALLEN_AGENT_AVOID_DISTANCE = 2f;



	// Use this for initialization
	void Start () {
		radius = GetComponent<CapsuleCollider> ().radius;
		collidingWalls = new List<GameObject> ();
		collidingAgents = new List<GameObject> ();
		waitingSphere = transform.Find("waitingSphere").gameObject;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		// make sure rigidbody isn't messing up results
		GetComponent<Rigidbody>().velocity = Vector3.zero;

		// get current goal or delete object
		if (currentGoal == null) {
			if (goalQueue.Count > 0) {
				currentGoal = GetNextGoal();
				goalscript = currentGoal.GetComponent<GoalScript> ();
			} else {
				//print ("agent " + ID + " reached all of its goals");
				Destroy (this.gameObject);
				return;
			}
		}

		// update the stoppingRule timer
		if (stoppingRule == 0.0f) {
			stoppingRuleCounter += Time.deltaTime;
		}
		if (stoppingRuleCounter >= STOPPINGRULE_TIME) {
			stoppingRule = 1.0f;
			stoppingRuleCounter = 0.0f;
		}

		// check to see if we need to apply waitingRule
		CheckingWaitingRule();

		// update the waitingRule timer
		if (waitingRule == 0.0f) {
			waitingRuleCounter += Time.deltaTime;
		}
		if (waitingRuleCounter >= WAITINGRULE_TIME) {
			waitingRule = 1.0f;
			waitingRuleCounter = 0.0f;
		}
			
		// update the force vector
		CalcForceVector ();
		Vector3 totalRepulsion = CalcTotalRepulsionForce ();

		// calculate speed and clamp it
		velocity = velocity + MAX_ACCELERATION * Time.deltaTime;
		velocity = Mathf.Clamp (velocity, 0, MAX_SPEED);

		// update position and moving direction based on stoppingRule and waitingRule
		Vector3 desiredMovement = stoppingRule * waitingRule * velocity * forceVector * Time.deltaTime 
			+ totalRepulsion * TOTAL_REPULSION_WEIGHT;
		movingDirection = desiredMovement.normalized;

		// clamp the speed
		Vector3 maxAllowedMovement = movingDirection * MAX_SPEED;
		if (desiredMovement.magnitude > maxAllowedMovement.magnitude) {
			desiredMovement = maxAllowedMovement;
		}
		transform.position += desiredMovement;

		/*walls = GetNeighborObstacles ();
		foreach (GameObject o in walls) {
			wallNorm = GetWallNormal (o);
		}*/
	}

	void OnTriggerEnter(Collider other) {
		if (other.gameObject == currentGoal) {
			//print ("agent " + ID + " reached its current goal" + goalscript.ID);
			currentGoal = null;
		}
	}

	void OnCollisionEnter(Collision other){
		if (other.gameObject.tag == "Agent") {
			collidingAgents.Add (other.gameObject);
		} else if (other.gameObject.tag == "Obstacle") {
			collidingWalls.Add (other.gameObject);
		}
	}

	void OnCollisionExit(Collision other){
		if (other.gameObject.tag == "Agent" && collidingAgents.Contains(other.gameObject)) {
			collidingAgents.Remove (other.gameObject);
		} else if (other.gameObject.tag == "Obstacle" && collidingWalls.Contains(other.gameObject)) {
			collidingWalls.Remove (other.gameObject);
		}
	}

	// returns the first goal in the list and shifts it
	private GameObject GetNextGoal(){
		GameObject target = goalQueue [0];
		goalQueue.RemoveAt (0);

		return target;
	}

	// calculate the force vector of the agent based on previous direction, goals and avoidance
	private void CalcForceVector(){
		// get the lists of neighbors
		List<GameObject> agentList = GetNeighborAgents ();
		// if it's crowded then reduce the search radius and try again
		if (agentList.Count > DENSITY_THRESHOLD) {
			crowded = true;
			agentList = GetNeighborAgents ();
		} else {
			crowded = false;
		}
		List<GameObject> obsList = GetNeighborObstacles (); // for now obs is the same as wall
		neighborAgents = agentList;
		neighborWalls = obsList;

		// calculate avoidance forces
		//Vector3 wallAvoidanceForce = new Vector3 (0, 0, 0);
		//Vector3 agentAvoidanceForce = new Vector3 (0, 0, 0);
		wallAvoidanceForce = new Vector3 (0, 0, 0);
		agentAvoidanceForce = new Vector3 (0, 0, 0);
		foreach (GameObject wall in obsList) {
			wallAvoidanceForce += CalculateWallAvoidanceForce (wall);
		}
		foreach (GameObject agent in agentList) {
			agentAvoidanceForce += CalculateAgentAvoidanceForce (agent);
		}

		// calculate attractor force aka speed towards goal
		Vector3 goalDir = (currentGoal.transform.position - transform.position).normalized;

		//calculate force vector
		forceVector = movingDirection
			+ goalDir * GOAL_ATTRACTION_WEIGHT
			+ wallAvoidanceForce * WALL_AVOIDANCE_WEIGHT
			+ agentAvoidanceForce * AGENT_AVOIDANCE_WEIGHT;
		forceVector = forceVector.normalized;
	}

	// calculate the total repulsion force exerted by walls and other agents
	// activate the stoppingRule if an agent is directly coming from the other direction
	private Vector3 CalcTotalRepulsionForce(){
		//Vector3 agentRepulsionForce = new Vector3 (0, 0, 0);
		agentRepulsionForce = new Vector3 (0, 0, 0);
		foreach (GameObject agent in collidingAgents) {
			if (agent != null){
				agentRepulsionForce += CalculateAgentRepulsionForce (agent);
			}
		}

		//Vector3 wallRepulsionForce = new Vector3(0,0,0);
		wallRepulsionForce = new Vector3(0,0,0);
		foreach (GameObject wall in collidingWalls){
			if (wall != null){
				wallRepulsionForce += CalculateWallRepulsionForce(wall);
			}
		}
		//if (float.IsNaN(wallRepulsionForce.x)){
		//	wallRepulsionForce = new Vector3(0,0,0);
		//}

		// activate stoppingRule if appropriate
		if (Vector3.Dot (movingDirection, agentRepulsionForce) < STOPPINGRULE_THRESHOLD && stoppingRule != 0.0f) {
			stoppingRule = 0.0f;
			if (ID == 15){
				//print("moving direction is " + movingDirection);
				//print("agent repulsion force is " + agentRepulsionForce);
			}
		}

		return agentRepulsionForce * AGENT_REPULSION_WEIGHT + wallRepulsionForce * WALL_REPULSION_WEIGHT;
	}

	// checks if we should apply waiting rule
	// also updates waiting rule's position
	private void CheckingWaitingRule(){
		WaitingSphereScript sphereScript = waitingSphere.GetComponent<WaitingSphereScript>();
		Vector3 goalDir = (currentGoal.transform.position - transform.position).normalized;
		if (goalDir != Vector3.zero){
			waitingSphere.transform.localPosition = goalDir;
		}
		float angle = Mathf.Rad2Deg * Mathf.Atan2(goalDir.z, goalDir.x);
		waitingSphere.transform.localEulerAngles = new Vector3(0, -angle, 90);

		foreach (GameObject agent in sphereScript.collidingAgents){
			if (agent != null
				&& Vector3.Dot(movingDirection, agent.GetComponent<AgentScript>().movingDirection) > WAITINGRULE_THRESHOLD 
				&& waitingRule != 0.0f){
				waitingRule = 0.0f;
				break;
			}
		}
	}


	// if crowd is dense, cull the list to only care about nearby agents
	private List<GameObject> GetNeighborAgents(){
		if (crowded) {
			return GetNeighborObjects ("Agent", QUERY_RADIUS/2);
		} else {
			return GetNeighborObjects ("Agent", QUERY_RADIUS);
		}
	}

	private List<GameObject> GetNeighborObstacles(){
		return GetNeighborObjects ("Obstacle", QUERY_RADIUS);
	}
		
	private List<GameObject> GetNeighborObjects(string tag, float queryRadius){
		GameObject[] allObj = GameObject.FindGameObjectsWithTag (tag);
		List<GameObject> neighborObj = new List<GameObject> ();

		foreach (GameObject obj in allObj) {
			// use distance squared to save runtime
			Vector3 vectorToObj = GetClosestPtOnObject(obj) - transform.position;
			float distanceSqrd = vectorToObj.sqrMagnitude;
			// also we only care about things in front of us
			if (distanceSqrd <= Mathf.Pow(queryRadius, 2)
				&& Vector3.Dot (movingDirection, vectorToObj) >= 0
				&& distanceSqrd > 0) {
				neighborObj.Add (obj);
			}
		}

		return neighborObj;
	}

	private Vector3 GetClosestPtOnObject (GameObject obj){
		//Vector3 direction = (obj.transform.position - transform.position).normalized;
		//Vector3 pointInDirection = transform.position + direction * radius;

		Collider coll;
		if (obj.tag == "Agent") {
			coll = obj.GetComponent<CapsuleCollider> ();
		} else {
			coll = obj.GetComponent<Collider> ();
		}
		return coll.ClosestPointOnBounds (transform.position);
	}

	// assumes the input obj is a wall, calculates the avoidance of that wall on this agent
	private Vector3 CalculateWallAvoidanceForce(GameObject wall){
		Vector3 wallNormal = GetWallNormal (wall);
		Vector3 avoidanceForce = Vector3.Cross (Vector3.Cross (wallNormal, movingDirection), wallNormal).normalized;

		return avoidanceForce;
	}

	// assumes the input obj is a wall, calculates the normal of that wall towards this agent
	private Vector3 GetWallNormal(GameObject wall){
		Vector3 wallNormal = new Vector3(0,0,0);
		Vector3 point = GetClosestPtOnObject (wall);
		Vector3 wallToAgent = transform.position - point;

		if (Mathf.Abs(wallToAgent.x) > Mathf.Abs(wallToAgent.z)) {
			wallNormal.x = wallToAgent.x;
		} else {
			wallNormal.z = wallToAgent.z;
		}

		wallNormal = wallNormal.normalized;
		return wallNormal;
	}

	// assumes the input obj is an agent, calculates the avoidance of that agent on this agent
	private Vector3 CalculateAgentAvoidanceForce(GameObject agent){
		Vector3 agentToSelf = transform.position - agent.transform.position;
		Vector3 tangentialForce = Vector3.Cross (Vector3.Cross (agentToSelf, movingDirection), agentToSelf).normalized;

		float queryRadius = QUERY_RADIUS;
		if (crowded) {
			queryRadius /= 2;
		}

		float distanceWeight = Mathf.Pow (agentToSelf.magnitude - queryRadius, 2);
		float orientationWeight = ORIENTATION_WEIGHT;
		if (Vector3.Dot (movingDirection, agent.GetComponent<AgentScript> ().movingDirection) > 0) {
			orientationWeight /= 2;
		}

		Vector3 avoidanceForce = tangentialForce * distanceWeight * orientationWeight;
		return avoidanceForce;
	}

	// assumes the input obj is an agent, calculates the repulsion of that agent on this agent
	private Vector3 CalculateAgentRepulsionForce(GameObject agent){
		float dist = Vector3.Distance (transform.position, agent.transform.position);

		Vector3 repulsionForce = (transform.position - agent.transform.position)
		                         * (radius * 2 + PERSONAL_SPACE_THRESHOLD - dist) / dist;
		return repulsionForce;
	}

	// assumes the input obj is an wall, calculates the repulsion of that wall on this agent
	private Vector3 CalculateWallRepulsionForce(GameObject wall){
		Vector3 wallNormal = GetWallNormal (wall);
		float dist = Vector3.Distance (transform.position, GetClosestPtOnObject (wall));

		Vector3 repulsionForce = wallNormal * (radius + PERSONAL_SPACE_THRESHOLD - dist) / dist;
		return repulsionForce;
	}
}
