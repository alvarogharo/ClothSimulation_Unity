using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeDef: MonoBehaviour{

    public int index;
    public float Mass;
    public float Damping;
    public bool Fixed;
    public Vector3 normal;
    public float surface;
    public float windReaction;
    public float collisionConstant;

    public Deformation Manager;

    public Vector3 Pos;
    public Vector3 Vel;
    public Vector3 Force;
    

    //Constructor
    public NodeDef(Vector3 p, int ind, float windR){
        Pos =  p;
        index = ind;
        windReaction = windR;
    }

	// Update is called once per frame
	public void Update () {

        Manager.vertexs[index] = Pos;
	}

    //Update all the forces applied to the node
    //Gravity, wind and collisions
    public void ComputeForces (Wind[] winds,GameObject[] collidables)
    {   
        //Gravity
        Force += Mass * Manager.Gravity;
        Force += -Damping * Vel;

        //Winds
        for (int i = 0; i<winds.Length;i++){
            Wind wind = winds[i];
            if (checkInsideCollider(wind.gameObject.GetComponent<SphereCollider>())){
                Force += ((wind.force*(Vector3.Dot(wind.dir,normal)))*normal)*windReaction;
            }
        }

        //Collisions
        for (int i = 0; i<collidables.Length;i++){
            GameObject coll = collidables[i];
            if (checkInsideCollider(coll.GetComponent<SphereCollider>())){
                float radiusWithOffset = coll.GetComponent<SphereCollider>().radius;
                Vector3 center = coll.transform.position;

                float distanceToCenter = (Pos-center).magnitude;
                
                float d = radiusWithOffset-distanceToCenter;
                Force += collisionConstant*d*(Pos-center).normalized;
            }
        }
    }

    //Determines wether this node is fixed or not
    //It is fixed if it is contained in a fixer Object
    public void isFixed(){

        Collider[] aux = Manager.coll;

        for (int i = 0; i<aux.Length;i++){
            if(checkInsideCollider(aux[i])){
                Fixed = true;
            }
        }
    }

    //Return true if the node is inside the collider coll
    private bool checkInsideCollider(Collider coll){
        return coll.bounds.Contains(Pos);
    }
}
