using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Diagnostics;
namespace Subdivision_Project
{
	public class Model
	{
		public Mesh baseMesh, simplifiedMesh, subdividedMesh, activeMesh;
		//indicates whether model should be rendered

		public Vector3 Center
		{
			get { return activeMesh.Box.Center; }
		}
		public Matrix4 Transform
		{
			get { return activeMesh.Transform; }
		}

		public Model(string pathname)
		{
			//base mesh always exists
			baseMesh = new Mesh(pathname);
			activeMesh = baseMesh;
		}

		//sets the active maesh to the indicated mesh if that mesh exists
		public bool setActive(int n)
		{
			Mesh newActive = null;
			switch (n)
			{
				case 0:
					newActive = baseMesh;
					break;
				case 1:
					newActive = subdividedMesh;
					break;
				case 2:
					newActive = simplifiedMesh;
					break;
			}
			if(newActive == null)
				return false;
			activeMesh = newActive;
			return true;
		}

		public void subdivide()
		{
			/*
			var temp = LoopSubdivision.subdivide(activeMesh);
			if (temp == null)
				return;
			subdividedMesh = temp;
			activeMesh = subdividedMesh;
			 */
		}
 
        public void simplify(int targetTris, float threshold)
        {
			/*
			var temp = Simple.simplify(activeMesh, targetTris, threshold);
			if (temp == null)
				return;
			simplifiedMesh = temp;
			activeMesh = simplifiedMesh;
			 */ 
		}

		public void draw(int p) {
			activeMesh.draw(p); }
	}
}