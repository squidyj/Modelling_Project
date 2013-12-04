using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Diagnostics;
namespace Subdivision_Project
{
	public class Model
	{
		//attribute object, vertex buffer, and index buffer locations needed for opengl
		int vao, vbo, ibo;
		int program;
		//local transformation matrix
		
		//should only be updated from within?

		Matrix4 transform;
		public Matrix4 Transform
		{
			get { return transform; }
		}

		//indicates whether model should be rendered
		bool loaded = false;
		public bool Loaded
		{
			get { return loaded; }
		}

		public Vector3 center;
		Vector3 min;
		Vector3 max;
		float scale;

		//index array
		Triangle[] triangles;
		public Triangle[] Triangles
		{
			get { return triangles; }
			set { triangles = value; }
		}

		HashSet<int>[] aVertices;
		HashSet<int>[] aFaces;
		HashSet<int> boundary;
			
		//vertex values, texture uv, and vertex normal
		Vertex[] vertices;
		public Vertex[] Vertices
		{
			get { return vertices; }
			set { vertices = value; }
		}

		public Model(int p)
		{
			program = p;
			vertices = new Vertex[3];
			vertices[0].vert = new Vector3(1f, -1f, 1f);
			vertices[1].vert = new Vector3(-1f, 0f, 1f);
			vertices[2].vert = new Vector3(1f, 1f, -1f);

			vertices[0].normal = new Vector3(0f, 1f, 0f);
			vertices[1].normal = new Vector3(0f, 1f, 0f);
			vertices[2].normal = new Vector3(0f, 1f, 0f);

			vertices[0].texcoord = new Vector2(0f, 1f);
			vertices[1].texcoord = new Vector2(1f, 0f);
			vertices[2].texcoord = new Vector2(1f, 1f); 

			triangles = new Triangle[1];
			triangles[0] = new Triangle(2,1,0);
			calculate();
			load();
		}

		//load model data into memory from file with a given pathname
		public Model(string filename, int p)
		{
			program = p;
			//load and interpret file format
			ObjLoader.Load(this, filename);
			//calculate the bounding box, potentially rewrite vertex values
			calculate();
			//create required opengl objects
			face();
			transform = Matrix4.Identity;

			load();
		}

		private void addAdjacent(int v, int v1, int v2, int f)
		{
			//for a given vertex v, encode that it is adjacent to the face f
			aFaces[v].Add(f);
			//and vertices v1 and v2;
			aVertices[v].Add(v1);
			aVertices[v].Add(v2);
		}

		private void addBoundary(int v1, int v2, int f)
		{
			//find all of the faces that are adjacent to the [v1,v2] edge
			HashSet<int> bound = new HashSet<int>(aFaces[v1].Intersect(aFaces[v2]));
			//if there is more than 1 such face then the edge cannot be on a boundary
			if(bound.Count > 1)
				return;
			//otherwise the edge is a boundary and both vertices are boundary vertices
			boundary.Add(v1);
			boundary.Add(v2);
		}

		private void cleanMesh()
		{
			HashSet<int[]> faces = new HashSet<int[]>();
			int k;
			int[] v = new int[3];
			List<int> removal = new List<int>();
			Triangle t;
			for(int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				v[0] = t.v0; v[1] = t.v1; v[2] = t.v2;
				if (v[0] > v[1])
				{ k = v[0]; v[0] = v[1]; v[1] = k; }
				if (v[1] > v[2])
				{ k = v[1]; v[1] = v[2]; v[2] = k; }
				if (v[0] > v[1])
				{ k = v[0]; v[0] = v[1]; v[1] = k; }
				if (faces.Contains(v))
					removal.Add(i);
				else
					faces.Add(v);
			}

			var tris = triangles.ToList();
			foreach (int n in removal)
				tris.RemoveAt(n);
			triangles = tris.ToArray();
		}

