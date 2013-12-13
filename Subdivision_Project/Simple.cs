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
            timer.Restart();
			foreach (Pair p1 in m.edges)
				p1.update();

			SortedSet<Pair> validPairs = new SortedSet<Pair>(m.edges);

            timer.Stop();

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
			Pair p;

            int counter = 0;
            
			while (m.triangles.Count() > targetTris && validPairs.Count > 0)
            {
                counter++;

                p = validPairs.First();
				/*
                Console.Out.WriteLine("Now attempting to contract pair: (" + p.v1.n + ", " + p.v2.n + ")");
                Console.Out.WriteLine("The pair is in validPairs: " + validPairs.Contains(p));
 */

//				Console.Out.Write(validPairs.Count + "->");
				validPairs.Remove(p);
//				Console.Out.Write(validPairs.Count + "->");
//                Console.Out.WriteLine("The pair is in validPairs: " + validPairs.Contains(p));

                m = contract(m, p);

				validPairs = updateCosts(m, validPairs, p, counter);

//				Console.Out.WriteLine(validPairs.Count);
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
  
        public static SortedSet<Pair> updateCosts(Mesh m, SortedSet<Pair> validPairs, Pair p, int counter)
        {
			bool modified;
			var pairs = validPairs.ToList();

			for(int i = 0; i < pairs.Count; i++)
			{
				modified = false;
                if (pairs[i].v1 == p.v2) { validPairs.Remove(pairs[i]); pairs[i].v1 = p.v1; modified = true; }
                if (pairs[i].v2 == p.v2) { validPairs.Remove(pairs[i]); pairs[i].v2 = p.v1; modified = true; }
				if(modified)
				{
					if(pairs[i].v1 != pairs[i].v2)
					{
						pairs[i].update();
						validPairs.Add(pairs[i]);
					}
				}
			}

            return validPairs;
        }

		// TODO: Make sure to check logic! This one is prone to errors!
		public static Mesh contract(Mesh m, Pair p)
		{
			//set the position of p.v1 to vbar
			//every edge to p.v2 must become an edge to p.v1
			//delete all degenerate triangles

            HashSet<HalfEdge> edges = p.v2.outgoing();

            HalfEdge adjExtEdge, oppExtEdge;

            foreach (HalfEdge outgoing in edges)
            {
                if (outgoing.vert == p.v1)
                {
                    adjExtEdge = outgoing.prev.opposite;
                    oppExtEdge = outgoing.next.opposite;

                    // Make sure none of the remaining vertices are linked to internal edges
                    p.v1.e = adjExtEdge;
                    adjExtEdge.vert.e = oppExtEdge;

                    adjExtEdge.opposite = oppExtEdge;
                    oppExtEdge.opposite = adjExtEdge;

                    m.triangles.Remove(outgoing.face);
                }
                else if (outgoing.next.vert == p.v1)
                {
                    adjExtEdge = outgoing.opposite;
                    oppExtEdge = outgoing.next.opposite;

                    adjExtEdge.vert = p.v1;

                    // Make sure none of the remaining vertices are linked to internal edges
                    p.v1.e = oppExtEdge;
                    oppExtEdge.vert.e = adjExtEdge;

                    adjExtEdge.opposite = oppExtEdge;
                    oppExtEdge.opposite = adjExtEdge;

                    m.triangles.Remove(outgoing.face);
                }
                else 
                {
                    outgoing.prev.vert = p.v1;
                }
            }
            m.vertices.Remove(p.v2);
            p.v1.pos = p.vbar;

            p.v1.Q = p.Q;

            return m;
		}
	}
}
