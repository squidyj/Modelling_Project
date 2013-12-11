using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Subdivision_Project.Primitives;
namespace Subdivision_Project
{

	//possibility to improve data 
	public class BoundingBox
	{
		const float xSize = 2.0f;
		Vector3 min;
		Vector3 max;
		Vector3 center;
		float scale;

		public Vector3 Center
		{
			get { return center; }
		}

		public BoundingBox(Mesh m)
		{
			//set the min and the max to the values of the first vertex to ensure 
			min = m.vertices[0].pos;
			max = m.vertices[0].pos;
			center = new Vector3();


			foreach (Vertex v in m.vertices)
			{
				max.X = Math.Max(v.pos.X, max.X);
				max.Y = Math.Max(v.pos.Y, max.Y);
				max.Z = Math.Max(v.pos.Z, max.Z);

				min.X = Math.Min(v.pos.X, min.X);
				min.Y = Math.Min(v.pos.Y, min.Y);
				min.Z = Math.Min(v.pos.Z, min.Z);

				center += v.pos;
			}
			//make the height 2 units
			scale = xSize / (max.X - min.X);
			center = (1.0f / m.vertices.Count) * center;
		}

		public Matrix4 orientMesh()
		{
			return (centerMesh() * scaleMesh());
		}

		//move the center of the bounding box to the origin;
		public Matrix4 centerMesh()
		{ return Matrix4.CreateTranslation(-center); }

		public Matrix4 scaleMesh()
		{ return Matrix4.Scale(scale); }
	}	
}
