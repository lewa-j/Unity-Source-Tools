using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using uSrcTools;

[CustomEditor(typeof(COLLADAExport))]
public class COLLADAExportCustomEditor:Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector ();
		COLLADAExport script = (COLLADAExport)target;
		if (GUILayout.Button ("Export")) 
		{
			COLLADAExport.Export(script.filename,COLLADAExport.MeshToGeometry(script.curMesh),script.zUp,script.sourceUnits);
			Debug.Log ("Exported to: "+script.filename);
		}

	}
}

public class COLLADAExport : MonoBehaviour
{
	public string filename=@"I:\uSource\test\mesh1.dae";

	public GameObject curMesh;
	public bool zUp;
	public bool sourceUnits;
	
	public static void Export(string path, Geometry geom,bool zUp,bool sourceUnits)
	{
		CreateXmlFile(path);
		WrtieXmlDocument (path,geom,zUp,sourceUnits);
	}

	static void CreateXmlFile(string path)
	{
		XmlTextWriter xtw = new XmlTextWriter (path, System.Text.Encoding.UTF8);
		xtw.WriteStartDocument ();
		xtw.WriteStartElement("COLLADA");
		xtw.WriteAttributeString ("version", "1.4.1");
		xtw.WriteEndElement();
		xtw.Close ();
	}

	static void WrtieXmlDocument(string path,Geometry geom, bool zUp,bool sourceUnits)
	{
		XmlDocument xDoc = new XmlDocument ();
		xDoc.Load (path);

		XmlNode rootNode = xDoc.DocumentElement;
		
		XmlAttribute xAtrib = xDoc.CreateAttribute ("xmlns");
		xAtrib.Value = "http://www.collada.org/2005/11/COLLADASchema";
		rootNode.Attributes.Prepend (xAtrib);

		//Asset
		XmlNode assetNode = WriteAsset (zUp, sourceUnits, xDoc);
		rootNode.AppendChild (assetNode);
		xDoc.Save (path);
//===============================<<<<TEMP>>>>====================================
		/*Geometry g = new Geometry ("Test-mesh","Test",1);
		Source src = new Source("Test-mesh-position",12);
		src.positionArray = new float[]{0,0,0, 32,0,0, 0,32,0, 32,32,0};
		Vertices verts = new Vertices ("Test-mesh-vertices");
		Triangles tris = new Triangles ("Test-mesh-tris",2,"Mat1");
		tris.p = new int[]{0,1,2,2,1,3};
		g.meshes [0] = new COLLADAMesh (new Source[]{src},verts,tris);*/
//===============================================================================

		//GeometryLibrary
		XmlNode GeomLibNode = xDoc.CreateElement("library_geometries");
		rootNode.AppendChild (GeomLibNode);
		WriteGeometryLibrary (new Geometry[]{geom},GeomLibNode,xDoc,path);
		xDoc.Save (path);

		XmlNode VisSceneNode = WriteVisualSceneLibrary (geom.id,geom.name,xDoc);
		rootNode.AppendChild (VisSceneNode);
		
		xDoc.Save (path);
	}

	static XmlNode WriteAsset(bool zUp,bool sourceUnits, XmlDocument xDoc)
	{
		XmlElement assetNode = xDoc.CreateElement ("asset");

		//<unit name="Source unit" meter="0.026"/>
		XmlElement unitElem = xDoc.CreateElement ("unit");
		XmlAttribute nameAtrib = xDoc.CreateAttribute ("name");
		nameAtrib.Value = sourceUnits?"Source unit":"meter";
		unitElem.Attributes.Append (nameAtrib);
		XmlAttribute meterAtrib = xDoc.CreateAttribute ("meter");
		meterAtrib.Value = sourceUnits?uSrcSettings.Inst.worldScale.ToString() :"1";
		unitElem.Attributes.Append (meterAtrib);
		assetNode.AppendChild (unitElem);
		//
		
		XmlNode axisNode = xDoc.CreateElement ("up_axis");
		axisNode.InnerText = zUp ? "Z_UP":"Y_UP";
		assetNode.AppendChild (axisNode);

		return assetNode;
	}

