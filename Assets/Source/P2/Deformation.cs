using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic physics manager capable of simulating a given ISimulable
/// implementation using diverse integration methods: explicit,
/// implicit, Verlet and semi-implicit.
/// </summary>
public class Deformation : MonoBehaviour 
{
	/// <summary>
	/// Default constructor. Zero all. 
	/// </summary>
	public Deformation()
	{
		Paused = false;
		TimeStep = 0.01f;
		Gravity = new Vector3 (0.0f, -9.81f, 0.0f);
		IntegrationMethod = Integration.Explicit;
	}

	/// <summary>
	/// Integration method.
	/// </summary>
	public enum Integration
	{
		Explicit = 0,
		Symplectic = 1,
	};

	#region InEditorVariables

	public bool Paused;
   
    public float tetrahedronDensity;
    public float stiffnes;
    public float flexionStiffnes;
    public float nodeDamping;
    public float springDamping;
    public float windReaction;
    public float collisionConstant;
    public float nodeMass;
	public float TimeStep;
    public Vector3 Gravity;
	public Integration IntegrationMethod;
    public List<NodeDef> nodes;
    public List<SpringDef> springs;

    #endregion

    #region OtherVariables

    private Mesh mesh;

    public Vector3[] vertexs;
    public Vector3[] normals;
    public int[] tetrahedrons;
    public float[] volumes;

    public Collider[] coll;
    public Wind[] winds;
    public GameObject[] collidables;



    #endregion

    #region MonoBehaviour

    //Initalize the PhysicsManager
    public void Start()
    {
        /*mesh = GetComponent<MeshFilter>().mesh;
        vertexs =  mesh.vertices;
        tetrahedrons =  mesh.tetrahedrons;*/

        nodes = new List<NodeDef>();
        springs = new List<SpringDef>();
        Dictionary<string,int> edges = new Dictionary<string,int>();

        /*GameObject[] fixedBoxGO = GameObject.FindGameObjectsWithTag("FixedBox");
        coll = new Collider[fixedBoxGO.Length];

        //Get al the fixers in the scene
        for (int i = 0;i<fixedBoxGO.Length;i++){
            coll[i] = fixedBoxGO[i].GetComponent<Collider>();
        }*/


        volumes = new float[vertexs.Length];
        calculateVolumes();


        //Initialize all the nodes to the vertices porsitions
        for (int i = 0; i < vertexs.Length; i++){
            NodeDef n =  new NodeDef(transform.TransformPoint(vertexs[i]),i,windReaction);
            n.Manager = this;
            //n.isFixed();
            n.Mass = (tetrahedronDensity * volumes[i])/4;
            n.Damping = nodeDamping;
            n.collisionConstant = collisionConstant;
            nodes.Add(n);
        }

        //Inialize all the springs related to the tetrahedrons of the mesh
        for (int i = 0; i < tetrahedrons.Length; i+=4){
            int[] tetrahedron = new int[4];

            for (int z = 0; z<4;z++){
                tetrahedron[z] = tetrahedrons[i+z]; 
            }

            int a,b,c,d;

            a = tetrahedron[0];
            b = tetrahedron[1];
            c = tetrahedron[2];
            d = tetrahedron[3];

            string key = createkey(d,b);

            if (!edges.ContainsKey(key)){
                edges.Add(key,c);
                SpringDef auxSpringDef = new SpringDef(nodes[d],nodes[b]);
                auxSpringDef.Stiffness = stiffnes;
                springs.Add(auxSpringDef);
            }

            key = createkey(d,c);

            if (!edges.ContainsKey(key)){
                edges.Add(key,c);
                SpringDef auxSpringDef = new SpringDef(nodes[d],nodes[c]);
                auxSpringDef.Stiffness = stiffnes;
                springs.Add(auxSpringDef);
            }

            for (int w = 0; w<4;w++){
                a = tetrahedron[w];
                b = tetrahedron[(w+1)%4];
                c = tetrahedron[(w+2)%4];
                d = tetrahedron[(w+3)%4];

                key = createkey(a,b);

                if (!edges.ContainsKey(key)){
                    edges.Add(key,c);
                    SpringDef auxSpringDef = new SpringDef(nodes[a],nodes[b]);
                    auxSpringDef.Stiffness = stiffnes;
                    springs.Add(auxSpringDef);
                }/*else{
                    int aux = -1;
                    edges.TryGetValue(key,out aux);
                    SpringDef auxSpringDef = new SpringDef(nodes[c],nodes[aux]);
                    auxSpringDef.Stiffness = flexionStiffnes;
                    auxSpringDef.isFlexion = true;
                    springs.Add(auxSpringDef);
                }*/
            }
        }

    }

    //Returns the order of the key depending of the to values of the key
    private string createkey(int a, int b){
        if(a<b){
            return a+","+b;
        }else{
            return b+","+a;
        }
    }

