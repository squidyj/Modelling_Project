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

		//index array
		Triangle[] triangles;
		public Triangle[] Triangles
		{
			get { return triangles; }
			set { triangles = value; }
		}

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

			calculateBox();
			load();
		}

		//load model data into memory from file with a given pathname
		public Model(string filename, int p)
		{
			program = p;
			//load and interpret file format
			ObjLoader.Load(this, filename);
			//calculate the bounding box, potentially rewrite vertex values
			calculateBox();
			//create required opengl objects
			face();
			transform = Matrix4.Identity;

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
				v1 = vertices[t.i1].vert - vertices[t.i0].vert;
				v2 = vertices[t.i2].vert - vertices[t.i0].vert;
				norm = Vector3.Normalize(Vector3.Cross(v1, v2));
				vertices[t.i0].normal += norm;
				vertices[t.i1].normal += norm;
				vertices[t.i2].normal += norm;
				count[t.i0]++; count[t.i1]++; count[t.i2]++;
			}

			for(int i = 0; i < vertices.Length; i++)
			{
				vertices[i].normal /= count[i];
				vertices[i].normal = Vector3.Normalize(vertices[i].normal);
			}
		}


		//naive calculation of a bounding box for the model
		private void calculateBox()
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

			Console.Out.WriteLine(center);
			Console.Out.WriteLine(min);
			Console.Out.WriteLine(max);
			Console.Out.WriteLine(vertices.Length);
			Console.Out.WriteLine(triangles.Length);
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

		//generate the control mesh and displacement map, save to file, create new displacement mapped model
		public Model subdivide()
		{
			//generate a control mesh
			//
			return null;
		}

		//draw the model
		public void draw()
		{

			Console.Out.WriteLine("Locations: " + vao + ", " + vbo + ", " + ibo);
			GL.BindVertexArray(vao);
			GL.DrawElements(BeginMode.Triangles, triangles.Length *3, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Triangle
		{
			public Triangle(int n0, int n1, int n2)
			{
				i0 = n0; i1 = n1; i2 = n2;
			}
			public int i0;
			public int i1;
			public int i2;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Vertex
		{
			public Vertex(Vector3 v, Vector3 n, Vector2 t)
			{
				vert = v; normal = n; texcoord = t;
			}
			public Vector3 vert;
			public Vector3 normal;
			public Vector2 texcoord;
		}
	}
}