	//<library_geometries>
	static XmlNode WriteGeometryLibrary(Geometry[] geoms,XmlNode GeomLib, XmlDocument xDoc,string path)
	{

		for (int i=0; i<geoms.Length; i++) 
		{
			XmlNode Geom = xDoc.CreateElement("geometry");
			GeomLib.AppendChild(Geom);
			WriteGeometry(geoms[i],Geom, xDoc,path);
			//xDoc.Save (path);
		}
		return GeomLib;
	}

	static XmlNode WriteVisualSceneLibrary(string mesh, string name, XmlDocument xDoc)
	{
		XmlNode VisSceneNode = xDoc.CreateElement ("library_visual_scenes");

		VisSceneNode.InnerXml = "<visual_scene id=\"Scene\" name=\"Scene\">\n"+
				"<node id=\""+name+"\" name=\""+name+"\" type=\"NODE\">\n"+
				"<instance_geometry url=\"#"+mesh+"\">\n"+
				"</instance_geometry>\n"+
      			"</node>\n"+
    			"</visual_scene>";

		return VisSceneNode;
	}

	//<geometry id="Cube-mesh" name="Cube">
	static XmlNode WriteGeometry(Geometry geom,XmlNode GeomNode, XmlDocument xDoc,string path)
	{

		XmlAttribute idAtrib = xDoc.CreateAttribute ("id");
		idAtrib.Value = geom.id;
		GeomNode.Attributes.Append (idAtrib);

		XmlAttribute nameAtrib = xDoc.CreateAttribute ("name");
		nameAtrib.Value = geom.name;
		GeomNode.Attributes.Append (nameAtrib);


		XmlNode MeshNode = xDoc.CreateElement("mesh");
		GeomNode.AppendChild (MeshNode);
		WriteMesh (geom.mesh, MeshNode, xDoc,path);

		return GeomNode;
	}

	static XmlNode WriteMesh(COLLADAMesh mesh, XmlNode MeshNode, XmlDocument xDoc,string path)
	{

		for (int i=0; i<mesh.sources.Length; i++) 
		{
			XmlNode SourceNode=xDoc.CreateElement ("source");
			MeshNode.AppendChild (SourceNode);
			WriteSource(mesh.sources[i],SourceNode,xDoc);
			xDoc.Save (path);
		}

		XmlNode VertsNode = WriteVertices (mesh.vertices, mesh.sources[0].id,mesh.sources[1].id,xDoc);
		MeshNode.AppendChild (VertsNode);
		xDoc.Save (path);

		for (int i=0; i<mesh.triangles.Length; i++) 
		{
			XmlNode TrisNode=WriteTriangles(mesh.triangles[i], mesh.vertices.id, xDoc);
			MeshNode.AppendChild (TrisNode);
			xDoc.Save (path);
		}

		return MeshNode;
	}

	static XmlNode WriteSource(Source src, XmlNode SrcNode, XmlDocument xDoc)
	{
		XmlAttribute idAtrib = xDoc.CreateAttribute ("id");
		idAtrib.Value = src.id;
		SrcNode.Attributes.Append (idAtrib);

		XmlNode arrayNode = xDoc.CreateElement ("float_array");
		SrcNode.AppendChild (arrayNode);

		idAtrib = xDoc.CreateAttribute ("id");
		idAtrib.Value = src.id+"-array";
		arrayNode.Attributes.Append (idAtrib);

		if (src.positionArray != null) 
		{
			XmlAttribute countAtrib = xDoc.CreateAttribute ("count");
			countAtrib.Value = (src.positionArray.Length*3).ToString ();
			arrayNode.Attributes.Append (countAtrib);

			string verts = "";
			for (int i=0; i<src.positionArray.Length; i++) 
			{
				verts += src.positionArray [i].x + " ";
				verts += src.positionArray [i].y + " ";
				verts += src.positionArray [i].z + " ";
			}
			arrayNode.InnerText = verts;

			SrcNode.InnerXml += "<technique_common>\n" +
				"<accessor source=\"#" + src.id + "-array\" count=\"" + src.positionArray.Length + "\" stride=\"3\">\n" +
					"<param name=\"X\" type=\"float\"/>\n" +
					"<param name=\"Y\" type=\"float\"/>\n" +
					"<param name=\"Z\" type=\"float\"/>\n" +
					"</accessor>\n" +
					"</technique_common>\n";
		}
		else
		{
			XmlAttribute countAtrib = xDoc.CreateAttribute ("count");
			countAtrib.Value = (src.uvArray.Length*2).ToString ();
			arrayNode.Attributes.Append (countAtrib);
			
			string uvs = "";
			for (int i=0; i<src.uvArray.Length; i++) 
			{
				uvs += src.uvArray [i].x + " ";
				uvs += (1-src.uvArray [i].y) + " ";
			}
			arrayNode.InnerText = uvs;

			SrcNode.InnerXml += 
				"<technique_common>\n" +
					"<accessor source=\"#" + src.id + "-array\" count=\"" + src.uvArray.Length+ "\" stride=\"2\">\n" +
						"<param name=\"S\" type=\"float\"/>\n" +
						"<param name=\"T\" type=\"float\"/>\n" +
					"</accessor>\n" +
				"</technique_common>\n";
		}
		/*xDoc.CreateTextNode("<technique_common>\n"+
			"<accessor source=\""+src.id+"-array\" count=\""+src.positionArray.Length/3+"\" stride=\"3\">\n"+
				"<param name=\"X\" type=\"float\"/>\n"+
				"<param name=\"Y\" type=\"float\"/>\n"+
				"<param name=\"Z\" type=\"float\"/>\n"+
				"</accessor>\n"+
				"</technique_common>\n");*/


		return SrcNode;
	}