		private void makeAdjacency()
		{

			//initialize adjacency structures
			boundary = new HashSet<int>();
			aVertices = new HashSet<int>[vertices.Length];
			aFaces = new HashSet<int>[vertices.Length];

			for (int i = 0; i < aVertices.Length; i++)
			{
				aVertices[i] = new HashSet<int>();
				aFaces[i] = new HashSet<int>();
			}
			
			Triangle t;
			for(int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				addAdjacent(t.v0, t.v1, t.v2, i);
				addAdjacent(t.v1, t.v2, t.v0, i);
				addAdjacent(t.v2, t.v0, t.v1, i);
			}
			for(int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				addBoundary(t.v0, t.v1, i);
				addBoundary(t.v1, t.v2, i);
				addBoundary(t.v2, t.v0, i);
			}
		}

		//return the index of the current vertex
		private int edgeVertex(int v0, int v1, int o1, List<Vertex> verts, Dictionary<Vertex, int> lookup)
		{
			//we are given two adjacent vertices and an opposite vertex, we need to find the final opposite vertex if it exists
			Vertex v;
			//the edge vertex is boundary if and only if its adjacent vertices are boundary
			if (boundary.Contains(v0) && boundary.Contains(v1))
				v = 0.5f * (verts[v0] + verts[v1]);
			else
			{
				//otherwise there is a second opposite vertex
				//find all of the vertices that are adjacent to both ends of the edge
				HashSet<int> hs = new HashSet<int>(aVertices[v0].Intersect(aVertices[v1]));
				//remove from that set the already gathered opposite
				hs.Remove(o1);
				if (hs.Count == 0)
					Console.Out.WriteLine(v0 + ", " + v1);
				int o2 = hs.Last();
				v = 0.375f * (verts[v0] + verts[v1]) + 0.125f * (verts[o1] + verts[o2]);
			}
			int n;
			if(lookup.TryGetValue(v, out n))
				return n;
			n = verts.Count;
			verts.Add(v);
			lookup.Add(v, n);
			return n;
		}

		public void subdivide()
		{

			List<Triangle> mTriangles = new List<Triangle>();
			List<Vertex> mVerts = vertices.ToList();
			Dictionary<Vertex, int> lookup = new Dictionary<Vertex, int>();
			int off = vertices.Length;
			int v0, v1, v2;

			//begin by updating adjacency data for all vertices
			//ideally this would be maintained with each modification to the mesh
			makeAdjacency();
			//for each face in the mesh
			int m = triangles.Length;
			Triangle t;
			for (int i = 0; i < m; i++)
			{
				t = triangles[i];

				//construct a vertex on each edge
				v0 = edgeVertex(t.v0, t.v1, t.v2, mVerts, lookup);
				v1 = edgeVertex(t.v1, t.v2, t.v0, mVerts, lookup);
				v2 = edgeVertex(t.v2, t.v0, t.v1, mVerts, lookup);

				//construct the 4 new faces maintaining the original winding
				mTriangles.Add(new Triangle(t.v0, v0, v2));
				mTriangles.Add(new Triangle(v0, t.v1, v1));
				mTriangles.Add(new Triangle(v0, v1, v2));
				mTriangles.Add(new Triangle(v2, v1, t.v2));
			}

			Vertex v, vx;
			float a;
			int n;
			int[] adj;
			HashSet<int> bound;
			for (int i = 0; i < off; i++)
			{

				if (boundary.Contains(i))
				{
					//find the vertices that are adjacent to the target vertex AND boundary vertices
					bound = new HashSet<int>(aVertices[i].Intersect(boundary));
					//there should only be 2 such vertices, if something wickity wack is going on, better handle it
					adj = bound.ToArray();
					v = 0.125f * (vertices[adj[0]] + vertices[adj[1]]) + 0.75f * vertices[i];
				}
				else
				{
					n = aVertices[i].Count;
					a = 1 / (float)n * (float)(0.625f - Math.Pow((0.375f + 0.25f * Math.Cos(2 * Math.PI / n)), 2));
					vx = (1 - n * a) * mVerts[i];
					v = new Vertex();
					foreach (int j in aVertices[i])
						v += mVerts[j];
					v = vx + a * v;
				}
				mVerts[i] = v;
			}
			 
			vertices = mVerts.ToArray();
			triangles = mTriangles.ToArray();
			load();
		}

