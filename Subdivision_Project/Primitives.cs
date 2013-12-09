using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using OpenTK;

namespace Subdivision_Project
{
	namespace Primitives
	{

		[StructLayout(LayoutKind.Sequential)]
		public struct Triangle
		{
			public Triangle(int i0, int i1, int i2)
			{
				v0 = i0; v1 = i1; v2 = i2;
			}
			public int v0, v1, v2;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Vertex
		{
			public Vertex(Vector3 v, Vector3 n, Vector2 t) { vert = v; normal = n; texcoord = t; }
			public static Vertex operator +(Vertex v1, Vertex v2) { return new Vertex(v1.vert + v2.vert, v1.normal + v2.normal, v1.texcoord + v2.texcoord); }
			public static Vertex operator *(float s, Vertex v) { return new Vertex(s * v.vert, s * v.normal, s * v.texcoord); }
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
	}
}