	static XmlNode WriteVertices(Vertices verts, string posSource,string uvSource, XmlDocument xDoc)
	{
		XmlNode VertsNode = xDoc.CreateElement ("vertices");

		XmlAttribute idAtrib = xDoc.CreateAttribute ("id");
		idAtrib.Value = verts.id;
		VertsNode.Attributes.Append (idAtrib);

		//===============================================================
		XmlNode InputNode = xDoc.CreateElement ("input");
		VertsNode.AppendChild (InputNode);

		XmlAttribute semanticAtrib = xDoc.CreateAttribute ("semantic");
		semanticAtrib.Value = "POSITION";
		InputNode.Attributes.Append (semanticAtrib);

		XmlAttribute sourceAtrib = xDoc.CreateAttribute ("source");
		sourceAtrib.Value = "#"+posSource;
		InputNode.Attributes.Append (sourceAtrib);
		//===============================================================
		InputNode = xDoc.CreateElement ("input");
		VertsNode.AppendChild (InputNode);
		
		semanticAtrib = xDoc.CreateAttribute ("semantic");
		semanticAtrib.Value = "TEXCOORD";
		InputNode.Attributes.Append (semanticAtrib);
		
		sourceAtrib = xDoc.CreateAttribute ("source");
		sourceAtrib.Value = "#"+uvSource;
		InputNode.Attributes.Append (sourceAtrib);


		return VertsNode;
	}

	static XmlNode WriteTriangles(Triangles tris, string source, XmlDocument xDoc)
	{
		XmlNode TrisNode = xDoc.CreateElement ("triangles");

		XmlAttribute countAtrib = xDoc.CreateAttribute ("count");
		countAtrib.Value = tris.count.ToString();
		TrisNode.Attributes.Append (countAtrib);

		XmlAttribute matAtrib = xDoc.CreateAttribute ("material");
		matAtrib.Value = tris.material;
		TrisNode.Attributes.Append (matAtrib);
		
		XmlNode InputNode = xDoc.CreateElement ("input");
		TrisNode.AppendChild(InputNode);

		XmlAttribute semanticAtrib = xDoc.CreateAttribute ("semantic");
		semanticAtrib.Value = "VERTEX";
		InputNode.Attributes.Append (semanticAtrib);
		
		XmlAttribute sourceAtrib = xDoc.CreateAttribute ("source");
		sourceAtrib.Value = "#"+source;
		InputNode.Attributes.Append (sourceAtrib);

		XmlAttribute offsetAtrib = xDoc.CreateAttribute ("offset");
		offsetAtrib.Value = "0";
		InputNode.Attributes.Append (offsetAtrib);

		XmlNode pNode = xDoc.CreateElement ("p");
		TrisNode.AppendChild (pNode);

		string pStr = "";
		for (int i=0; i<tris.p.Length; i++) 
		{
			pStr += tris.p[i]+" ";
		}
		pNode.InnerText = pStr;

		return TrisNode;
	}

