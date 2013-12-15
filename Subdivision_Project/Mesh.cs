using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using Subdivision_Project.Primitives;
using System.Diagnostics;
namespace Subdivision_Project
{

	public class Mesh
	{
		//vertex attribute object, vertex buffer object, and index buffer object locations
		int vao, vbo, ibo;

		Material mat;
		//rename to adjacentVertices
	
		BoundingBox box;
		public BoundingBox Box
		{
			get { return box; }
		}

		Matrix4 transform;
		public Matrix4 Transform
		{
			get { return transform; }
		}

		public DrawTriangle[] drawtriangles;
		public DrawVertex[] drawvertices;

		public List<Vertex> vertices = new List<Vertex>();
		public List<Triangle> triangles = new List<Triangle>();
		public HashSet<Pair> edges;

		public Mesh(string pathname)
		{
			//load the model data from file
			//TODO change objloader to work with mesh type object

			
			Stopwatch overall = new Stopwatch();
			overall.Start();
			Stopwatch timer = new Stopwatch();
			ObjLoader.Load(this, pathname);
			//sweep redundant triangles
			timer.Restart();
			Console.Out.WriteLine("Checking Mesh for duplicate triangles");
			cleanMesh();
			Console.Out.WriteLine("Check complete, took " + timer.ElapsedMilliseconds + " milliseconds");

			timer.Restart();
			Console.Out.WriteLine("Calculating bounding box");
			box = new BoundingBox(this);
			Console.Out.WriteLine("Bounding box complete, took " + timer.ElapsedMilliseconds + " milliseconds");

			timer.Restart();
			Console.Out.WriteLine("Initializing Half-Edge Structure");
			initHalfEdge();
			Console.Out.WriteLine("Half-Edge complete, took " + timer.ElapsedMilliseconds + " milliseconds");

			setVerts();
			reset();
			firstLoad();
			Console.Out.WriteLine("Model took " + overall.ElapsedMilliseconds + " milliseconds to load");
			Console.Out.WriteLine("Vertices: " + drawvertices.Length);
			Console.Out.WriteLine("Faces: " + drawtriangles.Length);
			Console.Out.WriteLine("Testing");

			SortedSet<Pair> pairs = new SortedSet<Pair>();
			Pair p1 = new Pair(vertices[0], vertices[1]);
			p1.cost = 0.5f;
			Pair p2 = new Pair(vertices[0], vertices[1]);
			p2.cost = 0.6f;
			Pair p3 = new Pair(vertices[1], vertices[2]);
			p3.cost = 0.5f;
			Pair p4 = new Pair(vertices[0], vertices[1]);
			p4.cost = 0.5f;

			pairs.Add(p1);
			Console.Out.WriteLine(pairs.Contains(p2));
			Console.Out.WriteLine(pairs.Contains(p3));
			Console.Out.WriteLine(pairs.Contains(p4));

		}

		public Mesh(DrawVertex[] verts, DrawTriangle[] tris, BoundingBox b)
		{
			drawvertices = verts;
			drawtriangles = tris;
			box = b;
			//should be updated while the modification is ongoing
			firstLoad();
		}

		public void reset()
		{ transform = box.orientMesh(); }

		private void setVerts()
		{
			drawvertices = new DrawVertex[vertices.Count];
			for (int i = 0; i < vertices.Count; i++)
			{
				drawvertices[i] = new DrawVertex(vertices[i].pos);
				vertices[i].n = i;			
			}		
		}

		private void setTris()
		{
			List<DrawTriangle> dt = new List<DrawTriangle>();
			List<Vertex> v;
			DrawTriangle d;
			foreach(Triangle t in triangles)
			{
				v = t.vertices();
				d = new DrawTriangle(v[0].n, v[1].n, v[2].n);
				d.normal = t.normal;
				dt.Add(d);

				//adding opposite vertices of the triangle as attributes of a vertex
				//used for wirefram shader and higher quality normal than gradient method

			}
			drawtriangles = dt.ToArray();
		}

