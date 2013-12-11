using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Subdivision_Project.Primitives;
using VCSKicksCollection;


//give it a try using half-edge to help
namespace Subdivision_Project
{
	class Simple
	{
		public static Mesh simplify(Mesh m, int targetTris, float threshold)
		{

			//then the valid pairs
			findValidPairs();
			//then the costs for each contraction
			calcCosts();
			//loop until enough triangles have been removed 
			//contract the lowest cost pair and remove it from the heap
			contract();
			//update the valid pairs to point to the newly created vertex where applicable
			//update the costs of those valid pairs

			return null;
		}

		public static void findValidPairs()
		{
			//for every vertex in the mesh, v1
			//for every vertex in the mesh v2 != v1
			//if the distance between v1 and v2 is less than threshold or if the two vertices share an edge
			//they are a valid pair and are added to the list of valid pairs
		}

		public static void calcCosts()
		{
			//for every valid pair
			//take the sum of the quadric matrix for the two vertices
			//find the optimal vertex position, vbar
			//find the cost associated with that position
		}

		public static void contract()
		{
			//set the position of p.v1 to vbar
			//every edge to p.v2 must become an edge to p.v1
			//delete all degenerate triangles
		}
	}
}
