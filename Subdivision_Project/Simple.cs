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

            Console.Out.WriteLine("Now simplifying to at most " + targetTris + " triangles");

			//then the valid pairs
            Console.Out.Write("Finding valid pairs...");
            /*timer.Restart();
			SortedSet<Pair> validPairs = findValidPairs(m, threshold);
            timer.Stop();
            */
			foreach (Pair p1 in m.edges)
				p1.update();
			SortedSet<Pair> validPairs = new SortedSet<Pair>(m.edges);
            
			 Console.Out.WriteLine(timer.ElapsedMilliseconds + "ms");

            //then the costs for each contraction
            Console.Out.Write("Calculating costs...");
            timer.Restart();
            timer.Stop();
            Console.Out.WriteLine(timer.ElapsedMilliseconds + "ms");

			//loop until enough triangles have been removed 
			//contract the lowest cost pair and remove it from the heap
            Console.Out.Write("Contracting pairs...");
            timer.Restart();
            Console.Out.WriteLine();
			Pair p;
			while (m.triangles.Count() > targetTris && validPairs.Count > 0)
            {
                p = validPairs.First();
				/*
                Console.Out.WriteLine("Now attempting to contract pair: (" + p.v1.n + ", " + p.v2.n + ")");
                Console.Out.WriteLine("The pair is in validPairs: " + validPairs.Contains(p));
 */
 */
                validPairs.Remove(p);
//                Console.Out.WriteLine("The pair is in validPairs: " + validPairs.Contains(p));
                m = contract(m, p);
				Console.Out.Write(validPairs.Count + "->");
                validPairs = updateCosts(m, validPairs, p);
				Console.Out.WriteLine(validPairs.Count);
//              Console.Out.WriteLine("Contracted pair (" + p.v1.n + ", " + p.v2.n + ")");
            }
            timer.Stop();
            Console.Out.WriteLine(timer.ElapsedMilliseconds + "ms");

			//update the valid pairs to point to the newly created vertex where applicable
			//update the costs of those valid pairs

            m.reconstruct();

            Console.Out.WriteLine("Simplified mesh from " + numOfTris + " triangles to " + m.triangles.Count() + " triangles!");

			return m;
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
            Pair newPair;

            for (int i = 0; i < numOfVertices; i++)
            {
                v1 = m.vertices[i];

                for (int j = i + 1; j < numOfVertices; j++)
                {
                    v2 = m.vertices[j];

                    newPair = new Pair(v1, v2);

                    if (m.edges.Contains(newPair) || ((v1.pos - v2.pos).Length < threshold))
                    {
                        newPair.update();
                        validpairs.Add(newPair);
                    }
                }
            }

            return validpairs;
		}

        // TODO: Complete this        
        public static SortedSet<Pair> updateCosts(Mesh m, SortedSet<Pair> validPairs, Pair p)
        {
            SortedSet<Pair> updatedPairs = new SortedSet<Pair>();
            Pair newPair;
			bool modified;
			var pairs = validPairs.ToList();
			for(int i = 0; i < pairs.Count; i++)
			{
				modified = false;
                if (pairs[i].v1 == p.v2) { pairs[i].v1 = p.v1; modified = true;}
                if (pairs[i].v2 == p.v2) { pairs[i].v2 = p.v1; modified = true;}
				if(modified)
				{
					validPairs.Remove(pairs[i]);
					if(pairs[i].v1 != pairs[i].v2)
					{
						pairs[i].update();
						validPairs.Add(pairs[i]);
					}
				}
			}
 /*
            foreach (Pair updatePair in validPairs)
            {
                if ((updatePair.v1 == p.v1 && updatePair.v2 == p.v2) ||
                    (updatePair.v1 == p.v2 && updatePair.v2 == p.v1))
                {
                    // throw new Exception("A pair with the same vertices as the removed pair was found!");
                }
                else
                {
                    newPair = new Pair(updatePair);
                    if (updatePair.v1 == p.v2) { newPair.v1 = p.v1; }
                    else if (updatePair.v2 == p.v2) { newPair.v2 = p.v1; }
                    newPair.update();
                    updatedPairs.Add(newPair);
                }
            }
			Console.Out.Write(validPairs.Count + "->");
  * */
            return validPairs;
        }

		// TODO: Make sure to check logic! This one is prone to errors!
		public static Mesh contract(Mesh m, Pair p)
		{
			//set the position of p.v1 to vbar
			//every edge to p.v2 must become an edge to p.v1
			//delete all degenerate triangles

            
            HalfEdge firstEdge = p.v2.e;
            HalfEdge nextEdge = firstEdge;

			HashSet<HalfEdge> edges = p.v2.outgoing(); ;
/*
            bool success;
            // TODO: This still loops forever on teapot

            // TODO: This still loops forever on teapot
            // TODO: This still loops forever on teapot
			do
            {
//                Console.Out.WriteLine("First edge: " + firstEdge.vert.n + ", " + firstEdge.opposite.vert.n);
//                Console.Out.WriteLine("Current edge: " + nextEdge.vert.n + ", " + nextEdge.opposite.vert.n);
                success = edges.Add(nextEdge);
                nextEdge = nextEdge.opposite.prev;
            }
            while (success);

			HalfEdge oppExternEdge;
			HalfEdge adjExternEdge;
*/	
		
			HalfEdge e1, e2;
			foreach (HalfEdge e in edges)
            {
                // NOTE: If trying to understand this code, this half-edge implementation is a bit weird.
                // Each vertex has a reference to an edge
                // Each edge has a reference to its starting vertex.
                // An edge's opposite has the same vertex as the edge's previous's vertex
                // ^ (This part is weird. Treat prev as next and next as prev.)


				//outgoing edge to v1
				if((e.vert == p.v1) || (e.opposite.vert == p.v1))
                if (e.prev.opposite.vert == p.v1)
				{
					e1 = e.next.opposite;
					e2 = e.prev.opposite;
					
					e1.opposite = e2;
					e2.opposite = e1;

					p.v1.e = e2;
					e2.vert.e = e1;
					m.triangles.Remove(e.face);
				}
				else
				{
					e.opposite.vert = p.v1;
				}
				/*
                if (e.prev.opposite.vert == p.v1)
                {
                    adjExternEdge = e.opposite;
                    oppExternEdge = e.prev.opposite;

                    p.v1.e = oppExternEdge;

                    if (oppExternEdge.vert != p.v1) { throw new Exception(); }

                    adjExternEdge.opposite = oppExternEdge;
                    oppExternEdge.opposite = adjExternEdge;

                    m.triangles.Remove(e.face);
                }
                else if (e.opposite.vert == p.v1)
                {
                    adjExternEdge = e.next.opposite;
                    oppExternEdge = e.prev.opposite;

                    adjExternEdge.vert = p.v1;
                    p.v1.e = adjExternEdge;

                    adjExternEdge.opposite = oppExternEdge;
                    oppExternEdge.opposite = adjExternEdge;

                    m.triangles.Remove(e.face);
                }
                else
                {
                    e.vert = p.v1;
                }
				 */ 
            }
			var vec = new Vector3(0, 0, 0);
			if (p.vbar == vec)
				Console.Out.WriteLine("Fuck");
			p.v1.pos = p.vbar;
			p.v1.Q = p.Q;
			m.vertices.Remove(p.v2);

            return m;
		}
	}
}
