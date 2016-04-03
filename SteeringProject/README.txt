Unity Steering Framework by Yichen Shou

Walls/Obstacles
- items with the tag "Obstacle"
- feel free to scale/rotate them all you like

Goals
- markers with a trigger that will tell you when an agent has entered

Agents
- has a goal queue of goals that it will try to accomplish in order.
	- drag Goal prefab objects into it 
	- if goal queue is null then agent will self-destruct
- has a bunch of parameters that you can set yourself
- you can add forces to it using rigidbodies.addForce.
	- velocity is automatically clamped to max_speed
- use GetClosestPtOnObject to get the closest point to a given object's mesh/collider
- use getNeighborAgents/Obstacles to get agents/obstacles within the QUERY_RADIUS
	QUERY_RADIUS starts at center of agent
	

To Implement Your Steering Algorithm
- just add functions to AgentScript