	public static Geometry MeshToGeometry(GameObject go)
	{
		Mesh mesh = go.GetComponent<MeshFilter> ().sharedMesh;

		Geometry geom = new Geometry(mesh.name+"-mesh", mesh.name);

		Source posSrc = new Source (mesh.name + "-mesh-position");
		posSrc.positionArray = mesh.vertices;

		Source uvSrc = new Source (mesh.name + "-mesh-uv");
		uvSrc.uvArray = mesh.uv;

		Vertices verts = new Vertices (mesh.name + "-mesh-vertices");

		int numMeshes=mesh.subMeshCount;
		Debug.Log ("Mesh count "+numMeshes);

		Triangles[] tris = new Triangles[numMeshes];
		for (int i=0; i<numMeshes; i++) 
		{
			int indCount=mesh.GetTriangles(i).Length;
			Debug.Log ("Triangles "+i+" count "+indCount);
			tris [i] = new Triangles (mesh.name + "-mesh-tris", indCount / 3, "Mat"+(i+1));
			tris [i].p = mesh.GetTriangles(i);
		}

		/*
		Triangles[] tris = new Triangles[1];
		int i = 1;
		tris [0] = new Triangles (mesh.name + "-mesh-tris", mesh.GetIndices (i).Length / 3, "Mat"+i);
		tris [0].p = mesh.GetIndices(i);
		*/
		geom.mesh = new COLLADAMesh (new Source[]{posSrc,uvSrc}, verts, tris);
		
		return geom;
	}

	public static Geometry MeshToGeometry(Mesh mesh)
	{
		Geometry geom = new Geometry(mesh.name+"-mesh", mesh.name);
		
		Source posSrc = new Source (mesh.name + "-mesh-position");
		posSrc.positionArray = mesh.vertices;
		
		Source uvSrc = new Source (mesh.name + "-mesh-uv");
		uvSrc.uvArray = mesh.uv;
		
		Vertices verts = new Vertices (mesh.name + "-mesh-vertices");
		
		int numMeshes=mesh.subMeshCount;
		Debug.Log ("Mesh count "+numMeshes);
		Triangles[] tris = new Triangles[numMeshes];
		
		tris[0] = new Triangles (mesh.name + "-mesh-tris", mesh.triangles.Length / 3, "Mat1");
		tris[0].p = mesh.triangles;
		//Triangles tris2 = new Triangles (mesh.name + "-mesh-tris", mesh.triangles.Length / 3, "Mat2");
		//tris2.p = mesh.triangles;
		geom.mesh = new COLLADAMesh (new Source[]{posSrc,uvSrc}, verts, tris);
		
		return geom;
	}

	/*class GeometryLibrary
	{
		public Geometry[] geometries;
	}*/

	public class Geometry
	{
		public string id;
		public string name;

		public COLLADAMesh mesh;

		public Geometry(string id, string name)
		{
			this.id = id;
			this.name = name;
			this.mesh = null;
		}
	}

	public class COLLADAMesh
	{
		public Source[] sources;
		public Vertices vertices;
		public Triangles[] triangles;

		public COLLADAMesh(Source[] sources, Vertices verts, Triangles[] tris)
		{
			this.sources = sources;
			vertices = verts;
			triangles = tris;
		}
	}

	public class Source
	{
		public string id;
		
		public Vector3[] positionArray;
		public Vector2[] uvArray;
		//public TechniqueCommon technique_common;

		public Source(string id)
		{
			this.id=id;
			this.positionArray=null;
			this.uvArray=null;
		}
	}

	public class Triangles
	{
		public string name;
		public int count;
		public string material;

		public int[] p;

		//public COLLADAInput[] inputs;

		public Triangles(string name, int count, string material)
		{
			this.name = name;
			this.count = count;
			this.material = material;

			this.p = null;
		}
	}
	/*
	class COLLADAInput
	{

	}*/



	public class Vertices
	{
		public string id;
		public string name;

		public Vertices(string id)
		{
			this.id = id;
		}
	}
}
