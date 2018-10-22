using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringDef : MonoBehaviour {


    public float Stiffness;
    public float damping;
    public float volume;
    public NodeDef nodeA;
    public NodeDef nodeB;

    public Deformation Manager;

    public float Length0;
    public float Length;
    public Vector3 Pos;
    public bool isFlexion;

    //Constructor
    public SpringDef(NodeDef a, NodeDef b){
        nodeA = a;
        nodeB = b;
        Length0 = Length = (nodeA.Pos - nodeB.Pos).magnitude;
        Pos = 0.5f * (nodeA.Pos + nodeB.Pos);
        isFlexion = false;
    }

    //Update all the forces related with the spring and
    //apply this forces to the nodes
    public void ComputeForces()
    {
        Vector3 dir = nodeA.Pos - nodeB.Pos;
        Length = dir.magnitude;
        dir = dir * (1.0f / Length);
        Vector3 Force = -(volume/Mathf.Pow(Length0,2))*Stiffness * (Length - Length0) * dir;
        Force += -(damping*Stiffness)*((Vector3.Dot(dir,(nodeA.Vel-nodeB.Vel)))*dir);
        nodeA.Force += Force;
        nodeB.Force -= Force;
    }

}
