using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using System.Drawing;
namespace Subdivision_Project
{
	class Trackball
	{
		Matrix4 transform = Matrix4.Identity;
		Vector3 oldPoint;
		public Vector3 origin;
		public Matrix4 current;
		public float radius;


		public void reset()
		{
			current = Matrix4.Identity;
		}
	}
}
