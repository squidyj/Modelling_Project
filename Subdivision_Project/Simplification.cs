using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Subdivision_Project.Primitives;
using OpenTK;
using System.Diagnostics;
namespace Subdivision_Project
{
	class Simplification
	{
		public static Matrix4 add(Matrix4 m, Matrix4 n)
		{
			return new Matrix4(
				m.M11 + n.M11, m.M12 + n.M12, m.M13 + n.M13, m.M14 + n.M14,
				m.M21 + n.M21, m.M22 + n.M22, m.M23 + n.M23, m.M24 + n.M24,
				m.M31 + n.M31, m.M32 + n.M32, m.M33 + n.M33, m.M34 + n.M34,
				m.M41 + n.M41, m.M42 + n.M42, m.M43 + n.M43, m.M44 + n.M44);
		}

		private static bool isEdge(Mesh m, int v1, int v2)
		{
			if (m.aVertices[v1].Contains(v2)) { return true; }
			else { return false; }
		}

		private static Matrix4[] computeQ(Mesh m)
		{
			int numVertices = m.Vertices.Length;

			Matrix4[] q = new Matrix4[numVertices];

			for (int i = 0; i < numVertices; i++)
			{
				Matrix4 qi = new Matrix4();

				foreach (int tri in m.aFaces[i])
				{
					Triangle target = m.Triangles[tri];

					// Construct Kp, the matrix
					// [ a^2 ab  ac  ad
					//   ab  b^2 bc  bd
					//   ac  bc  c^2 cd
					//   ad  bd  cd  d^2 ]

					Matrix4 kp = new Matrix4();
					Plane plane = new Plane(
						m.Vertices[target.v0].vert,
						m.Vertices[target.v1].vert,
						m.Vertices[target.v2].vert);

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

				q[i] = mult(qi, (1.0f / m.aFaces[i].Count));
			}
			return q;
		}

		public static Matrix4 mult(Matrix4 q, float s)
		{
			q.Row0 = q.Row0 * s;
			q.Row1 = q.Row1 * s;
			q.Row2 = q.Row2 * s;
			q.Row3 = q.Row3 * s;
			return q;
		}

		private static float calcDeltaV(Vector3 v, Matrix4 q)
		{
			//delta(v) = v^TQv

			float vx, vy, vz, vw;

			vx = v.X * q.M11 + v.Y * q.M21 + v.Z * q.M31 + 1.0f * q.M41;
			vy = v.X * q.M12 + v.Y * q.M22 + v.Z * q.M32 + 1.0f * q.M42;
			vz = v.X * q.M13 + v.Y * q.M23 + v.Z * q.M33 + 1.0f * q.M43;
			vw = v.X * q.M14 + v.Y * q.M24 + v.Z * q.M34 + 1.0f * q.M44;

			return vx * v.X + vy * v.Y + vz * v.Z + vw * 1.0f;
		}

		public static Vector4 matmult(Matrix4 m, Vector4 v)
		{
			float x, y, z, w;
			x = v.X * m.M11 + v.Y * m.M12 + v.Z * m.M13 + v.W * m.M14;
			y = v.X * m.M21 + v.Y * m.M22 + v.Z * m.M23 + v.W * m.M24;
			z = v.X * m.M31 + v.Y * m.M32 + v.Z * m.M33 + v.W * m.M34;
			w = v.X * m.M41 + v.Y * m.M42 + v.Z * m.M43 + v.W * m.M44;
			return new Vector4(x, y, z, w);
		}

		private static VertexPair vBarCalc(Mesh m, int v1s_index, int v2s_index, Matrix4[] q)
		{
			Matrix4 qbar = add(q[v1s_index], q[v2s_index]);

			Vertex v1 = m.Vertices[v1s_index];
			Vertex v2 = m.Vertices[v2s_index];

			// depending on which one of these produces the lowest value of delta (v bar)

			// TODO: Check if the normals and textures for the inverse calculated vbar are correct

			Matrix4 inverse = qbar;
			inverse.Row3 = new Vector4(0, 0, 0, 1);
			inverse.Invert();

			Vertex vBar = new Vertex();
			float cost;

			//unnecessary, Invert throws an exception if the matrix is not invertible
			//should implement try catch
			if (Matrix4.Mult(qbar, inverse).Equals(Matrix4.Identity))
			{
			/*	Vector3 newPoint = new Vector3(inverse.Row3.X, inverse.Row3.Y, inverse.Row3.Z);
				float weight = ((newPoint - v1.vert).Length) / ((v2.vert - v1.vert).Length);
				vBar = new Vertex(newPoint, v1.normal * weight + v2.normal * (1 - weight), v1.texcoord * weight + v2.texcoord * (1 - weight));
				cost = calcDeltaV(vBar.vert, qbar);
			
			 */
				vBar.vert = new Vector3(matmult(inverse, new Vector4(0, 0, 0, 1)));
				cost = calcDeltaV(vBar.vert, qbar);
			}
			 
			else
			{
				vBar = 0.5f * (v1 + v2);
				cost = calcDeltaV(vBar.vert, qbar);

				float newCost;
				if ((newCost = calcDeltaV(v1.vert, qbar)) < cost)
				{
					cost = newCost;
					vBar = v1;
				}

				if ((newCost = calcDeltaV(v2.vert, qbar)) < cost)
				{
					cost = newCost;
					vBar = v2;
				}
			}

			return new VertexPair(v1s_index, v2s_index, cost, vBar);
		}

		private static SortedSet<VertexPair> updateCosts(Mesh m, VertexPair updatePair, SortedSet<VertexPair> pairHeap, Matrix4[] q)
		{
			List<VertexPair> updateVertices = new List<VertexPair>();

			foreach (VertexPair v in pairHeap)
			{
				if (v.v1 == updatePair.v2)
				{
					updateVertices.Add(new VertexPair(updatePair.v2, v.v2, v.cost, v.newVertex));
				}
				else if (v.v2 == updatePair.v2)
				{
					updateVertices.Add(new VertexPair(v.v1, updatePair.v2, v.cost, v.newVertex));
				}
				if (v.v1 == updatePair.v1 || v.v2 == updatePair.v1)
				{
					updateVertices.Add(v);
				}
			}

			foreach (VertexPair v in updateVertices)
			{
				pairHeap.Remove(v);
				pairHeap.Add(vBarCalc(m, v.v1, v.v2, q));
			}

			return pairHeap;
		}

		public static Mesh simplify(Mesh m, int targetTris, float threshold)
		{
			Console.Out.WriteLine("Begin surface simplification!");
			Console.Out.WriteLine("Currently the model has " + m.Triangles.Length + " triangles.");

			if (m.Triangles.Length < targetTris)
			{
				Console.Out.WriteLine("The target number of triangles specified is larger than the amount of the triangles the model already has!");
				return null;
			}

			Stopwatch overallTime = new Stopwatch();
			Stopwatch timer = new Stopwatch();

			overallTime.Restart();
			Console.Out.WriteLine("We will now attempt to reduce the model to " + targetTris + " triangles.");

			List<Triangle> mTriangles = m.Triangles.ToList();
			List<Vertex> mVerts = m.Vertices.ToList();

			Console.Out.WriteLine("Now deriving error quadrics...");

			timer.Restart();
			// 1. Compute the Q matrices for all the initial vertices.
			Matrix4[] q = computeQ(m);
			timer.Stop();
			Console.Out.WriteLine("Error quadrics derived! Time taken: " + timer.ElapsedMilliseconds + "ms");

			SortedSet<int> removedVerts = new SortedSet<int>();
			SortedSet<int> removedTris = new SortedSet<int>();

			while ((mTriangles.Count - removedTris.Count) > targetTris)
			{
				timer.Restart();
				Console.Out.WriteLine("Now selecting valid pairs...");
				// 2. Select all valid pairs.
				SortedSet<VertexPair> validPairs = new SortedSet<VertexPair>();

				for (int i = 0; i < mVerts.Count; i++)
				{
					for (int j = i + 1; j < mVerts.Count; j++)
					{
						if (i != j)
						{
							if (isEdge(m, i, j) || (mVerts[i].vert - mVerts[j].vert).Length < threshold)
							{
								// 3. Compute the optimal contraction target vbar for each valid pair (v_1, v_2).
								// The error vbar^T(Q_1+Q_2)vbar of this target vertex becomes the cost of contracting that pair
								// 4. Place all the pairs in a heap keyed on cost with the minimum cost pair at the top.
								validPairs.Add(vBarCalc(m, i, j, q));
							}
						}
					}
				}
				timer.Stop();
				Console.Out.WriteLine("Valid pairs selected! Time taken: " + timer.ElapsedMilliseconds + "ms");

				// 5. Iteratively remove the pair (v_1, v_2) of least cost from heap, contract this pair,
				// and update the costs of all valid pairs involving v_1.

				timer.Restart();
				Console.Out.WriteLine("Now contracting pairs...");
				while (validPairs.Count != 0 && (mTriangles.Count - removedTris.Count) > targetTris)
				{
					VertexPair removePair = validPairs.First();
					validPairs.Remove(removePair);

					// Remember to remove this vertex
					removedVerts.Add(removePair.v2);

					// Replace v_1 with the new vertex, vbar
					mVerts[removePair.v1] = removePair.newVertex;

					foreach (int tri in m.aFaces[removePair.v2])
					{
						// If the triangle hasn't been removed yet...
						if (!removedTris.Contains(tri))
						{
							int v0 = mTriangles[tri].v0;
							int v1 = mTriangles[tri].v1;
							int v2 = mTriangles[tri].v2;

							// THIS CONDITION IS REALLY IMPORTANT. IT MAKES IT SO YOU DON'T MAKE HOLES.
							// I DON'T KNOW WHY THIS ISN'T ALWAYS THE CASE, BUT W/E.
							if (v0 == removePair.v2 ||
								v1 == removePair.v2 ||
								v2 == removePair.v2)
							{
								// If any triangle has both vertices in the pair...
								if (v0 == removePair.v1 ||
									v1 == removePair.v1 ||
									v2 == removePair.v1)
								{
									// TODO: Do something about that

									// This triangle is no longer used. It's not adjacent to anything.
									// So disconnect the other points from the triangle
									if (v0 == removePair.v2)
									{
										m.aFaces[v1].Remove(tri);
										m.aFaces[v2].Remove(tri);
									}
									else if (v1 == removePair.v2)
									{
										m.aFaces[v0].Remove(tri);
										m.aFaces[v2].Remove(tri);
									}
									else
									{
										m.aFaces[v0].Remove(tri);
										m.aFaces[v1].Remove(tri);
									}

									// Remember to remove this triangle
									removedTris.Add(tri);
								}
								// Else replace v_2 with v_1
								else
								{
									if (v0 == removePair.v2)
									{
										v0 = removePair.v1;
										m.addAdjacent(v0, v1, v2, tri);
									}
									else if (v1 == removePair.v2)
									{
										v1 = removePair.v1;
										m.addAdjacent(v1, v0, v2, tri);
									}
									else if (v2 == removePair.v2)
									{
										v2 = removePair.v1;
										m.addAdjacent(v2, v0, v1, tri);
									}

									// update triangle with new vertices
									mTriangles[tri] = new Triangle(v0, v1, v2);
								}
							}
						}
					}
					validPairs = updateCosts(m, removePair, validPairs, q);
				}
				timer.Stop();
				Console.Out.WriteLine("Pairs contracted! Time taken: " + timer.ElapsedMilliseconds + "ms");
			}


			timer.Restart();
			Console.Out.WriteLine("Now removing unused triangles and vertices...");

			while (removedTris.Count > 0)
			{
				int tri = removedTris.Last();
				removedTris.Remove(tri);
				mTriangles.RemoveAt(tri);
			}

			Triangle[] nTriangles = mTriangles.ToArray();
			/*                
							while (removedVerts.Count > 0)
							{
								int v = removedVerts.Last();

								removedVerts.Remove(v);
								mVerts.RemoveAt(v);

								// Update triangles
								for (int j = 0; j < nTriangles.Length; j++)
								{
									Triangle tri = mTriangles[j];
									int v0 = tri.v0;
									int v1 = tri.v1;
									int v2 = tri.v2;

									bool update = false;

									if (tri.v0 > v)
									{
										v0--;
										update = true;
									}
									if (tri.v1 > v)
									{
										v1--;
										update = true;
									}
									if (tri.v2 > v)
									{
										v2--;
										update = true;
									}
									if (update)
									{
										mTriangles[j] = new Triangle(v0, v1, v2);
									}
								}
							}
			*/
			timer.Stop();
			overallTime.Stop();
			Console.Out.WriteLine("Surface simplification complete!");
			Console.Out.WriteLine("The model was reduced from " + m.Triangles.Length + " to " + mTriangles.Count + " triangles in " + overallTime.ElapsedMilliseconds + "ms");

			//triangles = nTriangles;
			return new Mesh(mVerts.ToArray(), mTriangles.ToArray(), m.Box);
			
		}


		public class Plane
		{
			// Create a plane given three vertices
			public Plane(Vector3 tri_v1, Vector3 tri_v2, Vector3 tri_v3)
			{
				Vector3 ab = tri_v2 - tri_v1;
				Vector3 ac = tri_v3 - tri_v1;
				Vector3 plane = Vector3.Normalize(Vector3.Cross(ab, ac));
				a = plane.X;
				b = plane.Y;
				c = plane.Z;

				d = -((a * tri_v1.X) + (b * tri_v1.Y) + (c * tri_v1.Z));
			}
			public float a, b, c, d;
		}

		//internal class VertexPair
        public class VertexPair : IComparable
        {
            // Create a pair given two vertices
            public VertexPair(int vertex1, int vertex2)
            {
                v1 = vertex1;
                v2 = vertex2;
                cost = -1;
                newVertex = new Vertex();
            }
            // Keep track of an already calculated cost
            public VertexPair(int vertex1, int vertex2, float theCost, Vertex theNewVertex)
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
            public Vertex newVertex;
            public float cost;
        }
	
	}
}
