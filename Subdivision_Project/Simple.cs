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
		public static Mesh simplify(Mesh m, int targetTris, Form1 f)
		{
            Stopwatch timer = new Stopwatch();
			Stopwatch overall = new Stopwatch();
			overall.Start();
            int numOfTris = m.triangles.Count();
			f.textBox1.Clear();
			f.textBox1.Text = "Now simplifying to at most " + targetTris + " triangles\n";

			//then the valid pairs
			f.textBox1.AppendText("Updating edge costs...");
            timer.Restart();
			foreach (Pair p1 in m.edges)
				p1.update();
			f.textBox1.AppendText(" done, in " + timer.ElapsedMilliseconds + "ms\n");
            Pair oddPair = new Pair(m.vertices[0], m.vertices[0]);

            foreach (Pair p2 in m.edges)
            {
                if (p2.v1.n == 2396 && p2.v2.n == 767)
                {
                    oddPair = p2;
                }
            }
			SortedSet<Pair> validPairs = new SortedSet<Pair>(m.edges);

			//loop until enough triangles have been removed 
			//contract the lowest cost pair and remove it from the heap
			f.textBox1.AppendText("Contracting pairs...");
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

				validPairs = updateCosts(m, validPairs, p);

//				Console.Out.WriteLine(validPairs.Count);
//              Console.Out.WriteLine("Contracted pair (" + p.v1.n + ", " + p.v2.n + ")");

//                Debug.Assert(p.v1 != oddPair.v1 && p.v2 != oddPair.v1 && p.v1 != oddPair.v2 && p.v2 != oddPair.v2);
            }
            timer.Stop();
			f.textBox1.AppendText(" done, in " + timer.ElapsedMilliseconds + "ms\n");

			//update the valid pairs to point to the newly created vertex where applicable
			//update the costs of those valid pairs

            m.reconstruct();

			f.textBox1.AppendText("Simplified mesh from " + numOfTris + " triangles to " + m.triangles.Count() + " triangles!\n");
			long elapsed = overall.ElapsedMilliseconds;
			if (elapsed >= 1000)
			{
				f.textBox1.AppendText("Simplification took " + (float)elapsed / 1000.0f + " seconds\n");
			}
			else
				f.textBox1.AppendText("Simplification took " + elapsed + "ms\n");
			return m;
		}
  
        public static SortedSet<Pair> updateCosts(Mesh m, SortedSet<Pair> validPairs, Pair p)
        {
			List<Pair> updated = new List<Pair>(p.v1.pairs.Union(p.v2.pairs));
			List<int> removal = new List<int>();
			Pair p0;
			for(int i = 0; i < updated.Count; i++)
			{
				p0 = updated[i];
				validPairs.Remove(p0);
				if(p0.v1 == p.v2)
					p0.v1 = p.v1;
				if(p0.v2 == p.v2)
					p0.v2 = p.v1;
				if (p0.v1 != p0.v2)
				{
					p0.update();
					validPairs.Add(p0);
				}
				else
					removal.Add(i);
			}
			for (int i = removal.Count - 1; i >= 0; i--)
				updated.RemoveAt(removal[i]);
			p.v1.pairs = new HashSet<Pair>(updated);
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
			Console.Out.WriteLine(p.v1.pos);
 //           m.vertices.Remove(p.v2);
            p.v1.pos = p.vbar;

            p.v1.Q = p.Q;

            return m;
		}
	}
}