    //Calculate all the volumes of the tetrahedrons in the mesh
    private void calculateVolumes(){
        for (int i = 0;i<vertexs.Length;i++){
            volumes[i] = 0f;
        }

         for (int i = 0; i < tetrahedrons.Length-3; i+=4){
            Vector3 a,b,c,d;

            a = vertexs[tetrahedrons[i]];
            b = vertexs[tetrahedrons[i+1]];
            c = vertexs[tetrahedrons[i+2]];
            d = vertexs[tetrahedrons[i+3]];

            float aux = Mathf.Abs((Vector3.Dot(Vector3.Cross((c-a),(d-a)),(b-a)))/6);
                
            volumes[tetrahedrons[i]] += aux;
            volumes[tetrahedrons[i+1]] += aux;
            volumes[tetrahedrons[i+2]] += aux;
            volumes[tetrahedrons[i+4]] += aux;

        }
    }
    
	public void Update()
	{
		if (Input.GetKeyUp (KeyCode.P))
			Paused = !Paused;

    }

    public void FixedUpdate()
    {
        
        if (Paused)
            return; // Not simulating

        // Select integration method
        switch (IntegrationMethod)
        {
            case Integration.Explicit: for (int i = 0;i<10;i++){stepExplicit();} break;
            case Integration.Symplectic: stepSymplectic(); break;
            default:
                throw new System.Exception("[ERROR] Should never happen!");
        }
    }

    #endregion

    /// <summary>
    /// Performs a simulation step in 1D using Explicit integration.
    /// </summary>
    private void stepExplicit()
	{
        GameObject[] windGO = GameObject.FindGameObjectsWithTag("Wind");
        winds = new Wind[windGO.Length];

        //Update the winds data
        for (int i = 0;i<windGO.Length;i++){
            winds[i] = windGO[i].GetComponent<Wind>();
        }

        collidables = GameObject.FindGameObjectsWithTag("Collidable");

        //Calculate normals and volumes
        /*mesh.RecalculateNormals();
        normals = mesh.normals;*/
        //calculateVolumes();

        //Update node forces
        foreach (NodeDef node in nodes)
        {
            int index = node.index;
            node.Force = Vector3.zero;
            //node.Mass = nodeMass;
            node.normal = normals[index];
            //node.surface = volumes[index];
            node.Force = Vector3.zero;
            node.ComputeForces(winds,collidables);
        }
        
        //Update spring forces
        foreach (SpringDef spring in springs)
        {
            if (spring.isFlexion){
                spring.Stiffness = flexionStiffnes;
            }else{
                spring.Stiffness = stiffnes;
            }
            spring.ComputeForces();
        }

        //Apply the explicit method to the nodes
        foreach (NodeDef node in nodes)
        {
            if (!node.Fixed)
            {
                node.Pos += node.Vel * TimeStep;
                node.Vel += node.Force * TimeStep / node.Mass;
                node.Update();
            }
        }

        //Change vertices coordinates form world to local
        for (int i = 0; i<vertexs.Length;i++){
            vertexs[i] = transform.InverseTransformDirection(vertexs[i]);
        }

        //Apply the new vertices positions
        mesh.vertices = vertexs;
    }

	/// <summary>
	/// Performs a simulation step in 1D using Symplectic integration.
	/// </summary>
	private void stepSymplectic()
	{
        GameObject[] windGO = GameObject.FindGameObjectsWithTag("Wind");
        winds = new Wind[windGO.Length];

        //Update the winds data
        for (int i = 0;i<windGO.Length;i++){
            winds[i] = windGO[i].GetComponent<Wind>();
        }

        collidables = GameObject.FindGameObjectsWithTag("Collidable");

        //Calculate normals and volumes
        /*mesh.RecalculateNormals();
        normals = mesh.normals;*/
        //calculateVolumes();

         //Update node forces
        foreach (NodeDef node in nodes)
        {
            int index = node.index;
            node.Mass = nodeMass;
            node.normal = normals[index];
            //node.surface = volumes[index];
            node.Force = Vector3.zero;
            node.ComputeForces(winds, collidables);
        }

         //Update spring forces
        foreach (SpringDef spring in springs)
        {
            if (spring.isFlexion){
                spring.Stiffness = flexionStiffnes;
            }else{
                spring.Stiffness = stiffnes;
            }
            spring.ComputeForces();
        }

        //Apply the symplectic method to the nodes
        foreach (NodeDef node in nodes)
        {
            if (!node.Fixed)
            {
                node.Vel += node.Force * TimeStep / node.Mass;
                node.Pos += node.Vel * TimeStep;
                node.Update();
            }
        }

        //Change vertices coordinates form world to local
        for (int i = 0; i<vertexs.Length;i++){
            vertexs[i] = transform.InverseTransformDirection(vertexs[i]);
        }
        
        //Apply the new vertices positions
        mesh.vertices = vertexs;
    }


}
