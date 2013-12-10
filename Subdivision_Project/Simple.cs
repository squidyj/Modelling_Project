using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Subdivision_Project.Primitives;


//give it a try using half-edge to help
namespace Subdivision_Project
{
	class Simple
	{
		public static Mesh simplify(Mesh m, int targetTris, float threshold)
		{
			//then the valid pairs
			SortedSet<Pair> validPairs = findValidPairs(m, threshold);

            //then the costs for each contraction
            calcCosts(m, validPairs);

            // TODO: NOTE! It looks like each vertex is being counted twice!

			//loop until enough triangles have been removed 
			//contract the lowest cost pair and remove it from the heap
            Pair p;

            while (targetTris > 0)
            {
                targetTris--;
                p = validPairs.First();
                validPairs.Remove(p);
                contract(m, p);
                updateCosts(m, validPairs, p);
            }

			//update the valid pairs to point to the newly created vertex where applicable
			//update the costs of those valid pairs

			return null;
		}

        // TODO: Check logic
        // Edit: Looks good. I checked with testgraph.obj and outputted the positions of the vertex pairs.
        public static bool isEdge(Vertex v1, Vertex v2)
        {
            Vertex firstAdjacent = v1.e.opposite.vert;
            if (v2.n == firstAdjacent.n) { return true; }
            else
            {
                Vertex nextAdjacent = firstAdjacent.e.next.opposite.vert;

                while (nextAdjacent.n != firstAdjacent.n)
                {
                    if (nextAdjacent.n == v2.n) { return true; }
                    else { nextAdjacent = nextAdjacent.e.next.opposite.vert; }
                }

                return false;
            }
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
            return updatedPairs;
        }

		public static void contract(Mesh m, Pair p)
		{
			//set the position of p.v1 to vbar
			//every edge to p.v2 must become an edge to p.v1
			//delete all degenerate triangles
		}
	}
}