        public Matrix4 add(Matrix4 m, Matrix4 n)
        {
            return new Matrix4(
                m.M11+n.M11, m.M12+n.M12, m.M13+n.M13, m.M14+n.M14,
                m.M21+n.M21, m.M22+n.M22, m.M23+n.M23, m.M24+n.M24,
                m.M31+n.M31, m.M32+n.M32, m.M33+n.M33, m.M34+n.M34,
                m.M41+n.M41, m.M42+n.M42, m.M43+n.M43, m.M44+n.M44);
        }

        private Matrix4[] computeQ(Vertex[] theVertices, LinkedList<int>[] trisUsingVertex)
        {
            int numVertices = theVertices.Length;
            int numTris = trisUsingVertex.Length;

            Matrix4[] q = new Matrix4[numVertices];

            for (int i = 0; i < numVertices; i++)
            {
                Matrix4 qi = new Matrix4();

                // TODO: come up with a better way to get triangles?
                LinkedListNode<int> currentTri = trisUsingVertex[i].First;

                // for every triangle containing this vertex
                while (currentTri!=null)
                {
                    Triangle target = triangles[currentTri.Value];
                    if (target.v0 == i || target.v1 == i || target.v2 == i)
                    {
                        // Construct Kp, the matrix
                        // [ a^2 ab  ac  ad
                        //   ab  b^2 bc  bd
                        //   ac  bc  c^2 cd
                        //   ad  bd  cd  d^2 ]

                        Matrix4 kp = new Matrix4();
                        Plane plane = new Plane(
                            theVertices[target.v0].vert,
                            theVertices[target.v1].vert,
                            theVertices[target.v2].vert);

                        kp.Row0.X = plane.a * plane.a;
                        kp.Row0.Y = kp.Row1.X = plane.a * plane.b;
                        kp.Row0.Z = kp.Row2.X = plane.a * plane.c;
                        kp.Row0.W = kp.Row3.X = plane.a * plane.d;

                        kp.Row1.Y = plane.b * plane.b;
                        kp.Row1.Z = kp.Row2.Y = plane.b * plane.c;
                        kp.Row1.W = kp.Row3.Y = plane.b * plane.d;

                        kp.Row2.Z = plane.c * plane.c;
                        kp.Row2.W = kp.Row3.Z = plane.c * plane.d;

                        kp.Row3.W = plane.d * plane.d;

                        qi = add(qi, kp);
                    }

                    currentTri = currentTri.Next;
                }

                q[i] = qi;
            }

            return q;
        }

        // TODO: Make this better
        private bool isEdge(int v1, int v2, LinkedList<int>[] trisUsingVertex)
        {
            LinkedListNode<int> currentTri = trisUsingVertex[v1].First;
            while (currentTri != null)
            {
                if (triangles[currentTri.Value].v0 == v2 || triangles[currentTri.Value].v1 == v2 || triangles[currentTri.Value].v2 == v2)
                {
                    return true;
                }

                currentTri = currentTri.Next;
            }
            return false;
        }

        private float calcCost(Vector3 v, Matrix4 q)
        {
            //delta(v) = v^TQv

            float vx, vy, vz, vw;

            vx = v.X * q.M11 + v.Y * q.M21 + v.Z * q.M31 + 1.0f * q.M41;
            vy = v.X * q.M12 + v.Y * q.M22 + v.Z * q.M32 + 1.0f * q.M42;
            vz = v.X * q.M13 + v.Y * q.M23 + v.Z * q.M33 + 1.0f * q.M43;
            vw = v.X * q.M14 + v.Y * q.M24 + v.Z * q.M34 + 1.0f * q.M44;

            return vx * v.X + vy * v.Y + vz * v.Z + vw * 1.0f;
        }

