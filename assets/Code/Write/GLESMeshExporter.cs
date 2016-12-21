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
		if (GUILayout.Button ("Import")) 
		{
			script.curMesh.GetComponent<MeshFilter>().sharedMesh = GLESMeshExporter.Import(script.filename);
			Debug.Log ("Imported from: "+script.filename);
		}
	}
}

public class GLESMeshExporter : MonoBehaviour 
{
	public GameObject curMesh;
	public string filename = @"I:\uSource\test\mesh1.mesh";

	public static void Export(string path, Mesh mesh)
	{
		BinaryWriter bw = new BinaryWriter (File.Create (path));

		int numVerts = mesh.triangles.Length;//index array length is tris*3
		Debug.Log("mesh.triangles.Length is "+numVerts);
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
	
	public static Mesh Import(string path)
	{
		BinaryReader br = new BinaryReader (File.Open (path, FileMode.Open));

		int numVerts = br.ReadInt32 ()*3;
		Debug.Log("numVerts is "+numVerts);

		numVerts = (int)(br.BaseStream.Length-4)/96;

		numVerts=Mathf.Min(numVerts,65000);
		if(numVerts%3>0)
			numVerts -= numVerts%3;

		Debug.Log("numVerts is "+numVerts);

		Mesh mesh = new Mesh();
		mesh.name="Test mesh";

		Vector3[] verts = new Vector3[numVerts];
		Vector3[] normals = new Vector3[numVerts];
		Vector2[] uv = new Vector2[numVerts];
		int[] tris = new int[numVerts];
		
		for (int i=0; i<numVerts; i++) 
		{
			if(((i+1)*32)+4>br.BaseStream.Length)
				break;
			verts[i] = ConvertUtils.ReadVector3(br);
			normals[i] = ConvertUtils.ReadVector3(br);
			uv[i] = ConvertUtils.ReadVector2(br);
			tris[i] = i;
		}

		mesh.vertices = verts;
		mesh.normals = normals;
		mesh.uv = uv;
		mesh.SetTriangles( tris, 0 );
		//mesh.UploadMeshData(false);
		//mesh.RecalculateBounds();

		br.Close ();
		
		return mesh;
	}
}
