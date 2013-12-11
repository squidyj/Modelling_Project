using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Subdivision_Project.Primitives;
using System.Diagnostics;


//give it a try using half-edge to help
namespace Subdivision_Project
{
	class Simple
	{
		public static Mesh simplify(Mesh m, int targetTris, float threshold)
		{
            Stopwatch timer = new Stopwatch();
            int numOfTris = m.triangles.Count();

			//then the valid pairs
            Console.Out.Write("Finding valid pairs...");
            timer.Restart();
			SortedSet<Pair> validPairs = findValidPairs(m, threshold);
            timer.Stop();
            Console.Out.WriteLine(timer.ElapsedMilliseconds + "ms");

            //then the costs for each contraction
            Console.Out.Write("Calculating costs...");
            timer.Restart();
            validPairs = calcCosts(m, validPairs);
            timer.Stop();
            Console.Out.WriteLine(timer.ElapsedMilliseconds + "ms");

			//loop until enough triangles have been removed 
			//contract the lowest cost pair and remove it from the heap
            Console.Out.Write("Contracting pairs...");
            timer.Restart();
            while (m.triangles.Count() > targetTris && validPairs.Count > 0)
            {
                Pair p = validPairs.First();
                validPairs.Remove(p);
                m = contract(m, p);
                validPairs = updateCosts(m, validPairs, p);
            }
            timer.Stop();
            Console.Out.WriteLine(timer.ElapsedMilliseconds + "ms");

			//update the valid pairs to point to the newly created vertex where applicable
			//update the costs of those valid pairs

            m.reconstruct();

            Console.Out.WriteLine("Simplified mesh from " + numOfTris + " triangles to " + m.triangles.Count() + " triangles!");

			return m;
		}

        // Wait is this already done in vertex as adjacent vertices?!
        // TODO: Check logic
        // Edit: Looks good. I checked with testgraph.obj and outputted the positions of the vertex pairs.
        // TODO: Question: Can an edge not have an opposite?
        public static bool isEdge(Vertex v1, Vertex v2)
        {
            HalfEdge firstEdge = v1.e;
            HalfEdge nextEdge = firstEdge;

            Console.Out.WriteLine("First edge: " + firstEdge.vert.n + " to " + firstEdge.opposite.vert.n + " = " + firstEdge.next.vert.n);

            Console.Out.WriteLine("First edge is part of triangle: (" + firstEdge.vert.n + ", " + firstEdge.next.vert.n + ", " + firstEdge.prev.vert.n + ")");
            Console.Out.WriteLine("First edge opposite is part of triangle: (" + firstEdge.opposite.vert.n + ", " + firstEdge.opposite.next.vert.n + ", " + firstEdge.opposite.prev.vert.n + ")");

            Console.Out.WriteLine("Checking neighbours of v1: " + v1.n + " for v2: " + v2.n);

            do
            {
                Console.Out.WriteLine("v1: " + v1.n + " has an edge: " + nextEdge.vert.n + " to " + nextEdge.opposite.vert.n + " = " + nextEdge.next.vert.n);

                if (nextEdge.next.vert.n == v2.n) { return true; }
                else { nextEdge = nextEdge.opposite.next; }
            }
            while (nextEdge != firstEdge);

            return false;
        }

		public static SortedSet<Pair> findValidPairs(Mesh m, float threshold)
		{
			//for every vertex in the mesh, v1
			//for every vertex in the mesh v2 != v1
			//if the distance between v1 and v2 is less than threshold or if the two vertices share an edge
			//they are a valid pair and are added to the list of valid pairs

            SortedSet<Pair> validpairs = new SortedSet<Pair>();
            int numOfVertices = m.vertices.Count();
            Vertex v1, v2;

            for (int i = 0; i < numOfVertices; i++)
            {
                for (int j = i + 1; j < numOfVertices; j++)
                {
                    v1 = m.vertices[i];
                    v2 = m.vertices[j];

                    if (isEdge(v1, v2) || ((v1.pos - v2.pos).Length < threshold))
                    {
                        validpairs.Add(new Pair(v1, v2));
                    }
                }
            }

            return validpairs;
		}

