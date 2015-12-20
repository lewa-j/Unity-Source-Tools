using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using uSrcTools;

[CustomEditor(typeof(GLESMeshExporter))]
public class GLESMeshExporterCustomEditor:Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector ();
		GLESMeshExporter script = (GLESMeshExporter)target;
		if (GUILayout.Button ("Export")) 
		{
			GLESMeshExporter.Export(script.filename,script.curMesh.GetComponent<MeshFilter>().sharedMesh);
			Debug.Log ("Exported to: "+script.filename);
		}
		
	}
}

public class GLESMeshExporter : MonoBehaviour 
{
	public GameObject curMesh;
	public string filename=@"I:\uSource\test\mesh1.mesh";

	public static void Export(string path, Mesh mesh)
	{
		BinaryWriter bw = new BinaryWriter (File.Create (path));

		int numVerts = mesh.triangles.Length;
		int curIndex;
		Vector3 vert;
		Vector3 normal;
		Vector2 uv;

		bw.Write (numVerts/3);
		for (int i=0; i<numVerts; i++) 
		{
			curIndex=mesh.triangles[i];

			vert=mesh.vertices[curIndex];
			normal=mesh.normals[curIndex];
			uv=mesh.uv[curIndex];

			ConvertUtils.WriteVector3(bw,vert);
			ConvertUtils.WriteVector3(bw,normal);
			ConvertUtils.WriteVector2(bw,uv);
		}

		bw.Close ();
	}
}
