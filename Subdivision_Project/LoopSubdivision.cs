using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subdivision_Project.Primitives;

namespace Subdivision_Project
{
	class LoopSubdivision
	{
		/*
		public static Mesh subdivide(Mesh m)
		{
		
			//begin by creating all the new edge vertices and maintaining the halfedge structure
		
			DrawTriangle t;
			for (int i = 0; i < m.Triangles.Length; i++)
			{
				t = m.Triangles[i];

				//construct a vertex on each edge
				v0 = edgeVertex(m, t.v0, t.v1, t.v2, mVerts, lookup);
				v1 = edgeVertex(m, t.v1, t.v2, t.v0, mVerts, lookup);
				v2 = edgeVertex(m, t.v2, t.v0, t.v1, mVerts, lookup);

				//construct the 4 new faces maintaining the original winding
				mTriangles.Add(new DrawTriangle(t.v0, v0, v2));
				mTriangles.Add(new DrawTriangle(v0, t.v1, v1));
				mTriangles.Add(new DrawTriangle(v0, v1, v2));
				mTriangles.Add(new DrawTriangle(v2, v1, t.v2));
			}

			DrawVertex v;
			int[] adj;
			HashSet<int> bound;
			for (int i = 0; i < off; i++)
			{
				if (m.boundary.Contains(i))
				{
					//find the vertices that are adjacent to the target vertex AND boundary m.Vertices
					bound = new HashSet<int>(m.aVertices[i].Intersect(m.boundary));
					//there should only be 2 such vertices, if something wickity wack is going on, better handle it
					adj = bound.ToArray();
					v = 0.125f * (m.Vertices[adj[0]] + m.Vertices[adj[1]]) + 0.75f * m.Vertices[i];
				}
				else
					v = findPosition(m, 1, i);

				mVerts[i] = v;
			}
			return new Mesh(mVerts.ToArray(), mTriangles.ToArray(), m.Box);
		}

		private static DrawVertex meanNeighbourhood(Mesh m, int n)
		{
			DrawVertex v = new DrawVertex();
			foreach (int i in m.aVertices[n])
			{
				v += m.Vertices[i];
			}
			return (1.0f / m.aVertices[n].Count * v);
		}

		private static int edgeVertex(Mesh m, int v0, int v1, int o1, List<DrawVertex> verts, Dictionary<DrawVertex, int> lookup)
		{
			//we are given two adjacent vertices and an opposite vertex, we need to find the final opposite vertex if it exists
			DrawVertex v;
			//the edge vertex is boundary if and only if its adjacent vertices are boundary
			if (m.boundary.Contains(v0) && m.boundary.Contains(v1))
				v = 0.5f * (verts[v0] + verts[v1]);
			else
			{
				//otherwise there is a second opposite vertex
				//find all of the vertices that are adjacent to both ends of the edge
				HashSet<int> hs = new HashSet<int>(m.aVertices[v0].Intersect(m.aVertices[v1]));
				//remove from that set the already gathered opposite
				hs.Remove(o1);
				if (hs.Count == 0)
					Console.Out.WriteLine(v0 + ", " + v1);
				int o2 = hs.Last();
				v = 0.375f * (verts[v0] + verts[v1]) + 0.125f * (verts[o1] + verts[o2]);
			}
			int n;
			if (lookup.TryGetValue(v, out n))
				return n;
			n = verts.Count;
			verts.Add(v);
			lookup.Add(v, n);
			return n;
		}

		//find the position of the kth additional subdivision of this vertex
		private static DrawVertex findPosition(Mesh m, int k, int i)
		{
			DrawVertex v;
			float alpha = findAlpha(m.aVertices[i].Count);
			float mu = (float)Math.Pow((0.625f - alpha), k);
			v = mu * m.Vertices[i] + (1 - mu) * findLimitPosition(m, i, alpha);
			return v;
		}

		private static DrawVertex findLimitPosition(Mesh m, int i, float alpha)
		{
			DrawVertex v;
			float beta = 1 / (1 + ((float)(8 / 3) + alpha));
			v = beta * m.Vertices[i] + (1 - beta) * meanNeighbourhood(m, i);
			return v;
		}

		private static float findAlpha(int n)
		{
			return ((float)(0.625f - Math.Pow((0.375f + 0.25f * Math.Cos(2 * Math.PI / n)), 2)));
		}

			*/
	}
}