        // TODO: Make sure update is working
		public static SortedSet<Pair> calcCosts(Mesh m, SortedSet<Pair> validPairs)
		{
			//for every valid pair
			//take the sum of the quadric matrix for the two vertices
			//find the optimal vertex position, vbar
			//find the cost associated with that position

            SortedSet<Pair> updatedPairs = new SortedSet<Pair>();

            foreach (Pair p in validPairs)
            {
                p.update();
                updatedPairs.Add(p);
            }

            return updatedPairs;
		}

        // TODO: Complete this        
        public static SortedSet<Pair> updateCosts(Mesh m, SortedSet<Pair> validPairs, Pair p)
        {
            SortedSet<Pair> updatedPairs = new SortedSet<Pair>();
            Pair newPair;

            foreach (Pair updatePair in validPairs)
            {
                if (p.v1 == p.v2)
                {
                    throw new Exception("A pair with a vertex and itself has been given as input!");
                }

                else if (updatePair.v1 == updatePair.v2)
                {
                    throw new Exception("A pair with a vertex and itself has been found!");
                }

                else if ((updatePair.v1 == p.v1 && updatePair.v2 == p.v2) ||
                    (updatePair.v1 == p.v2 && updatePair.v2 == p.v1))
                {
                    throw new Exception("A pair with the same vertices as the removed pair was found!");
                }

                else
                {
                    newPair = updatePair;

                    if (updatePair.v1 == p.v1 || updatePair.v2 == p.v1)
                    {
                        newPair.update();
                    }

                    else if (updatePair.v1 == p.v2)
                    {
                        newPair.v1 = p.v1;
                        newPair.update();
                    }

                    else if (updatePair.v2 == p.v2)
                    {
                        newPair.v2 = p.v1;
                        newPair.update();
                    }

                    updatedPairs.Add(newPair);
                }
            }
            return updatedPairs;
        }

        // TODO: Make sure to check logic! This one is prone to errors!
		public static Mesh contract(Mesh m, Pair p)
		{
			//set the position of p.v1 to vbar
			//every edge to p.v2 must become an edge to p.v1
			//delete all degenerate triangles

            m.vertices[p.v1.n].pos = p.vbar;

            HalfEdge firstEdge = p.v2.e;

            Console.Out.WriteLine();

            Console.Out.WriteLine("FirstEdge: " + firstEdge.vert.n + " to " + firstEdge.opposite.vert.n);

            Console.Out.WriteLine("FirstEdge.next: " + firstEdge.next.vert.n + " to " + firstEdge.next.opposite.vert.n);
            Console.Out.WriteLine("FirstEdge.prev: " + firstEdge.prev.vert.n + " to " + firstEdge.prev.opposite.vert.n);

            Console.Out.WriteLine("FirstEdge.opposite.next: " + firstEdge.opposite.next.vert.n + " to " + firstEdge.opposite.next.opposite.vert.n);
            Console.Out.WriteLine("FirstEdge.opposite.prev: " + firstEdge.opposite.prev.vert.n + " to " + firstEdge.opposite.prev.opposite.vert.n);

            HalfEdge nextEdge = firstEdge;
            HalfEdge combineEdge1;
            HalfEdge combineEdge2;

            List<HalfEdge> edges = new List<HalfEdge>();

            Console.Out.WriteLine("Finding neighbours of v2: " + p.v2.n);

            do
            {
                Console.Out.WriteLine("Edge : " + nextEdge.vert.n + " to " + nextEdge.opposite.vert.n);
                edges.Add(nextEdge);
                // nextEdge = nextEdge.next.opposite;
            }
            while (nextEdge != firstEdge);

            foreach (HalfEdge e in edges)
            {
                // TODO: Does this deal with non-manifest and boundaries?
                // ...or work at all?
                if (e.opposite.vert == p.v1)
                {
                    combineEdge1 = e.prev.opposite;
                    combineEdge2 = e.next.opposite;

                    combineEdge1.vert = p.v1;

                    p.v1.e = combineEdge1;

                    combineEdge1.opposite = combineEdge2;
                    combineEdge2.opposite = combineEdge1;
                    
                    m.triangles.Remove(e.face);
                }
                else if (e.next.opposite.vert == p.v1)
                {
                    combineEdge1 = e.next.opposite;
                    combineEdge2 = e.opposite;

                    p.v1.e = combineEdge1;

                    combineEdge1.opposite = combineEdge2;
                    combineEdge2.opposite = combineEdge1;
                    
                    m.triangles.Remove(e.face);
                }
                e.vert = p.v1;
            }

            return m;
		}
	}
}