		private void initFaceNormals()
		{
			List<Vertex> v;
			Vector3 v1, v2;
			foreach (Triangle t in triangles)
			{
				v = t.vertices();
				v1 = v[1].pos - v[0].pos;
				v2 = v[2].pos - v[0].pos;
				t.normal = Vector3.Normalize(Vector3.Cross(v1, v2));
			}
		}

		public void reconstruct()
		{
			setVerts();
			loadVerts();
			initFaceNormals();
			setTris();
			loadTris();
		}

		//calculates Q matrices for all triangles and Verteices
		//generates half-edge data structure
		//generates a set of edges that exist in the mesh as a starting point for pair contraction
		private void initHalfEdge()
		{
			HashSet<HalfEdge> he = new HashSet<HalfEdge>();
			Dictionary<Pair, HalfEdge> lookup = new Dictionary<Pair, HalfEdge>();
			edges = new HashSet<Pair>();
			HalfEdge e0, e1, e2, opp;
			Triangle t;
			Stopwatch timer = new Stopwatch();
			Console.Out.WriteLine("Processing Interior HalfEdges");
			timer.Start();
			foreach (DrawTriangle dt in drawtriangles)
			{
				//constructor builds normal and Q matrix for the face
				t = new Triangle(vertices[dt.v0], vertices[dt.v1], vertices[dt.v2]);
				triangles.Add(t);
				//sum the q matrices for each vertex as we go through the list
				vertices[dt.v0].Q += t.Q;
				vertices[dt.v1].Q += t.Q;
				vertices[dt.v2].Q += t.Q;
				
				//create halfedges internal to the face and associate them with the face and their respective vertices
				e0 = new HalfEdge(vertices[dt.v1], t);
				e1 = new HalfEdge(vertices[dt.v2], t);
				e2 = new HalfEdge(vertices[dt.v0], t);

				//set the halfedge that each vertex points to
				//later assignments override earlier ones but that is fine
				vertices[dt.v0].e = e0;
				vertices[dt.v1].e = e1;
				vertices[dt.v2].e = e2;

				//set the the halfedge that the triangle points to
				t.e = e2;

				//link the halfedges together to loop
				e0.next = e1; e1.next = e2; e2.next = e0;
				e0.prev = e2; e2.prev = e1; e1.prev = e0;

				//find the opposite halfedge for each halfedge if it exists
				//if it isnt created yet then add these halfedges to the dictionary for later retrieval
				if (lookup.TryGetValue(e1.edge, out opp))
				{
					e1.opposite = opp;
					opp.opposite = e1;
					lookup.Remove(e1.edge);
				}
				else
				{
					lookup.Add(e1.edge, e1);
					edges.Add(e1.edge);
				}
				if (lookup.TryGetValue(e2.edge, out opp))
				{
					e2.opposite = opp;
					opp.opposite = e2;
					lookup.Remove(e2.edge);
				}
				else
				{
					lookup.Add(e2.edge, e2);
					edges.Add(e2.edge);
				}
				if (lookup.TryGetValue(e0.edge, out opp))
				{
					e0.opposite = opp;
					opp.opposite = e0;
					lookup.Remove(e0.edge);
				}
				else
				{
					lookup.Add(e0.edge, e0);
					edges.Add(e0.edge);
				}
				he.Add(e0); he.Add(e1); he.Add(e2);
			}

			Console.Out.WriteLine("Interior HalfEdges complete, took " + timer.ElapsedMilliseconds + " milliseconds");
			timer.Restart();				
			//for all of the halfedges that have no opposite currently
			Console.Out.WriteLine("Processing Exterior HalfEdges");
			foreach (HalfEdge e in lookup.Values)
			{
				if (e.opposite != null)
					continue;
				e0 = new HalfEdge(e.prev.vert, null);
				e.opposite = e0;
				e0.opposite = e;
				e.vert.e = e0;
				walkBoundary(e0);
			}
			Console.Out.WriteLine("Exterior HalfEdges complete, took " + timer.ElapsedMilliseconds + " milliseconds");
		}

