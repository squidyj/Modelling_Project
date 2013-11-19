using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace Subdivision_Project
{
	class MatrixStack
	{
		Matrix4 top;
		Stack<Matrix4> matrices;
		int location;
		public MatrixStack(int l)
		{
			matrices = new Stack<Matrix4>();
			//push in an identity matrix on creation for simplicity, our stack will alwyas have a bottom to multiply against
			top = Matrix4.Identity;
			matrices.Push(top);
			location = l;
		}

		public void push()
		{
			matrices.Push(top);
		}
		//push a transformation onto the stack
		public void push(Matrix4 m)
		{
			//is that the correct order?
			top = top * m;
			GL.UniformMatrix4(location, false, ref top);
			matrices.Push(top);
		}

		public void pop()
		{
			if (matrices.Count <= 1)
				return;
			matrices.Pop();
			top = matrices.Peek();
			GL.UniformMatrix4(location, false, ref top);
		}
	}
}