        // TODO: Improve new vertex selection
        private VertexPair simpleVBarCalc(int v1s_index, int v2s_index, Matrix4[] q)
        {
            Matrix4 qbar = add(q[v1s_index], q[v2s_index]);

            // Simple scheme: select either v1, v2, or (v1+v2)/2 
            Vector3 v1 = vertices[v1s_index].vert;
            Vector3 v2 = vertices[v2s_index].vert;

            // depending on which one of these produces the lowest value of delta (v bar)
            Vector3 newVertex = (v1 + v2) / 2;
            float cost = calcCost(newVertex, qbar);

            float newCost;
            if ((newCost = calcCost(v1, qbar)) < cost)
            {
                cost = newCost;
                newVertex = v1;
            }

            if ((newCost = calcCost(v2, qbar)) < cost)
            {
                cost = newCost;
                newVertex = v2;
            }

            return new VertexPair(v1s_index, v2s_index, cost, newVertex);
        }

        // TODO: Complete this
        public void simplify(int steps, float threshold)
        {
            // Surface Simplification Using Quadric Error Metrics (Garland)

            Console.Out.WriteLine("Now generating vertex-triangle association list!");
            // An easy way to get all the triangles associated with a vertex
            LinkedList<int>[] trisUsingVertex = new LinkedList<int>[vertices.Length];

            for (int i = 0; i < trisUsingVertex.Length; i++)
            {
                trisUsingVertex[i] = new LinkedList<int>();
            }

            for (int i = 0; i < triangles.Length; i++)
            {
                trisUsingVertex[triangles[i].v0].AddLast(i);
                trisUsingVertex[triangles[i].v1].AddLast(i);
                trisUsingVertex[triangles[i].v2].AddLast(i);
            }
            Console.Out.WriteLine("Vertex-triangle association list completed!");

            Console.Out.WriteLine("Now computing Q matrices for initial vertices!");
            // 1. Compute the Q matrices for all the initial vertices.
            Matrix4[] q = computeQ(vertices, trisUsingVertex);
            Console.Out.WriteLine("Q matrices for initial vertices completed!");

            Console.Out.WriteLine("Now selecting valid pairs!");
            // 2. Select all valid pairs.
            VCSKicksCollection.PriorityQueue<VertexPair> validPairs = new VCSKicksCollection.PriorityQueue<VertexPair>();

            int numVertices = vertices.Length;

            for (int i = 0; i < numVertices; i++)
            {
                for (int j = 0; j < numVertices; j++)
                {
                    if (i != j)
                    {
                        // For every two unique vertices, if they make up an edge or are within the
                        // specified threshold, they are a valid pair.
                        Vector3 v1 = vertices[i].vert;
                        Vector3 v2 = vertices[j].vert;
                        if (isEdge(i, j, trisUsingVertex) || (v1 - v2).Length < threshold)
                        {
                            // 3. Compute the optimal contraction target v bar for each valid pair
                            // (v_1, v_2). The error v bar^T (Q_1 + Q_2)v bar of this target vertex becomes
                            // the cost of contracting that pair.

                            // 4. Place all the pairs in a heap keyed on cost with the minimum
                            // cost pair at the top

                            validPairs.Enqueue(simpleVBarCalc(i, j, q));
                        }
                    }
                }
            }
            Console.Out.WriteLine("Valid pair selection completed!");

            Console.Out.WriteLine("Now removing " + validPairs.Count + " pairs...");
            // 5. Iteratively remove the pair (v_1, v_2) of least cost from the heap,
            // contract this pair, and update the costs of all valid pairs involving
            // v_1.
            while (validPairs.Count > 0)
            {
                VertexPair removePair = validPairs.Dequeue();

                Console.Out.WriteLine("Contracting pair: (" + removePair.v1 + ", " + removePair.v2 + ")");
                Console.Out.WriteLine(removePair.v1 + ": " + vertices[removePair.v1].vert.X + ", "
                    + vertices[removePair.v1].vert.Y + ", " + vertices[removePair.v1].vert.Z);
                Console.Out.WriteLine(removePair.v2 + ": " + vertices[removePair.v2].vert.X + ", "
                    + vertices[removePair.v2].vert.Y + ", " + vertices[removePair.v2].vert.Z);

                vertices[removePair.v1].vert = removePair.newVertex;

                Console.Out.WriteLine("New vertex: " + removePair.v1);
                Console.Out.WriteLine(removePair.v1 + ": " + vertices[removePair.v1].vert.X + ", "
                    + vertices[removePair.v1].vert.Y + ", " + vertices[removePair.v1].vert.Z);

                // Contract pairs
                LinkedListNode<int> currentTri = trisUsingVertex[removePair.v2].First;

                while (currentTri != null)
                {
                    if (triangles[currentTri.Value].v0 == removePair.v2)
                    {
                        triangles[currentTri.Value].v0 = removePair.v1;
                    }
                    else if (triangles[currentTri.Value].v1 == removePair.v2)
                    {
                        triangles[currentTri.Value].v1 = removePair.v1;
                    }
                    else if (triangles[currentTri.Value].v2 == removePair.v2)
                    {
                        triangles[currentTri.Value].v2 = removePair.v1;
                    }
                    currentTri = currentTri.Next;
                }

                // Mark unused vertex
                trisUsingVertex[removePair.v2] = null;                

                // Update costs
                VCSKicksCollection.PriorityQueue<VertexPair> updatePairs = new VCSKicksCollection.PriorityQueue<VertexPair>();

                for (int i = 0; i < validPairs.Count; i++)
                {
                    VertexPair updatePair = validPairs.Dequeue();

                    if (updatePair.v1 == removePair.v1 || updatePair.v2 == removePair.v1)
                    {
                        updatePairs.Enqueue(simpleVBarCalc(updatePair.v1, updatePair.v2, q));
                    }
                    else if (updatePair.v1 == removePair.v2)
                    {
                        updatePairs.Enqueue(simpleVBarCalc(removePair.v1, updatePair.v2, q));
                    }
                    else if (updatePair.v2 == removePair.v2)
                    {
                        updatePairs.Enqueue(simpleVBarCalc(updatePair.v1, removePair.v1, q));
                    }
                    else
                    {
                        updatePairs.Enqueue(updatePair);
                    }
                }

                validPairs = updatePairs;
            }

            int newNumVertices = vertices.Length;

            // TODO: Remove now unused vertices
            for (int i = 0; i < trisUsingVertex.Length; i++)
            {
                if (trisUsingVertex[i] == null)
                {
                    newNumVertices--;

                    for (int j = i; j < newNumVertices; j++)
                    {
                        vertices[j] = vertices[j + 1];
                    }

                    for (int j = 0; j < triangles.Length; j++)
                    {
                        if (triangles[j].v0 > i) { triangles[j].v0--; }
                        if (triangles[j].v1 > i) { triangles[j].v1--; }
                        if (triangles[j].v2 > i) { triangles[j].v2--; }
                    }
                }
            }

            Vertex[] newVertices = new Vertex[newNumVertices];

            for (int i = 0; i < newNumVertices; i++ )
            {
                newVertices[i] = vertices[i];
            }

            Console.Out.WriteLine("Number of vertices " + vertices.Length + " reduced to " + newNumVertices);

            vertices = newVertices;

            Console.Out.WriteLine("Pairs removed!");

            load();
        }

