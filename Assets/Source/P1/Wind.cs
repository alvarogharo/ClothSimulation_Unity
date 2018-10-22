using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wind : MonoBehaviour {

	public Vector3 dir;
	public float force;

	//Determines the direction of the wind
	void Start () {
		dir = (transform.GetChild(0).position - transform.position).normalized;
	}

	//Updates the dir and the force of the wind
	void Update()
	{
		dir = (transform.GetChild(0).position - transform.position).normalized;
	}
}
