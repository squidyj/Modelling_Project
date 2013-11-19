﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using System.IO;
using OpenTK;

namespace Subdivision_Project
{
	public partial class Form1 : Form
	{
		//program, vertex shader, fragment shader, and modelview locations
		int program, vs, fs, mvLoc, pLoc;
		//field of view
		float fov = 0.5f; 
		float ratio;

		//list of models that are loaded in memory, potentially being rendered
		List<Model> models;
		//projection matrix
		Matrix4 projection;
		//Modelview stack 
		MatrixStack modelview;

		public Form1()
		{
			InitializeComponent();
		}

		private void gl_load(object sender, EventArgs e)
		{
			//Get OpenGL version
			var glVersion = GL.GetString(StringName.Version);
			int major = int.Parse(glVersion[0].ToString());
			int minor = int.Parse(glVersion[2].ToString());
			Console.Out.WriteLine("OpenGL major version " + major + ", minor version " + minor + ".") ;

			models = new List<Model>();
			fov = 60f * ((float) Math.PI / 180f);
			Console.Out.WriteLine(fov);
			createShaders();
			getMatrixLocations();
			setProjection();
			setView();
				
			Model m = new Model(program);
			models.Add(m);
			setView(target: m.center);
	
			GL.ClearColor(Color.Bisque);
		}

		private void setProjection()
		{
			ratio = glControl1.Width / (float)glControl1.Height;
			projection = Matrix4.CreatePerspectiveFieldOfView(fov, ratio, 0.01f, 10000);
			GL.UniformMatrix4(pLoc, false, ref projection);
		}

		private void glResize(object sender, EventArgs e)
		{
			setProjection();
			glControl1.Invalidate();
		}

		private void getMatrixLocations()
		{
			mvLoc = GL.GetUniformLocation(program, "modelview");
			pLoc = GL.GetUniformLocation(program, "projection");
		}

		//use nullables to allow the parameters to be optional 
		private void setView(Vector3? eye = null, Vector3? up = null, Vector3? target = null)
		{
			if(eye == null)
				eye = new Vector3(0,0,-2);
			if(up == null)
				up = new Vector3(0,1,0);
			if(target == null)
				target = new Vector3(0,0,0);

			modelview = new MatrixStack(mvLoc);
			modelview.push(Matrix4.LookAt((Vector3)eye, (Vector3)up, (Vector3)target));
		}

		private void initMatrices()
		{
		}

		private void createShaders()
		{
			//make a new empty program that will use our shaders
			program = GL.CreateProgram();

			//read in the shader source code from their respective files
			StreamReader v_read = new StreamReader("D:\\Projects\\Modelling_Project\\Subdivision_Project\\v_shader.glsl");
			StreamReader f_read = new StreamReader("D:\\Projects\\Modelling_Project\\Subdivision_Project\\f_shader.glsl");
			string v_string = v_read.ReadToEnd();
			string f_string = f_read.ReadToEnd();

			//create empty shader objects with unsigned int identifiers
			vs = GL.CreateShader(ShaderType.VertexShader);
			fs = GL.CreateShader(ShaderType.FragmentShader);

			//set the source code for our shader objects using what we got from our files
			GL.ShaderSource(vs, v_string);
			GL.ShaderSource(fs, f_string);

			//compile the source code
			GL.CompileShader(vs);
			GL.CompileShader(fs);

			//add the shaders to the program
			GL.AttachShader(program, vs);
			GL.AttachShader(program, fs);
			Console.WriteLine(GL.GetShaderInfoLog(vs));
			Console.WriteLine(GL.GetShaderInfoLog(fs));
 
			//link our program together
			GL.LinkProgram(program);
			Console.WriteLine(GL.GetProgramInfoLog(program));

			//tell opengl to use our shader program when rendering
			GL.UseProgram(program);
		}

		private void glPaint(object sender, PaintEventArgs e)
		{
			GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit); // Clear required buffers
			foreach (Model m in models)
				if (m.Loaded)
				{
					//modelview.push(m.Transform);
					m.draw();
					//modelview.pop();
				}
			glControl1.SwapBuffers();
		}


		private void loadModel(object sender, EventArgs e)
		{
			OpenFileDialog open = new OpenFileDialog();
			open.Filter = "Object Files (.obj)|*.obj|All Files (*.*)|*.*";
			open.FilterIndex = 1;
			open.ShowDialog();
			Model m = new Model(open.FileName, program);
			models.Add(m);
			setView(target: m.center); 
			glControl1.Invalidate();
		}
	}
}