		private void face()
		{
			Vector3 v1, v2, norm;
			int[] count = new int[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i].normal = new Vector3(0, 0, 0);
			}
			foreach (Triangle t in triangles)
			{
				v1 = vertices[t.v1].vert - vertices[t.v0].vert;
				v2 = vertices[t.v2].vert - vertices[t.v0].vert;
				norm = Vector3.Normalize(Vector3.Cross(v1, v2));
				vertices[t.v0].normal += norm;
				vertices[t.v1].normal += norm;
				vertices[t.v2].normal += norm;
				count[t.v0]++; count[t.v1]++; count[t.v2]++;
			}

			for(int i = 0; i < vertices.Length; i++)
			{
				vertices[i].normal /= count[i];
				vertices[i].normal = Vector3.Normalize(vertices[i].normal);
			}
		}


		//naive calculation of a bounding box for the model
		private void calculate()
		{
			min = new Vector3();
			max = new Vector3();
			center = new Vector3();

			foreach (Vertex v in vertices)
			{
				if (v.vert.X > max.X)
					max.X = v.vert.X;
				if (v.vert.Y > max.Y)
					max.Y = v.vert.Y;
				if (v.vert.Z > max.Z)
					max.Z = v.vert.Z;
				if (v.vert.X < min.X)
					min.X = v.vert.X;
				if (v.vert.Y < min.Y)
					min.Y = v.vert.Y;
				if (v.vert.Z < min.Z)
					min.Z = v.vert.Z;
			}
			center.X = 0.5f * (max.X + min.X);
			center.Y = 0.5f * (max.Y + min.Y);
			center.Z = 0.5f * (max.Z + min.Z);
		}

		
		//set up model attributes and bind buffer for rendering
		public void load()
		{
			//calculate size of index and vertex buffers
			int vertSize = Marshal.SizeOf(typeof(Vertex));
			int triSize = Marshal.SizeOf(typeof(Triangle));
			int vecSize = Marshal.SizeOf(typeof(Vector3));

			GL.GenVertexArrays(1, out vao);
			GL.BindVertexArray(vao);

			GL.GenBuffers(1, out vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertSize * vertices.Length), vertices, BufferUsageHint.StaticDraw);