		void walkBoundary(HalfEdge e)
		{
			//starting at boundary halfedge e, move around the next vertex until you find a halfedge with a null opposite
			HalfEdge e1, e2, e3;
			e2 = e3 = e;
			do
			{
				//walk the 'spokes' of the vertex to find the next halfedge that is opposite the boundary
				while (e2.opposite != null)
					//condition for closing out the boundary
					if (e2.opposite == e)
						goto breakout;
					else
						e2 = e2.opposite.prev;
				//if somehow we're at a defined halfedge that is boundary, we should leave
				if (e2.face == null)
					return;
				//our new boundary halfedge
				e1 = new HalfEdge(e2.prev.vert, null);
				e2.vert.e = e1;
				//set opposites
				e2.opposite = e1;
				e1.opposite = e2;
				//set next and previous from the last boundary we assigned
				e3.next = e1;
				e1.prev = e3;
				//move up one halfedge and start looking for the next
				e3 = e2 = e1;
			} while (true);
breakout:
			//close the boundary
			e2.opposite.prev = e3;
			e3.next = e2.opposite;
		}
			

		//clean redundant faces from a mesh that might otherwise make subdivision troublesome
		private void cleanMesh()
		{
			HashSet<DrawTriangle> faces = new HashSet<DrawTriangle>();
			int k;
			int[] v = new int[3];
			List<int> removal = new List<int>();

			DrawTriangle t, t1;
			for (int i = 0; i < drawtriangles.Length; i++)
			{
				t = drawtriangles[i];
				t1 = new DrawTriangle();
				t1.v0 = t.v0; t1.v1 = t.v1; t1.v2 = t.v2;
				if (t1.v0 > t1.v1)
				{ k = t1.v0; t1.v0 = t1.v1; t1.v1 = k; }
				if (t1.v1 > t1.v2)
				{ k = t1.v1; t1.v1 = t1.v2; t1.v2 = k; }
				if (t1.v0 > t1.v1)
				{ k = t1.v0; t1.v0 = t1.v1; t1.v1 = k; }
				if (faces.Contains(t1))
					removal.Add(i);
				else
					faces.Add(t1);
			}
			var tris = drawtriangles.ToList();

			//be careful to remove correct index watch for offset
			for(int i = removal.Count - 1; i >= 0; i--)
				tris.RemoveAt(removal[i]);
			drawtriangles = tris.ToArray();
		}

		private void loadVerts()
		{
			GL.BindVertexArray(vao);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Marshal.SizeOf(typeof(DrawVertex)) * drawvertices.Length), drawvertices, BufferUsageHint.StaticDraw);
			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		private void loadTris()
		{
			GL.BindVertexArray(vao);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Marshal.SizeOf(typeof(DrawTriangle)) * drawtriangles.Length), drawtriangles, BufferUsageHint.StaticDraw);
			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
		//set up model attributes and bind buffer for rendering
		public void firstLoad()
		{
			//calculate size of index and vertex buffers
			int vertSize = Marshal.SizeOf(typeof(DrawVertex));
			int triSize = Marshal.SizeOf(typeof(DrawTriangle));
			int vecSize = Marshal.SizeOf(typeof(Vector3));

			GL.GenVertexArrays(1, out vao);
			GL.BindVertexArray(vao);

			GL.GenBuffers(1, out vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertSize * drawvertices.Length), drawvertices, BufferUsageHint.StaticDraw);

			GL.GenBuffers(1, out ibo);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(triSize * drawtriangles.Length), drawtriangles, BufferUsageHint.StaticDraw);

			//position of target vertex
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertSize, 0);

			//position of second vertex in triangle
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertSize, vecSize);

			//position of third vertex in triangle
			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertSize, 2 * vecSize);

			//state is set, unbind objects so they are not modified.
			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		public void draw(int m)
		{
			//GL.UseProgram(p);
			GL.UniformMatrix4(m, false, ref transform);				
			GL.BindVertexArray(vao);
			GL.DrawElements(BeginMode.Triangles, drawtriangles.Length * 6, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);
		}
	}
}
