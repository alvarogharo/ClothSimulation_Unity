using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

	public float scale;
	public Vector3[] vertices;
	public int[] tetrahedrons;
	

	public Test(){
		scale = 1f;
	}

	void Start()
	{
		for (int i = 0; i<vertices.Length;i++){
			vertices[i] = vertices[i]*scale;  
			print(vertices[i]);
		}
	}

	/// <summary>
	/// Callback to draw gizmos that are pickable and always drawn.
	/// </summary>
	void OnDrawGizmos()
	{

		for (int i = 0; i < tetrahedrons.Length; i+=4){
			int[] tetrahedron = new int[4];

            for (int z = 0; z<4;z++){
                tetrahedron[z] = tetrahedrons[i+z]; 
				print(tetrahedron[z]);
            }

            Gizmos.DrawLine(vertices[tetrahedron[0]]*scale,vertices[tetrahedron[1]]*scale);
			Gizmos.DrawLine(vertices[tetrahedron[1]]*scale,vertices[tetrahedron[2]]*scale);
			Gizmos.DrawLine(vertices[tetrahedron[2]]*scale,vertices[tetrahedron[0]]*scale);
			Gizmos.DrawLine(vertices[tetrahedron[0]]*scale,vertices[tetrahedron[3]]*scale);
			Gizmos.DrawLine(vertices[tetrahedron[1]]*scale,vertices[tetrahedron[3]]*scale);
			Gizmos.DrawLine(vertices[tetrahedron[2]]*scale,vertices[tetrahedron[3]]*scale);
        }
	}
}
