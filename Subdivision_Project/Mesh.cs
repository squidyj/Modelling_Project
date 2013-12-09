using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using Subdivision_Project.Primitives;
namespace Subdivision_Project
{
	public class Mesh
	{
		//vertex attribute object, vertex buffer object, and index buffer object locations
		int vao, vbo, ibo;

		Material mat;
		//rename to adjacentVertices
		public HashSet<int>[] aVertices;
		//rename to AdjacentFaces
		public HashSet<int>[] aFaces;
		//rename to boundaryVertices
		public HashSet<int> boundary;
		
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

		Triangle[] triangles;
		public Triangle[] Triangles
		{
			get { return triangles; }
			set { triangles = value; }
		}

		Vertex[] vertices;
		public Vertex[] Vertices
		{
			get { return vertices; }
			set { vertices = value; }
		}

		public Mesh(string pathname)
		{
			//load the model data from file
			//TODO change objloader to work with mesh type object
			ObjLoader.Load(this, pathname);
			//sweep redundant triangles
			cleanMesh();
			bruteForceNormal();
			makeAdjacency();
			box = new BoundingBox(this);
			load();
			Console.Out.WriteLine("Vertices: " + vertices.Length);
			Console.Out.WriteLine("Faces: " + triangles.Length);
		}

		public Mesh(Vertex[] verts, Triangle[] tris, BoundingBox b)
		{
			vertices = verts;
			triangles = tris;
			box = b;
			//should be updated while the modification is ongoing
			makeAdjacency();
			load();
		}

		public void reset()
		{transform = Matrix4.Identity * box.orientMesh();}

		private void bruteForceNormal()
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

			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i].normal /= count[i];
				vertices[i].normal = Vector3.Normalize(vertices[i].normal);
			}
		}

		//clean redundant faces from a mesh that might otherwise make subdivision troublesome
		private void cleanMesh()
		{
			HashSet<Triangle> faces = new HashSet<Triangle>();
			int k;
			int[] v = new int[3];
			List<int> removal = new List<int>();

			Triangle t, t1;
			for (int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				t1 = new Triangle();
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
			var tris = triangles.ToList();

			//be careful to remove correct index watch for offset
			foreach (int n in removal)
				tris.RemoveAt(n);
			triangles = tris.ToArray();
		}

		//only calculates vertices adjacency to faces
		public void makeFaceAdjacency()
		{
			HashSet<int>[] hs = new HashSet<int>[vertices.Length];
			for (int i = 0; i < hs.Length; i++)
				hs[i] = new HashSet<int>();
			Triangle t;
			for (int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				hs[t.v0].Add(i);
				hs[t.v1].Add(i);
				hs[t.v2].Add(i);
			}
			aFaces = hs;		
		}

		//calculates adjacency information for the mesh
		public void makeAdjacency()
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
			for (int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				addAdjacent(t.v0, t.v1, t.v2, i);
				addAdjacent(t.v1, t.v2, t.v0, i);
				addAdjacent(t.v2, t.v0, t.v1, i);
			}
			for (int i = 0; i < triangles.Length; i++)
			{
				t = triangles[i];
				addBoundary(t.v0, t.v1, i);
				addBoundary(t.v1, t.v2, i);
				addBoundary(t.v2, t.v0, i);
			}
		}


		public void addAdjacent(int v, int v1, int v2, int f)
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
			if (bound.Count > 1)
				return;
			//otherwise the edge is a boundary and both vertices are boundary vertices
			boundary.Add(v1);
			boundary.Add(v2);
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
		}

		public void draw(int p)
		{
			//GL.UseProgram(p);
			GL.BindVertexArray(vao);
			GL.DrawElements(BeginMode.Triangles, triangles.Length * 3, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);
		}
	}
}
