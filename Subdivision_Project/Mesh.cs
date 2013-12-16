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
		Form1 own;
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

		public Mesh(string pathname, Form1 f)
		{
			//load the model data from file
			//TODO change objloader to work with mesh type object

			own = f;
			Stopwatch overall = new Stopwatch();
			overall.Start();
			Stopwatch timer = new Stopwatch();
			ObjLoader.Load(this, pathname);
			//sweep redundant triangles
			cleanMesh();		
			box = new BoundingBox(this);

			timer.Restart();
			initHalfEdge();
			foreach(Vertex v in vertices)
			own.textBox1.Clear();
			own.textBox1.Text = "Half-Edge initalized in " + timer.ElapsedMilliseconds + " milliseconds.\n\n";

			setVerts();
			reset();
			firstLoad();
			//pairlist();


			foreach (Vertex v in vertices)
			{
					Debug.Assert(v.pairs.IsSubsetOf(v.ePairs()), "Pairs has extra elements");
					Debug.Assert(v.ePairs().IsSubsetOf(v.pairs), "Pairs is missing elements");
			}

			own.textBox1.AppendText("Model took " + overall.ElapsedMilliseconds + " milliseconds to load\n");
			own.textBox1.AppendText("Vertices: " + drawvertices.Length + "\n");
			own.textBox1.AppendText("Faces: " + drawtriangles.Length + "\n");
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

		private void pairlist()
		{
			HalfEdge e;
			foreach (Vertex v in vertices)
			{
				e = v.e;
				do
				{
					v.pairs.Add(e.edge);
					e = e.opposite.next;
				} while (e != v.e);
			}
		}
		//calculates Q matrices for all triangles and Verteices
		//generates half-edge data structure
		//generates a set of edges that exist in the mesh as a starting point for pair contraction
		private void initHalfEdge()
		{
			var he = new HashSet<HalfEdge>();
			Dictionary<Pair, HalfEdge> lookup = new Dictionary<Pair, HalfEdge>();
			edges = new HashSet<Pair>();
			HalfEdge e0, e1, e2, opp;
			Triangle t;
			Pair p1, p2;
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
				//e0 is v0->v1
				e0 = new HalfEdge(vertices[dt.v1], t);
				//v1->v2
				e1 = new HalfEdge(vertices[dt.v2], t);
				//v2->v0
				e2 = new HalfEdge(vertices[dt.v0], t);

				//set the halfedge that each vertex points to
				//later assignments override earlier ones but that is fine
				vertices[dt.v0].e = e0;
				vertices[dt.v1].e = e1;
				vertices[dt.v2].e = e2;

				//set the the halfedge that the triangle points to
				t.e = e0;

				//link the halfedges together to loop
				e0.next = e1; e1.next = e2; e2.next = e0;
				e0.prev = e2; e2.prev = e1; e1.prev = e0;

				e0.edge = new Pair(e2.vert, e0.vert);
				e1.edge = new Pair(e0.vert, e1.vert);
				e2.edge = new Pair(e1.vert, e2.vert);


				edges.Add(e0.edge); edges.Add(e1.edge); edges.Add(e2.edge);
				//see if there is an unpeaired halfedge in the data structure already
				//if it doesn't exist then add this one to be found by it's opposite
				if (lookup.TryGetValue(e1.edge, out opp))
				{
					e1.opposite = opp;
					opp.opposite = e1;
					//paired off, remove the halfedge with that edge
					lookup.Remove(e1.edge);
				}
				else
				{
					lookup.Add(e1.edge, e1);
					edges.Add(e1.edge);
					e1.vert.pairs.Add(e1.edge);
					e0.vert.pairs.Add(e1.edge);
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
					e2.vert.pairs.Add(e2.edge);
					e1.vert.pairs.Add(e2.edge);
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
					e0.vert.pairs.Add(e0.edge);
					e2.vert.pairs.Add(e0.edge);
				}
				he.Add(e0); he.Add(e1); he.Add(e2);
			}

			//after all of the triangles have been processed
			//all of the halfedge values remaining in the dictionary should be halfedges that are opposite to a boundary
			foreach (HalfEdge e in lookup.Values)
			{
				if (e.opposite != null)
					continue;
				e0 = new HalfEdge(e.prev.vert, null);			
				e0.edge = e.edge;
				e.opposite = e0;
				e0.opposite = e;
				he.Add(e0);
				e.vert.e = e0;
				walkBoundary(e0, he);
			}

			debugHalfEdges(he);
		}

		private void debugHalfEdges(HashSet<HalfEdge> he)
		{
			foreach (HalfEdge e in he)
			{
				Debug.Assert(e.opposite.opposite == e, "Opposite Initialization Failed");
				Debug.Assert(e.prev.next == e, "Prev Initialization Failed");
				Debug.Assert(e.next.prev == e, "Next Initialization Failed");
				Debug.Assert(e.vert == e.opposite.prev.vert, "Vertex Equality Failed");
				Debug.Assert(e.vert == e.next.opposite.vert, "Vertex Equality Failed");
			}
		}

		void walkBoundary(HalfEdge e, HashSet<HalfEdge> he)
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
				e1.edge = e2.edge;
				
				e2.vert.e = e1;
				//set opposites
				e2.opposite = e1;
				e1.opposite = e2;
				//set next and previous from the last boundary we assigned
				e3.next = e1;
				e1.prev = e3;
				he.Add(e1);
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
