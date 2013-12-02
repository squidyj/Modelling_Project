using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

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

		int[][] neighbours;
		//index array
		Triangle[] triangles;
		public Triangle[] Triangles
		{
			get { return triangles; }
			set { triangles = value; }
		}

		HashSet<int>[] vFaces;
	
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

		//make sure all the vertices know what they are adjacent to
		private void makeAdjacency()
		{

			vFaces = new HashSet<int>[vertices.Length];
			for (int i = 0; i < vFaces.Length; i++)
				vFaces[i] = new HashSet<int>();

			foreach (Triangle t in triangles)
			{
				vFaces[t.v0].Add(t.v1);
				vFaces[t.v0].Add(t.v2);

				vFaces[t.v1].Add(t.v2);
				vFaces[t.v1].Add(t.v0);

				vFaces[t.v2].Add(t.v1);
				vFaces[t.v2].Add(t.v0);
			}
			int c = 0;
			foreach (HashSet<int> hs in vFaces)
			{
				if (hs.Count == 2)
				{
					c++;
				}
			}
			Console.Out.WriteLine(c);
		}

		//return the index of the current vertex
		private int edgeVertex(int v0, int v1, int o1, List<Vertex> verts, Dictionary<Vertex, int> lookup)
		{
			//find the other remaining vertex 
			HashSet<int> hs = new HashSet<int>(vFaces[v0].Intersect(vFaces[v1]));
			hs.Remove(o1);
			if (hs.Count == 2)
				Console.Out.WriteLine("correct");
			int o2 = -1;
			if (hs.Count > 0)
				o2 = hs.Last();
			Vertex v;
			if(o2 != -1)
				v = 0.375f * (verts[v0] + verts[v1]) + 0.125f * (verts[o1] + verts[o2]);
			else
				v = 0.5f * (verts[v0] + verts[v1]);
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
			//ideally this would be maintained with each modifica
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
		
				//Console.Out.WriteLine(i);
			}

			Vertex v, vi;
			float a;
			int n;
			
			for (int i = 0; i < off; i++)
			{

				n = vFaces[i].Count;
				a = 1 / (float)n * (float)(0.625f - Math.Pow((0.375f + 0.25f * Math.Cos(2 * Math.PI / n)), 2));
				vi = (1 - n * a) * mVerts[i];
				v = new Vertex();
				foreach (int j in vFaces[i])
					v += mVerts[j];
				v = vi + a * v;
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

        private Matrix4[] computeQ(Vertex[] theVertices, Triangle[] theTriangles)
        {
            int numVertices = theVertices.Length;
            int numTris = theTriangles.Length;

            Matrix4[] q = new Matrix4[numVertices];

            for (int i = 0; i < numVertices; i++)
            {
                Matrix4 qi = new Matrix4();

                // TODO: come up with a better way to get triangles?

                // for every triangle containing this vertex
                for (int j = 0; j < numTris; j++)
                {
                    Triangle target = theTriangles[j];
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
                }

                q[i] = qi;
            }

            return q;
        }

        // TODO: Complete this

        public void simplify(int steps)
        {
            // Surface Simplification Using Quadric Error Metrics (Garland)

            // 1. Compute the Q matrices for all the initial vertices.
            Matrix4[] q = computeQ(vertices, triangles);

            // 2. Select all valid pairs.
            
            // 3. Compute the optimal contraction target v for each valid pair
            // (v_1, v_2). The error v^T (Q_1 + Q_2)v of this target vertex becomes
            // the cost of contracting that pair.

            // 4. Place all the pairs in a heap keyed on cost with the minimum
            // cost pair at the top

            // 5. Iteratively remove the pair (v_1, v_2) of least cost from the heap,
            // contract this pair, and update the costs of all valid pairs involving
            // v_1.
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
	}
}