			GL.GenBuffers(1, out ibo);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(triSize * triangles.Length), triangles, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertSize, 0);

			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, vertSize, vecSize);

			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertSize, 2 * vecSize);
		
			//state is set, unbind objects so they are not modified.
			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			loaded = true;
		}

		//unbind buffers, clean up gpu-side memory, free locations
		public void unload()
		{

		}


		//draw the model
		public void draw()
		{
			GL.BindVertexArray(vao);
			GL.DrawElements(BeginMode.Triangles, triangles.Length *3, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Triangle
		{
			public Triangle(int i0, int i1, int i2)
			{
				v0 = i0; v1 = i1; v2 = i2;
			}
			public int v0, v1, v2;
		}

		//extended vertex contains a list of faces which the vertex participates in

		[StructLayout(LayoutKind.Sequential)]
		public struct Vertex
		{
			public Vertex(Vector3 v, Vector3 n, Vector2 t)
			{
				vert = v; normal = n; texcoord = t;
			}
			public static Vertex operator +(Vertex v1, Vertex v2)
			{
				return new Vertex(v1.vert + v2.vert, v1.normal + v2.normal, v1.texcoord + v2.texcoord);
			}
			public static Vertex operator *(float s, Vertex v)
			{
				return new Vertex(s * v.vert, s * v.normal, s * v.texcoord);
			}
			public Vector3 vert;
			public Vector3 normal;
			public Vector2 texcoord;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Material
		{
			public Material(Vector3 s, Vector3 d, float e)
			{
				ks = s; kd = d; exp = e;
			}
			public Vector3 ks;
			public Vector3 kd;
			float exp;
		}

        [StructLayout(LayoutKind.Sequential)]
        public struct Plane
        {
            // Create a plane given three vertices
            public Plane(Vector3 tri_v1, Vector3 tri_v2, Vector3 tri_v3)
            {
                Vector3 ab = tri_v1 - tri_v2;
                Vector3 ac = tri_v1 - tri_v3;
                Vector3 plane = Vector3.Cross(ab, ac);
                a = plane.X;
                b = plane.Y;
                c = plane.Z;

                d = -((a * tri_v1.X) + (b * tri_v1.Y) + (c * tri_v1.Z));
            }
            public float a, b, c, d;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPair : IComparable
        {
            // Create a pair given two vertices
            public VertexPair(int vertex1, int vertex2)
            {
                v1 = vertex1;
                v2 = vertex2;
                cost = -1;
                newVertex = new Vector3();
            }
            // Keep track of an already calculated cost
            public VertexPair(int vertex1, int vertex2, float theCost, Vector3 theNewVertex)
            {
                v1 = vertex1;
                v2 = vertex2;
                cost = theCost;
                newVertex = theNewVertex;
            }
            
            public int CompareTo(object obj)
            {
                if (obj is VertexPair)
                {
                    VertexPair v = (VertexPair)obj;
                    return cost.CompareTo(v.cost);
                }
                else { throw new ArgumentException("Object is not a vertexPair."); }
            }

            public int v1, v2;
            public Vector3 newVertex;
            public float cost;
        }
	}
}