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
			vertices = new Vertex[8];
			vertices[0].vert = new Vector3(1f, 1f, 1f);
			vertices[1].vert = new Vector3(-1f, 1f, 1f);
			vertices[2].vert = new Vector3(1f, -1f, 1f);
			vertices[3].vert = new Vector3(1f, 1f, -1f);
			vertices[4].vert = new Vector3(-1f, -1f, 1f);
			vertices[5].vert = new Vector3(-1f, 1f, -1f);
			vertices[6].vert = new Vector3(1f, -1f, -1f); 
			vertices[7].vert = new Vector3(-1f, -1f, -1f);

			vertices[0].normal = new Vector3(1f, 1f, 1f);
			vertices[1].normal = new Vector3(-1f, 1f, 1f);
			vertices[2].normal = new Vector3(1f, -1f, 1f);
			vertices[3].normal = new Vector3(1f, 1f, -1f);
			vertices[4].normal = new Vector3(-1f, -1f, 1f);
			vertices[5].normal = new Vector3(-1f, 1f, -1f);
			vertices[6].normal = new Vector3(1f, -1f, -1f);
			vertices[7].normal = new Vector3(-1f, -1f, -1f);

			triangles = new Triangle[12];
			triangles[0] = new Triangle(4,1,0);
			triangles[1] = new Triangle(0, 4, 2);
			triangles[2] = new Triangle(0, 2, 6);
			triangles[3] = new Triangle(0, 6, 3);
			triangles[4] = new Triangle(0, 3, 5);
			triangles[5] = new Triangle(0, 5, 1);
			triangles[6] = new Triangle(7, 6, 3);
			triangles[7] = new Triangle(7, 3, 5);
			triangles[8] = new Triangle(7, 5, 1);
			triangles[9] = new Triangle(7, 1, 4);
			triangles[10] = new Triangle(7, 4, 2);
			triangles[11] = new Triangle(7, 2, 6);

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
			transform = Matrix4.Identity;

			load();
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
			int vertSize = vertices.Length * Marshal.SizeOf(typeof(Vertex));
			int triSize = triangles.Length * Marshal.SizeOf(typeof(Triangle));

			GL.GenVertexArrays(1, out vao);
			GL.BindVertexArray(vao);

			GL.GenBuffers(1, out vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)vertSize, vertices, BufferUsageHint.StaticDraw);

			GL.GenBuffers(1, out ibo);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)sizeof(int), triangles, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertSize, 0);

			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertSize, Marshal.SizeOf(typeof(Vector3)));

			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertSize, 2 * Marshal.SizeOf(typeof(Vector3)));

		
			GL.BindAttribLocation(program, 0, "position");
			GL.BindAttribLocation(program, 1, "normal");
			GL.BindAttribLocation(program, 2, "texcoord");
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