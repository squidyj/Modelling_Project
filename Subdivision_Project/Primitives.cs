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

		public class Mat4
		{
			public float[,] m = new float[4,4];
			//write operators
			//need to replace rows as well
			public Mat4()
			{}

			public Mat4(Mat4 old)
			{
				for(int i = 0; i < 4; i++)
					for(int j = 0; j < 4; j++)
						m[i,j] = old.m[i,j];
			}

			public Vector4 row1
			{
				get { return getRow(0); }
				set { setRow(0, value); }
			}
			public Vector4 row2
			{
				get { return getRow(1); }
				set { setRow(1, value); }
			}
			public Vector4 row3
			{
				get { return getRow(2); }
				set { setRow(2, value); }
			}
			public Vector4 row4
			{
				get { return getRow(3); }
				set { setRow(3, value); }
			}

			private Vector4 getRow(int n)
			{
				return new Vector4(m[n,0], m[n,1], m[n,2], m[n,3]);
			}

			private void setRow(int n, Vector4 v)
			{
				m[n,0] = v.X; m[n,1] = v.Y; m[n,2] = v.Z; m[n,3] = v.W;
			}

			public static Mat4 quadric(Vector4 v)
			{
				Mat4 temp = new Mat4();

				temp.m[0, 0] = v.X * v.X; temp.m[0, 1] = v.X * v.Y; temp.m[0, 2] = v.X * v.Z; temp.m[0, 3] = v.X * v.W;
				temp.m[1, 0] = v.Y * v.X; temp.m[1, 1] = v.Y * v.Y; temp.m[1, 2] = v.Y * v.Z; temp.m[1, 3] = v.Y * v.W;
				temp.m[2, 0] = v.Z * v.X; temp.m[2, 1] = v.Z * v.Y; temp.m[2, 2] = v.Z * v.Z; temp.m[2, 3] = v.Z * v.W;
				temp.m[3, 0] = v.W * v.X; temp.m[3, 1] = v.W * v.Y; temp.m[3, 2] = v.W * v.Z; temp.m[3, 3] = v.W * v.W;

				return temp;
			}

			public static Vector4 operator *(Vector4 v, Mat4 m1)
			{
				Vector4 temp = new Vector4();
				temp.X = v.X * m1.m[0,0] + v.Y * m1.m[1,0] + v.Z * m1.m[2,0] + v.W * m1.m[3,0];
				temp.Y = v.X * m1.m[0,1] + v.Y * m1.m[1,1] + v.Z * m1.m[2,1] + v.W * m1.m[3,1];
				temp.Z = v.X * m1.m[0,2] + v.Y * m1.m[1,2] + v.Z * m1.m[2,2] + v.W * m1.m[3,2];
				temp.W = v.X * m1.m[0,3] + v.Y * m1.m[1,3] + v.Z * m1.m[2,3] + v.W * m1.m[3,3];
				return temp;
			}

			public static Vector4 operator *(Mat4 m1, Vector4 v)
			{
				Vector4 temp = new Vector4();
				temp.X = v.X * m1.m[0,0] + v.Y * m1.m[0,1] + v.Z * m1.m[0,2] + v.W * m1.m[0,3];
				temp.Y = v.X * m1.m[1,0] + v.Y * m1.m[1,1] + v.Z * m1.m[1,2] + v.W * m1.m[1,3];
				temp.Z = v.X * m1.m[2,0] + v.Y * m1.m[2,1] + v.Z * m1.m[2,2] + v.W * m1.m[2,3];
				temp.W = v.X * m1.m[3,0] + v.Y * m1.m[3,1] + v.Z * m1.m[3,2] + v.W * m1.m[3,3];
				return temp;
			}

			public static Mat4 operator +(Mat4 m1, Mat4 m2)
			{
				Mat4 mat = new Mat4();
				for(int i = 0; i < 4; i++)
					for(int j = 0; j < 4; j++)
						mat.m[i,j] = m1.m[i,j] + m2.m[i,j];
				return mat;
			}
		}

		public class HalfEdge
		{
			public HalfEdge next;
			public HalfEdge prev;
			public HalfEdge opposite;
			public Triangle face;
			public Vertex vert;

			public HalfEdge(Vertex v, Triangle t)
			{
				face = t; vert = v;
			}
			public Pair edge
			{
				get
				{
					return new Pair(vert, prev.vert);
				}
			}


		}

		public class Triangle
		{
			public HalfEdge e;
			public Vector3 normal;
			public Mat4 Q;

			public Triangle(HalfEdge e) { this.e = e; }
			public Triangle(HalfEdge e, Vertex v0, Vertex v1, Vertex v2)
			{
				this.e = e;
				calcNormal(v0.pos, v1.pos, v2.pos); 
			}
			public Triangle(Vertex v0, Vertex v1, Vertex v2)
			{
				calcNormal(v0.pos, v1.pos, v2.pos);
				calcQ(v0.pos);
			}

			void calcQ(Vector3 v)
			{
				float d = -(v.X * normal.X + v.Y * normal.Y + v.Z * normal.Z);
				Vector4 row = new Vector4(v, d);
				Q = Mat4.quadric(row);
			}

			void calcNormal(Vector3 p0, Vector3 p1, Vector3 p2)
			{
				Vector3 v1 = p1 - p0;
				Vector3 v2 = p2 - p0;
				normal = Vector3.Normalize(Vector3.Cross(v1, v2));
			}

			public HashSet<Triangle> adjacent()
			{
				var temp = new HashSet<Triangle>();
				temp.Add(e.opposite.face);
				temp.Add(e.next.opposite.face);
				temp.Add(e.prev.opposite.face);
				temp.Remove(null);
				return temp;
			}

			public List<Vertex> vertices()
			{
				List<Vertex> temp = new List<Vertex>();
				temp.Add(e.vert);
				temp.Add(e.next.vert);
				temp.Add(e.prev.vert);
				return temp;
			}
		}

		public class Vertex
		{
			public HalfEdge e;
			public int n;
			public Vector3 pos;
			public Mat4 Q = new Mat4();

			public Vertex(DrawVertex v)
			{
				pos = v.pos;
			}
			public Vertex(Vector3 p, HalfEdge e)
			{
				pos = p;
				this.e = e;
			}
			public Vertex(Vertex v)
			{
				pos = v.pos;
			}
			public Vertex(Vector3 v)
			{
				pos = v;
			}

			public HashSet<Triangle> adjacentFaces()
			{
				HashSet<Triangle> temp = new HashSet<Triangle>();
 				HalfEdge e0 = e;
				do{
					temp.Add(e0.face);
					e0 = e0.next.opposite;
				}while(e0 != e);
				temp.Remove(null);
				return temp;
			}
			
			//should probably be cached
			//inefficient to calculate 
			public bool boundary()
			{
				HalfEdge e0 = e;
				do{
					if(e0.face == null)
						return true;
					e0 = e0.next.opposite;
				}while(e0 != e);
				return false;
			}

			public HashSet<Vertex> adjacentVertices()
			{
				HashSet<Vertex> temp = new HashSet<Vertex>();
 				HalfEdge e0 = e.opposite;
				do{
					temp.Add(e0.vert);
					e0 = e0.prev.opposite;
				}while(e0 != e);
				return temp;
			}
		}

		public class Pair : IEquatable<Pair>, IComparable<Pair>
		{
			public Vertex v1, v2;
			public Vector3 vbar;
			public float cost;
			public Mat4 Q;

			public Pair(Vertex v1, Vertex v2)
			{ this.v1 = v1; this.v2 = v2; }

			public void update()
			{
				Q = v1.Q + v2.Q;
				findVBar();
				calcCost();
			}
			
			public override int GetHashCode()
			{
				return v1.GetHashCode() ^ v2.GetHashCode();
			}

			public int CompareTo(Pair p)
			{
				int n = cost.CompareTo(p.cost);
 				if(n == 0)
					if(!this.Equals(p))
						return -1;
				return n;
			}

			public bool Equals(Pair p)
			{
				return (v1.Equals(p.v1) && v2.Equals(p.v2)) || (v1.Equals(p.v2) && v2.Equals(p.v1));	
			}

			public void findVBar()
			{
				Matrix4 qnot = new Matrix4();
				qnot.Row0 = Q.row1;
				qnot.Row1 = Q.row2;
				qnot.Row2 = Q.row3;
				qnot.Row3 = new Vector4(0, 0, 0, 1);
				qnot.Invert();
				vbar = new Vector3(qnot.Column3);
			}

			public void calcCost()
			{
				var v = new Vector4(vbar, 1);
				var v1 = v * Q;
				cost = Vector4.Dot(v, v1);
			}				
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DrawTriangle
		{
			public DrawTriangle(int i0, int i1, int i2)
			{
				v0 = i0; v1 = i1; v2 = i2;
				normal = new Vector3();
			}
			public DrawTriangle(int i0, int i1, int i2, Vector3 n)
			{
				v0 = i0; v1 = i1; v2 = i2; normal = n;
			}
			public int v0, v1, v2;
			public Vector3 normal;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DrawVertex
		{
			public DrawVertex(Vector3 v) { pos = v; }
			//doesn't assign halfedge
			public Vector3 pos;
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
