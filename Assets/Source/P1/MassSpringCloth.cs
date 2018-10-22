using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic physics manager capable of simulating a given ISimulable
/// implementation using diverse integration methods: explicit,
/// implicit, Verlet and semi-implicit.
/// </summary>
public class MassSpringCloth : MonoBehaviour 
{
	/// <summary>
	/// Default constructor. Zero all. 
	/// </summary>
	public MassSpringCloth()
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
    public List<Node> nodes;
    public List<Spring> springs;

    #endregion

    #region OtherVariables

    private Mesh mesh;

    public Vector3[] vertexs;
    public Vector3[] normals;
    public int[] triangles;
    public float[] surfaces;

    public Collider[] coll;
    public Wind[] winds;
    public GameObject[] collidables;



    #endregion

    #region MonoBehaviour

    //Initalize the PhysicsManager
    public void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertexs =  mesh.vertices;
        triangles =  mesh.triangles;

        nodes = new List<Node>();
        springs = new List<Spring>();
        Dictionary<string,int> edges = new Dictionary<string,int>();

        GameObject[] fixedBoxGO = GameObject.FindGameObjectsWithTag("FixedBox");
        coll = new Collider[fixedBoxGO.Length];

        //Get al the fixers in the scene
        for (int i = 0;i<fixedBoxGO.Length;i++){
            coll[i] = fixedBoxGO[i].GetComponent<Collider>();
        }


        surfaces = new float[vertexs.Length];

        //Initialize all the nodes to the vertices porsitions
        for (int i = 0; i < vertexs.Length; i++){
            Node n =  new Node(transform.TransformPoint(vertexs[i]),i,windReaction);
            n.Manager = this;
            n.isFixed();
            n.Mass = nodeMass;
            n.Damping = nodeDamping;
            n.collisionConstant = collisionConstant;
            nodes.Add(n);
        }

        //Inialize all the springs related to the triangles of the mesh
        for (int i = 0; i < triangles.Length; i+=3){
            int[] triangle = new int[3];

            for (int z = 0; z<3;z++){
                triangle[z] = triangles[i+z]; 
            }

            int a,b,c;

            for (int w = 0; w<3;w++){
                a = triangle[w];
                b = triangle[(w+1)%3];
                c = triangle[(w+2)%3];
                string key = createkey(a,b);

                if (!edges.ContainsKey(key)){
                    edges.Add(key,c);
                    Spring auxSpring = new Spring(nodes[a],nodes[b]);
                    auxSpring.Stiffness = stiffnes;
                    springs.Add(auxSpring);
                }else{
                    int aux = -1;
                    edges.TryGetValue(key,out aux);
                    Spring auxSpring = new Spring(nodes[c],nodes[aux]);
                    auxSpring.Stiffness = flexionStiffnes;
                    auxSpring.isFlexion = true;
                    springs.Add(auxSpring);
                }
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

    //Calculate all the surfaces of the triangles in the mesh
    private void calculateSurfaces(){
        for (int i = 0;i<vertexs.Length;i++){
            surfaces[i] = 0f;
        }

         for (int i = 0; i < triangles.Length-2; i+=3){
            Vector3 a,b,c;

            a = vertexs[triangles[i]];
            b = vertexs[triangles[i+1]];
            c = vertexs[triangles[i+2]];

            float aux = ((b-a).magnitude * (c-((b+a)/2)).magnitude)/3f;
                
            surfaces[triangles[i]] += aux;
            surfaces[triangles[i+1]] += aux;
            surfaces[triangles[i+2]] += aux;

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

        //Calculate normals and surfaces
        mesh.RecalculateNormals();
        normals = mesh.normals;
        calculateSurfaces();

        //Update node forces
        foreach (Node node in nodes)
        {
            int index = node.index;
            node.Force = Vector3.zero;
            node.Mass = nodeMass;
            node.normal = normals[index];
            node.surface = surfaces[index];
            node.Force = Vector3.zero;
            node.ComputeForces(winds,collidables);
        }
        
        //Update spring forces
        foreach (Spring spring in springs)
        {
            if (spring.isFlexion){
                spring.Stiffness = flexionStiffnes;
            }else{
                spring.Stiffness = stiffnes;
            }
            spring.ComputeForces();
        }

        //Apply the explicit method to the nodes
        foreach (Node node in nodes)
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

        //Calculate normals and surfaces
        mesh.RecalculateNormals();
        normals = mesh.normals;
        calculateSurfaces();

         //Update node forces
        foreach (Node node in nodes)
        {
            int index = node.index;
            node.Mass = nodeMass;
            node.normal = normals[index];
            node.surface = surfaces[index];
            node.Force = Vector3.zero;
            node.ComputeForces(winds, collidables);
        }

         //Update spring forces
        foreach (Spring spring in springs)
        {
            if (spring.isFlexion){
                spring.Stiffness = flexionStiffnes;
            }else{
                spring.Stiffness = stiffnes;
            }
            spring.ComputeForces();
        }

        //Apply the symplectic method to the nodes
        foreach (Node node in nodes)
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
