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
			SortedSet<Vertex> contracted = new SortedSet<Vertex>(new VertIndex());
			overall.Start();
            int numOfTris = m.triangles.Count();
			f.textBox1.Clear();
			f.textBox1.Text = "Now simplifying to at most " + targetTris + " triangles\n";
			//then the valid pairs
			f.textBox1.AppendText("Updating edge costs...");
            timer.Restart();
			SortedSet<Pair> validPairs = new SortedSet<Pair>();

			foreach (Pair p1 in m.edges)
			{
				if (isValid(p1))
				{
					p1.update();
					validPairs.Add(p1);
				}
			}
			f.textBox1.AppendText(" done, in " + timer.ElapsedMilliseconds + "ms\n");
            

			//loop until enough triangles have been removed 
			//contract the lowest cost pair and remove it from the heap
			f.textBox1.AppendText("Contracting pairs...");
            timer.Restart();
			Pair p;
            
			while (m.triangles.Count() > targetTris && validPairs.Count > 0)
            {

                p = validPairs.First();
				/*
                Console.Out.WriteLine("Now attempting to contract pair: (" + p.v1.n + ", " + p.v2.n + ")");
                Console.Out.WriteLine("The pair is in validPairs: " + validPairs.Contains(p));
 */
				//Debug.Assert(!(contracted.Contains(p.v1) || contracted.Contains(p.v2)), "Attempting to contract a previously contracted vertex");
				

//				Console.Out.Write(validPairs.Count + "->");
				validPairs.Remove(p);
				contracted.Add(p.v2);
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
				
			var updated = new List<Pair>(p.v1.pairs.Union(p.v2.pairs));
			updated.Remove(p);
			Debug.Assert(!updated.Contains(p), "Duplicate instances of p");
			Pair p0;
			for(int i = 0; i < updated.Count; i++)
			{
				p0 = updated[i];
				validPairs.Remove(p0);
				if(p0.v1.Equals(p.v2))
					p0.v1 = p.v1;
				if(p0.v2.Equals(p.v2))
					p0.v2 = p.v1;
				p0.update();
				if (isValid(p0))
					validPairs.Add(p0);
				Debug.Assert(p0.v1 != p0.v2, "Pair is made up of two of the same vertex");
				Debug.Assert(!(p0.v1.Equals(p.v2) || p0.v2.Equals(p.v2)), "Contracted Vertex Passed Back into ValidPairs");
			}
			
			p.v1.pairs = new HashSet<Pair>(updated);
			p.v2.pairs = null;

			HashSet<Pair> forgotten = new HashSet<Pair>();
			foreach(Pair p1 in validPairs)
			{
				if(p1.v1.Equals(p.v2))
					forgotten.Add(p1);
				if(p1.v2.Equals(p.v2))
					forgotten.Add(p1);
			}
			
			return validPairs;
		}

		private static bool isValid(Pair p)
		{
			HalfEdge e;
			
			if (p.v1.boundary() || p.v2.boundary())
			{
				//if the edge of contraction is not a boundary
				//find the halfedge between the two
				e = p.findEdge();
				if ((e.face != null) && (e.opposite.face != null))
					return false;
			}
			return true;
		}
		// TODO: Make sure to check logic! This one is prone to errors!
		public static Mesh contract(Mesh m, Pair p)
		{
			//set the position of p.v1 to vbar
			//every edge to p.v2 must become an edge to p.v1
			//delete all degenerate triangles
            HashSet<HalfEdge> edges = p.v2.outgoing();

			//opposite half edges
			HalfEdge o1, o2;
            foreach (HalfEdge outgoing in edges)
            {
				//needs a boundary case
                if (outgoing.vert.Equals(p.v1))
                {
					if (outgoing.face == null)
					{
						outgoing.prev.vert = p.v1;
						outgoing.next.prev = outgoing.prev;
						outgoing.prev.next = outgoing.next;
					}
					else
					{
						o1 = outgoing.next.opposite;
						o2 = outgoing.prev.opposite; 
						// Make sure none of the remaining vertices are linked to internal edges
						//but don't overwrite if they are referencing boundary halfedges
						if(p.v1.e.face != null)
							p.v1.e = o2;
						if(outgoing.next.vert.e.face != null)
							outgoing.next.vert.e = o1;

						o1.opposite = o2;
						o2.opposite = o1;
						m.triangles.Remove(outgoing.face);
					}
                }
                else if (outgoing.next.vert.Equals(p.v1))
                {
					//imagine that v1 and v2 cut a corner
					//if you walk 2 steps on the boundary you'll get to v1 but that is incorrect
					Debug.Assert(outgoing.face != null, "Attempted to cut off a corner");
					o1 = outgoing.next.opposite;
					o2 = outgoing.opposite;

					if (p.v1.e.face != null)
						p.v1.e = o1;
					if (outgoing.next.vert.e.face != null)
						outgoing.next.vert.e = o2;

					o1.opposite = o2;
					o2.opposite = o1;

                    m.triangles.Remove(outgoing.face);
                }
                else
                {
					//if there is an incoming edge from v1 to v2 on the boundary
					if ((outgoing.face == null) && (outgoing.prev.prev.vert.Equals(p.v1)))
					{
						outgoing.prev = outgoing.prev.prev;
						outgoing.prev.next = outgoing;
					}
					else
						outgoing.prev.vert = p.v1;
                }

			}
            
//          m.vertices.Remove(p.v2);
			//does this line break everything? NO 
			p.v1.pos = p.vbar;
			p.v1.Q = p.Q;

            return m;
		}
	}
}
