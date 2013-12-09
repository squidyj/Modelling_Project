using System;
using System.IO;
using System.Collections.Generic;
using OpenTK;
using Subdivision_Project.Primitives;
namespace Subdivision_Project
{
	public class ObjLoader
	{
		public static bool Load(Mesh mesh, string fileName)
		{
			try
			{
				using (StreamReader streamReader = new StreamReader(fileName))
				{
					Load(mesh, streamReader);
					streamReader.Close();
					return true;
				}
			}
			catch { return false; }
		}

		static char[] splitCharacters = new char[] { ' ' };

		//dynamic lists to be converted to static arrays when done reading
		static List<Vector3> vertices;
		static List<Vector3> normals;
		static List<Vector2> texCoords;
		static Dictionary<Vertex, int> vertexIndexMap;
		static List<Vertex> mVertices;
		static List<Triangle> mTriangles;

		static void Load(Mesh mesh, TextReader textReader)
		{
			vertices = new List<Vector3>();
			normals = new List<Vector3>();
			texCoords = new List<Vector2>();
			vertexIndexMap = new Dictionary<Vertex, int>();
			mVertices = new List<Vertex>();
			mTriangles = new List<Triangle>();

			string line;
			while ((line = textReader.ReadLine()) != null)
			{
				line = line.Trim(splitCharacters);
				line = line.Replace("  ", " ");

				string[] parameters = line.Split(splitCharacters);

				switch (parameters[0])
				{
					case "p": // Point
						break;

					case "v": // Vertex
						float x = float.Parse(parameters[1]);
						float y = float.Parse(parameters[2]);
						float z = float.Parse(parameters[3]);
						vertices.Add(new Vector3(x, y, z));
						break;

					case "vt": // TexCoord
						float u = float.Parse(parameters[1]);
						float v = float.Parse(parameters[2]);
						texCoords.Add(new Vector2(u, v));
						break;

					case "vn": // Normal
						float nx = float.Parse(parameters[1]);
						float ny = float.Parse(parameters[2]);
						float nz = float.Parse(parameters[3]);
						normals.Add(new Vector3(nx, ny, nz));
						break;

					case "f":
						switch (parameters.Length)
						{
							case 4:
								Triangle objTriangle = new Triangle();
								objTriangle.v0 = ParseFaceParameter(parameters[1]);
								objTriangle.v1 = ParseFaceParameter(parameters[2]);
								objTriangle.v2 = ParseFaceParameter(parameters[3]);
								mTriangles.Add(objTriangle);
								break;

							//n-gons need to be triangulated
							case 5:
								mTriangles.AddRange(triangulate(parameters));
								break;
						}
						break;
				}
			}

			mesh.Vertices = mVertices.ToArray();
			mesh.Triangles = mTriangles.ToArray();

			vertexIndexMap = null;
			vertices = null;
			normals = null;
			texCoords = null;
			mVertices = null;
			mTriangles = null;
		}

		static char[] faceParamaterSplitter = new char[] { '/' };
		static int ParseFaceParameter(string faceParameter)
		{
			Vector3 vertex = new Vector3();
			Vector2 texCoord = new Vector2();
			Vector3 normal = new Vector3();

			string[] parameters = faceParameter.Split(faceParamaterSplitter);

			int vertexIndex = int.Parse(parameters[0]);
			if (vertexIndex < 0) 
				vertexIndex = vertices.Count + vertexIndex;
			else 
				vertexIndex = vertexIndex - 1;
			vertex = vertices[vertexIndex];

			if (parameters.Length > 1)
			{
				int texCoordIndex = int.Parse(parameters[1]);
				if (texCoordIndex < 0) texCoordIndex = texCoords.Count + texCoordIndex;
				else texCoordIndex = texCoordIndex - 1;
				texCoord = texCoords[texCoordIndex];
			}

			if (parameters.Length > 2)
			{
				int normalIndex = int.Parse(parameters[2]);
				if (normalIndex < 0) normalIndex = normals.Count + normalIndex;
				else normalIndex = normalIndex - 1;
				normal = normals[normalIndex];
			}

			return FindOrAddVertex(ref vertex, ref texCoord, ref normal);
		}


		//naive triangulation assumes convex and winding property
		static List<Triangle> triangulate(string[] parameters)
		{
			List<Triangle> ts = new List<Triangle>();
			List<int> vs = new List<int>();
			Triangle tri;
			//get or create the indices of all of the vertices
			for(int i = 1; i < parameters.Length; i++)
			{
				vs.Add(ParseFaceParameter(parameters[i]));
			}
			
			//creates a fan from the first listed vertex
			while(vs.Count > 2)
			{
				tri = new Triangle();
				tri.v0 = vs[0];
				tri.v1 = vs[1];
				tri.v2 = vs[2];
				ts.Add(tri);
				vs.RemoveAt(1);
			}
			return ts;
		}

		static int FindOrAddVertex(ref Vector3 vertex, ref Vector2 texCoord, ref Vector3 normal)
		{
			Vertex newVertex = new Vertex(vertex, normal, texCoord);

			int index;
			if (vertexIndexMap.TryGetValue(newVertex, out index))
			{
				return index;
			}
			else
			{
				mVertices.Add(newVertex);
				vertexIndexMap[newVertex] = mVertices.Count - 1;
				return mVertices.Count - 1;
			}
		}
	}
}