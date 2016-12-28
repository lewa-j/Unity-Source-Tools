using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace uSrcTools
{
	[Serializable]
	public class SourceBSPLoader : MonoBehaviour 
	{
		public enum Type
		{
			Full,
			WithoutBatching,
			OneFace,
			OnlyDisplacements
		}

		public BSPFile map;
		
		public GameObject mapObject;
		public GameObject modelsObject;
		public GameObject propsObject;
		public GameObject dispObject;
		public GameObject entObject;

		public List<GameObject> Models = new List<GameObject>();
		public List<GameObject> Props = new List<GameObject>();

		public List<string> entities = new List<string>();

		public List<LightmapData> lightmapsData;

		string materaialsToLoad;
		private string LevelName;

		public Type LoadType=Type.Full;
		public int faceId=0;
		public int maxLMs=4608;

		public void Load (string mapName)
		{
			LevelName = mapName;
			
			string path = "";

			path = ResourceManager.GetPath ("maps/" + LevelName + ".bsp");

			if(path==null)
			{
				return;
			}

			BinaryReader BR = new BinaryReader (File.Open (path, FileMode.Open));

			map = new BSPFile (BR, LevelName);
			if(uSrcSettings.Inst.entities)
			{
				ParseEntities (map.entitiesLump);
			}
			//ProcessFaces();

			//===================================================
			mapObject = new GameObject (LevelName);

			modelsObject = new GameObject ("models");
			modelsObject.transform.parent = mapObject.transform;

			if (uSrcSettings.Inst.displacements)
			{
				dispObject = new GameObject ("disp");
				dispObject.transform.parent = mapObject.transform;
			}

			if(uSrcSettings.Inst.props)
			{
				propsObject = new GameObject ("props");
				propsObject.transform.parent = mapObject.transform;
			}

			switch(LoadType)
			{
			case Type.Full:
				Debug.Log ("Start Loading World Faces");

				if(uSrcSettings.Inst.lightmaps&map.hasLightmaps)
					lightmapsData = new List<LightmapData>();

				for (int i=0; i<map.modelsLump.Length; i++) 
				{
					CreateModelObject(i);
				}

				if(uSrcSettings.Inst.lightmaps&map.hasLightmaps)
				{
					Debug.Log ("Loading "+lightmapsData.Count+" lightmap pages");
					LightmapSettings.lightmaps = lightmapsData.ToArray();
				}

				Debug.Log("Finish World Faces");

				if(uSrcSettings.Inst.entities)
				{
					Debug.Log ("Start Loading Entities");

					entObject=new GameObject ("entities"); 
					entObject.transform.parent = mapObject.transform;

					for(int i=0; i<entities.Count; i++)
					{
						LoadEntity (i);
					}
					Debug.Log ("Finish Entities");
				}

				if(uSrcSettings.Inst.displacements)
				{
					for(int m=0;m<map.modelsLump.Length;m++)
					{
						for(int i=map.modelsLump[m].firstface;i<map.modelsLump[m].firstface+map.modelsLump[m].numfaces;i++)
						{
							if(map.facesLump[i].dispinfo!=-1)
							{
								GenerateDispFaceObject(i,m);
							}
						}
					}
					Debug.Log ("Finish Displacements");
				}

				//Static props
				if(uSrcSettings.Inst.props && map.staticPropsReaded)
				{
					Debug.Log ("Start Loading Static props");
					for(int i=0; i < map.StaticPropCount; i++)
					{
						bspStaticPropLump prop = map.StaticPropLump[i];
						//Debug.Log ("static prop "+i+" model number is ("+prop.PropType+")");
						if(prop.PropType > map.staticPropDict.Length)
						{
							Debug.LogWarning ("Static prop "+i+" model number is ("+prop.PropType+")");
							continue;
						}
						string modelName = map.staticPropDict[prop.PropType];
						//Debug.Log ("static prop "+i+" model name is "+modelName);
						GameObject go = new GameObject();
						//GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
						go.name = "prop "+modelName;
						go.transform.parent=propsObject.transform;

						//GameObject testGo = (GameObject)MonoBehaviour.Instantiate(TempProp) as GameObject;
						//testGo.transform.parent=go.transform;

						go.transform.position = prop.Origin;
						go.transform.rotation = Quaternion.Euler(prop.Angles);


						SourceStudioModel tempModel = ResourceManager.Inst.GetModel(modelName);
						if(tempModel == null)
						{
							//Debug.LogWarning("Error loading: "+modelName);
							GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
							prim.transform.parent=go.transform;
							prim.transform.localPosition=Vector3.zero;
						}
						else
						{
							tempModel.GetInstance(go,false,0);
						}

						go.isStatic = true;
						Props.Add(go);

					}
					Debug.Log("Finish Static Props");
				}
				break;

			case Type.WithoutBatching:
				for (int i=0; i<map.modelsLump.Length; i++) 
				{
					Models.Add(new GameObject("*"+i));
					Models[i].transform.SetParent( modelsObject.transform);
					for (int f=map.modelsLump[i].firstface; f<map.modelsLump[i].firstface+map.modelsLump[i].numfaces; f++) 
					{
						GenerateFaceObject(f).transform.SetParent(Models[i].transform);
					}
				}
				break;

			case Type.OnlyDisplacements:
				if(uSrcSettings.Inst.displacements)
				{
					for(int i=0; i<map.dispinfoLump.Length;i++)
					{
						GenerateDispFaceObject ((int)map.dispinfoLump[i].MapFace, 0);
						Debug.Log ("FaceId: " + map.dispinfoLump[i].MapFace + " DispInfoId: "+i+
						           " DispVertStart: " + map.dispinfoLump[i].DispVertStart);
					}
				}
				break;

			case Type.OneFace:
				if(uSrcSettings.Inst.displacements&map.facesLump[faceId].dispinfo!=-1)
				{
					GenerateDispFaceObject (faceId, 0);
					Debug.Log ("FaceId: "+faceId+" DispInfoId: " + map.facesLump[faceId].dispinfo+
						" DispVertStart: "+map.dispinfoLump[map.facesLump[faceId].dispinfo].DispVertStart);
				}
				else
				{
					GenerateFaceObject(faceId);
				}
				break;
			}

			BR.BaseStream.Dispose ();
		}
		 
		void ParseEntities(string input)
		{
			string pattern = @"{[^}]*}";
			foreach(Match match in Regex.Matches (input,pattern,RegexOptions.IgnoreCase))
			{
				entities.Add (match.Value);
			}
		}

		void LoadEntity(int index)
		{
			List<string> data = new List<string> ();
			string pattern = "\"[^\"]*\"";

			foreach(Match match in Regex.Matches (entities[index],pattern,RegexOptions.IgnoreCase))
				data.Add (match.Value.Trim ('"'));

			int classNameIndex = data.FindIndex (n=>n=="classname");
			string className=data[classNameIndex+1];

			if(className=="worldspawn")
			{
				//WorldSpawn(data);
				return;
			}
			
			Vector3 angles = new Vector3 (0,0,0);
			if(data[0]=="model")
			{
				int modelIndex=int.Parse(data[data.FindIndex(n=>n=="model")+1].Substring(1));
				GameObject obj = Models[modelIndex];
				
				if(data.Contains ("origin"))
				{
					if(data.FindIndex (n=>n=="origin")%2==0)
						obj.transform.position = ConvertUtils.StringToVector(data[data.FindIndex (n=>n=="origin")+1]);
				}
				
				if(data.Contains ("angles"))
				{
					string[] t=data[data.FindIndex (n=>n=="angles")+1].Split(' ');
					angles = new Vector3(-float.Parse(t[2]),-float.Parse(t[1]),-float.Parse(t[0]));
					obj.transform.eulerAngles = angles;
				}
				
				if(className=="func_illusionary")
				{
					MeshRenderer[] renderers=obj.GetComponentsInChildren<MeshRenderer>();
					for(int i=0;i<renderers.Length;i++)
					{
						renderers[i].castShadows=false;
					}
				}

				return;
			}
			string[] testEnts = new string[]{"info_player_start","sky_camera","point_camera",
			"light_environment","prop_dynamic","prop_dynamic_override"/*,"point_viewcontrol"*/,"info_target",
				"light_spot","light","info_survivor_position","env_projectedtexture","func_illusionary",
				"prop_button","prop_floor_button","prop_weighted_cube"};

			if(testEnts.Contains(className))
			{
				string targetname=null;
				
				if(data.Contains ("targetname"))
					targetname = data[data.FindIndex (n=>n=="targetname")+1];
					
				if(data.Contains ("angles"))
				{
					string[] t=data[data.FindIndex (n=>n=="angles")+1].Split(' ');
					angles = new Vector3(-float.Parse(t[2]),-float.Parse(t[1]),-float.Parse(t[0]));
				}
				
				if(data.Contains ("pitch"))
					angles.x = -float.Parse (data[data.FindIndex (n=>n=="pitch")+1]);

				GameObject obj = new GameObject(targetname ?? className);
				//if(className.Contains("light"))
				obj.transform.parent = entObject.transform;
				obj.transform.position = ConvertUtils.StringToVector(data[data.FindIndex (n=>n=="origin")+1]);
				obj.transform.eulerAngles = angles;

				/*if(className=="light")
				{
					Light l = obj.AddComponent<Light>();
					l.color = ConvertUtils.stringToColor(data[data.FindIndex (n=>n=="_light")+1],255);
				}
				if(className=="light_spot")
				{
					Light l = obj.AddComponent<Light>();
					l.type=LightType.Spot;
					l.color = ConvertUtils.stringToColor(data[data.FindIndex (n=>n=="_light")+1],255);
					//float pitch = 0;
					//if(data.Contains ("pitch"))
					//	pitch = float.Parse (data[data.FindIndex (n=>n=="pitch")+1]);
					if(data.Contains ("_cone"))
					{
						l.spotAngle = int.Parse (data[data.FindIndex (n=>n=="_cone")+1]);
					}
					//angles.y = pitch;
					angles.y+=90;
					//obj.transform.eulerAngles = angles; 
				}*/

				if(className=="light_environment"&Test.Inst.light_environment!=null)
				{
					Light l=Test.Inst.light_environment;
					l.color = ConvertUtils.stringToColor(data[data.FindIndex (n=>n=="_light")+1],255);
					//float pitch = 0;
					//if(data.Contains ("pitch"))
					//	pitch = float.Parse (data[data.FindIndex (n=>n=="pitch")+1]);
					//angles.y = pitch;
					angles.y+=90;
					l.transform.eulerAngles = angles; 
				}
				 
				if(className=="sky_camera")
				{
					Test.Inst.skyCameraOrigin=obj.transform.position;
					if(Test.Inst.skyCamera!=null)
					{
						//Test.Inst.skyCamera.transform.SetParent(obj.transform);
						//Test.Inst.skyCamera.transform.localPosition=Vector3.zero;
						Test.Inst.skyCamera.transform.localPosition=(Test.Inst.playerCamera.transform.position/16)+Test.Inst.skyCameraOrigin;
						Test.Inst.skyCamera.transform.rotation=Test.Inst.playerCamera.transform.rotation;
					}
				}

				if(!Test.Inst.isL4D2)
				{
					if(className=="info_player_start")
					{
						Test.Inst.startPos=obj.transform.position;
					}
				}
				else
				{
					if(className=="info_survivor_position")
					{
						Test.Inst.startPos=obj.transform.position;
					}
				}

				if((className=="prop_dynamic"|className=="prop_dynamic_override"|className=="prop_weighted_cube"|className=="prop_floor_button"|className=="prop_button")&&uSrcSettings.Inst.propsDynamic)
				{
					string modelName="";
					
					if(data.Contains ("model"))
						modelName=data[data.FindIndex (n=>n=="model")+1];
					else if(className=="prop_weighted_cube")
						modelName="models/props/metal_box.mdl";
					else if(className=="prop_floor_button")
						modelName="models/props/portal_button.mdl";
					else if(className=="prop_button")
						modelName="models/props/switch001.mdl";

					//angles.y-=90;
					//Kostyl
					//if(modelName.Contains("signage_num0"))
					//	angles.y+=90;
					//======
					obj.transform.eulerAngles = angles;

					SourceStudioModel tempModel = ResourceManager.Inst.GetModel(modelName);
					if(tempModel==null||!tempModel.loaded)
					{
						//Debug.LogWarning("Error loading: "+modelName);
						GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
						prim.name=modelName;
						prim.transform.parent=obj.transform;
						prim.transform.localPosition=Vector3.zero;
					}
					else
					{
						tempModel.GetInstance(obj, true,0);
					}

				}
				
				/*if(className=="prop_floor_button")
				{
					string modelname="models/props/button_base_reference.mdl";
					SourceStudioModel baseModel = ResourceManager.Inst.GetModel(modelName);
					if(baseModel==null||!baseModel.loaded)
					{
						Debug.LogWarning("Error loading: "+modelName);
					}
					else
					{
						GameObject baseObj=new GameObject("button_base");
						baseObj.transform.SetParent(go.transform);
						baseModel.GetInstance(baseObj, true,0);
					}
					
					modelname="models/props/button_top_reference.mdl";
					SourceStudioModel topModel = ResourceManager.Inst.GetModel(modelName);
					if(topModel==null||!topModel.loaded)
					{
						Debug.LogWarning("Error loading: "+modelName);
					}
					else
					{
						GameObject topObj=new GameObject("button_base");
						topObj.transform.SetParent(go.transform);
						topModel.GetInstance(topObj, true,0);
					}
						
					obj.transform.eulerAngles = angles;
				}*/
			}
		}

//==================================================================================================
		int curLightmap = 0;
		public void CreateModelObject(int index)
		{
			int numFaces = map.modelsLump[index].numfaces;
			int firstFace = map.modelsLump[index].firstface;

			GameObject model = new GameObject ("*" + index);
			Models.Add (model);
			model.transform.SetParent( modelsObject.transform);
			model.isStatic = true;

			if(numFaces==0)
			{
				return;
			}

			if(uSrcSettings.Inst.lightmaps && map.hasLightmaps)
				model.layer=8;

			bspface face;
			bsptexdata texdata;
			int flags;

			if(uSrcSettings.Inst.textures)
			{
				for (int i = firstFace; i < firstFace + numFaces; i++) 
				{
					face = map.facesLump[i];
					texdata = map.texdataLump[map.texinfosLump[face.texinfo].texdata];
					if(texdata.faces==null)
					{
						if(Test.Inst.skipSky&(map.texinfosLump[face.texinfo].flags & SourceBSPStructs.SURF_SKY) != 0)
							continue;
						else
							texdata.faces=new List<int>();

					}

					if(face.dispinfo==-1)
					{
						texdata.faces.Add(i);
						texdata.numvertices+=face.numedges;
					}
				}
			}


			Texture2D  modelLightmap = null;
			List<Texture2D> tempLightmaps=null;
			if (uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255) 
			{
				tempLightmaps = new List<Texture2D>();
				modelLightmap = new Texture2D(64,64,TextureFormat.RGB24,true,true);
			}

			//int numlightfaces = numFaces;
			if (!uSrcSettings.Inst.textures) 
			{
				List<Vector3> verts=new List<Vector3>();
				List<Vector2> UVs=new List<Vector2>();
				List<Vector2> UV2s=new List<Vector2>();
				List<int> tris = new List<int>();

				int pointOffs=0;

				for (int i = firstFace; i < firstFace + numFaces; i++) 
				{
					surface surf = BuildFace (i);

					if ((Test.Inst.skipSky&(surf.texFlags & SourceBSPStructs.SURF_SKY) != 0)|surf.dispinfo!=-1)
						continue;
					
					pointOffs = verts.Count;

					verts.AddRange (surf.points);
					UVs.AddRange (surf.uv);
					UV2s.AddRange (surf.uv2);

					for (int j = 0; j < surf.triangles.Length; j++) 
					{
						tris.Add (surf.triangles [j] + pointOffs);
					}

					if (uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255&&(surf.texFlags & SourceBSPStructs.SURF_NOLIGHT) == 0)
					{
						tempLightmaps.Add(CreateLightmapTex (surf));
						if(tempLightmaps.Last()==null)
						{
							Debug.Log("Face "+i+" haven't lightmap. Model "+index+". Flags "+surf.texFlags );
						}
					}
				}

				Rect[] offsets = null;
				if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
				{
					offsets = modelLightmap.PackTextures (tempLightmaps.ToArray(),1);

					for(int i=0;i<tempLightmaps.Count;i++)
					{
						Destroy(tempLightmaps[i]);
					}
					tempLightmaps.Clear();
				}
				tempLightmaps = null;

				if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
				{
					int curVert=0;
					int curRect=0;

					//Debug.Log ("Offsets count "+offsets.Length);

					for(int i = 0; i < numFaces; i++)
					{
						face = map.facesLump[i+firstFace];
						flags = map.texinfosLump [face.texinfo].flags;
						if ((Test.Inst.skipSky&((flags & SourceBSPStructs.SURF_SKY) != 0))|face.dispinfo!=-1)
							continue;

						int numVerts = map.facesLump[firstFace+i].numedges;

						if((flags & SourceBSPStructs.SURF_NOLIGHT) != 0)
						{
							curVert+=numVerts;
							continue;
						}

						Rect offs=offsets[curRect];

						for(int v=curVert; v<curVert+numVerts; v++)
						{	
							//if(i<maxLMs)
							//{
								Vector2 tempUV = UV2s[v];
								UV2s[v] = new Vector2((tempUV.x*offs.width)+offs.x, (tempUV.y*offs.height)+offs.y);
							//}
							//else
							//	UV2s[v] = new Vector2(0.9f,0.9f);

						}
						curVert+=numVerts;
						curRect++;
					}

					lightmapsData.Add( new LightmapData(){lightmapFar=modelLightmap });
				}
				
				
				MeshRenderer mr = model.AddComponent<MeshRenderer>();
				mr.material = Test.Inst.testMaterial;
				
				MeshFilter mf = model.AddComponent<MeshFilter>();
				
				mf.mesh = new Mesh();
				mf.mesh.name = "BSPModel_"+index;
				mf.mesh.vertices=verts.ToArray ();
				mf.mesh.triangles=tris.ToArray ();
				mf.mesh.uv=UVs.ToArray ();
				if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
				{
					mf.mesh.uv2=UV2s.ToArray ();
					mr.lightmapIndex = curLightmap;
					curLightmap++;
					if(curLightmap>255)
						curLightmap=255;
					//mr.lightmapIndex=lightmapsData.Count-1;[
				}

				mf.mesh.RecalculateNormals ();
				
				if(uSrcSettings.Inst.genColliders)
				{
					model.AddComponent<MeshCollider>();
				}
				
			}
			else 
			{
				List<Mesh> texMeshes=new List<Mesh>();

				for(int i=0;i<map.texdataLump.Length;i++)
				{
					texdata=map.texdataLump[i];

					if(texdata.faces==null)
					{
						//Debug.Log("TexData "+i+" has no faces");
						continue;
					}

					List<Vector3> verts=new List<Vector3>();
					List<Vector2> UVs=new List<Vector2>();
					List<Vector2> UV2s=new List<Vector2>();
					List<int> tris = new List<int>();

					for(int j=0;j<texdata.faces.Count;j++)
					{
						face=map.facesLump[texdata.faces[j]];

						surface surf=BuildFace(texdata.faces[j]);

						//if ((f.flags & 1024) != 0)
							//numlightfaces--;

						int pointOffs = verts.Count;
						for (int t = 0; t < surf.triangles.Length; t++)
						{
							tris.Add (surf.triangles [t] + pointOffs);
						}
						
						verts.AddRange (surf.points);
						UVs.AddRange (surf.uv);
						if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
						{	
							UV2s.AddRange (surf.uv2);

							if (j < maxLMs)
								tempLightmaps.Add(CreateLightmapTex (surf));
						}
					}

					string materialName = "";
					if (map.texdataStringDataLump.Length > map.texdataStringTableLump [texdata.nameStringTableID] + 92)
						materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump [texdata.nameStringTableID], 92);
					else
						materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump [texdata.nameStringTableID], map.texdataStringDataLump.Length - map.texdataStringTableLump [texdata.nameStringTableID]);
					
					materialName = materialName.ToLower ();
					
					if (materialName.Contains ("\0"))
						materialName = materialName.Remove (materialName.IndexOf ('\0'));

					GameObject texObj;
					if(texdata.faces==null)
					{
						texObj=model;
					}
					else
					{
						texObj = new GameObject(materialName);
						texObj.transform.SetParent(model.transform);
					}
					texObj.isStatic = true;
					if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps)
						texObj.layer=8;

					MeshRenderer mr = texObj.AddComponent<MeshRenderer>();
					MeshFilter mf = texObj.AddComponent<MeshFilter>();
					Mesh mesh = new Mesh();


					mesh.name = materialName;
					mesh.vertices = verts.ToArray();
					mesh.uv = UVs.ToArray();

					if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
						mesh.uv2 = UV2s.ToArray ();

					mesh.triangles = tris.ToArray();
					mesh.RecalculateNormals();
					//mf.sharedMesh = mesh;
					mf.mesh = mesh;

					texMeshes.Add(mf.sharedMesh);

					Material tempmat = ResourceManager.Inst.GetMaterial (materialName);
					mr.material = tempmat;

					if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
					{
						mr.lightmapIndex = curLightmap;
					}
					if((materialName.Contains("trigger")||materialName.Contains("fogvolume"))&&!uSrcSettings.Inst.showTriggers)
					{
					   texObj.SetActive(false);
					}

					if(uSrcSettings.Inst.genColliders)
					{
						texObj.AddComponent<MeshCollider>();
					}

				}

				//if(texMeshes.Count>0)
				//	Debug.Log ("First Mesh is "+texMeshes[0].name);

				Rect[] offsets = null;
				if(uSrcSettings.Inst.lightmaps&&map.hasLightmaps&&curLightmap<255)
				{
					offsets = modelLightmap.PackTextures (tempLightmaps.ToArray(), 1);
					//Debug.Log ("tempLightmaps count "+tempLightmaps.Count);

					for(int l=0; l<tempLightmaps.Count; l++)
					{
						Destroy(tempLightmaps[l]);
					}
					tempLightmaps.Clear();

					int curMesh = 0;
					int curVert = 0;
					int numVerts = 0;
					int curRect=0;

					for(int t=0;t<map.texdataLump.Length;t++)
					{
						texdata = map.texdataLump[t];

						if(texdata.faces==null)
							continue;

						Mesh mesh = texMeshes[curMesh];
						curMesh++;
						Vector2[] uv2 = mesh.uv2;

						//flags=map.texinfosLump [face.texinfo].flags;

						for(int f=0; f<texdata.faces.Count; f++)
						{
							if(curRect>offsets.Length)
							{
								Debug.Log ("curent offset("+curRect+") > offsets count");
								break;
							}

							if(curVert>uv2.Length)
							{
								Debug.Log ("vert index > uv2 count");
								break;
							}

							face = map.facesLump[texdata.faces[f]];
							numVerts = face.numedges;
							Rect offs = offsets[curRect];
							curRect++;
							for(int v=curVert; v<curVert+numVerts; v++)
							{	
								Vector2 tempUV = uv2[v];
								uv2[v] = new Vector2((tempUV.x*offs.width)+offs.x, (tempUV.y*offs.height)+offs.y);
							}
							curVert+=numVerts;
						}
						curVert=0;
						mesh.uv2 = uv2;

					
						texdata.faces.Clear();
						texdata.faces=null;
					}

					lightmapsData.Add( new LightmapData(){lightmapFar=modelLightmap });
					curLightmap++;
					if(curLightmap>255)
						curLightmap=255;
				}
				else
				{
					for(int t=0;t<map.texdataLump.Length;t++)
					{
						texdata = map.texdataLump[t];
						
						if(texdata.faces==null)
							continue;

						texdata.faces.Clear();
						texdata.faces=null;
					}
				}

				texMeshes.Clear();
				texMeshes=null;
			}
		}

		GameObject GenerateFaceObject(int index)
		{
			//List<Vector3> verts=new List<Vector3>();
			//List<Vector2> UVs=new List<Vector2>();
			//List<Vector2> lightmapUV=new List<Vector2>();
			//List<int> tris = new List<int>();
			
			surface f = BuildFace (index);
			//f.index = index;

			//if((f.flags & 4) == 4)
			//	return;
				

			
			//tris.AddRange (f.triangles);
			//verts.AddRange (f.points);
			//UVs.AddRange (f.uv);
			//lightmapUV.AddRange(f.uv2);
			
			GameObject faceObject = new GameObject ("Face: "+index);
			
			MeshRenderer mr = faceObject.AddComponent<MeshRenderer>();
			
			//Material mat;
			MeshFilter mf = faceObject.AddComponent<MeshFilter>();
			mf.mesh=new Mesh();
			mf.mesh.name = "BSPFace "+index;
			mf.mesh.vertices=f.points;
			mf.mesh.triangles=f.triangles;
			mf.mesh.uv=f.uv;



			//=======================//?????????
			/*
			Vector4[] tang = new Vector4[verts.Count];
			for (int i=0; i<verts.Count; i++)
				tang [i] = new Vector4 (0, 0, 1);

			mf.mesh.tangents = tang;
			*/
			//========================

			if (uSrcSettings.Inst.textures) 
			{
				bsptexdata curTexData = map.texdataLump[map.texinfosLump[map.facesLump[index].texinfo].texdata];

				//===========================Material=============================
				string materialName = "";
				//VMTLoader.VMTFile vmtFile=null;
				//Material tempmat=null;
				
				//string
				if (map.texdataStringDataLump.Length > map.texdataStringTableLump [curTexData.nameStringTableID] + 92)
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump [curTexData.nameStringTableID], 92);
				else
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump [curTexData.nameStringTableID], map.texdataStringDataLump.Length - map.texdataStringTableLump [curTexData.nameStringTableID]);
				
				materialName = materialName.ToLower ();
				
				if (materialName.Contains ("\0"))
					materialName = materialName.Remove (materialName.IndexOf ('\0'));
				
				//return VMTLoader.GetMaterial (materialName);

				Material tempmat = ResourceManager.Inst.GetMaterial (materialName);
				//=================Material End========================


				//if (tempmat.name.Contains ("trigger")||tempmat.name.Contains ("fogvolume")||tempmat.name.Contains ("tools/toolsskybox")) 
				//	mr.enabled = false;

				mr.material = tempmat;
				
				if (uSrcSettings.Inst.lightmaps && map.hasLightmaps)// & !vmtFile.translucent)
				{

					//Texture2D lightMap = CreateLightmapTex (f);

					mf.mesh.uv2 = f.uv2;
					mr.lightmapIndex=0;
				}
			}
			else
			{
				mr.material = Test.Inst.testMaterial;
			}
			
			mf.mesh.RecalculateNormals ();
			//mf.mesh.RecalculateBounds ();

			faceObject.transform.parent = modelsObject.transform;

			mf.mesh.Optimize ();
			faceObject.isStatic = true;

			return faceObject;
		}

		void GenerateDispFaceObject(int index, int model)
		{
			surface f = BuildDispFace (index, model, map.facesLump[index].dispinfo);

			GameObject faceObject = new GameObject ("DispFace: "+f.index);
			
			MeshRenderer mr = faceObject.AddComponent<MeshRenderer>();
			
			//Material mat;
			MeshFilter mf = faceObject.AddComponent<MeshFilter>();
			mf.mesh = new Mesh();
			mf.mesh.name = "DispFace "+f.index;
			mf.mesh.vertices=f.points;
			mf.mesh.triangles = f.triangles;
			mf.mesh.uv=f.uv;
			mf.mesh.colors32=f.cols;

			if (uSrcSettings.Inst.textures) 
			{
				bsptexdata curTexData = map.texdataLump[map.texinfosLump[map.facesLump[index].texinfo].texdata];

				//===========================Material=============================
				string materialName = "";
				//VMTLoader.VMTFile vmtFile=null;
				//Material tempmat=null;
				
				//string
				if (map.texdataStringDataLump.Length > map.texdataStringTableLump [curTexData.nameStringTableID] + 92)
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump [curTexData.nameStringTableID], 92);
				else
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump [curTexData.nameStringTableID], map.texdataStringDataLump.Length - map.texdataStringTableLump [curTexData.nameStringTableID]);
				
				materialName = materialName.ToLower ();
				
				if (materialName.Contains ("\0"))
					materialName = materialName.Remove (materialName.IndexOf ('\0'));
				
				Material tempmat = ResourceManager.Inst.GetMaterial (materialName);
				//=================Material End========================

				mr.material = tempmat;
				
			}
			else
			{
				mr.material = Test.Inst.testMaterial;
			}

			mf.mesh.RecalculateNormals ();

			faceObject.transform.parent = dispObject.transform;
			mf.mesh.Optimize ();
			faceObject.isStatic = true;

			if(uSrcSettings.Inst.genColliders)
			{
				faceObject.AddComponent<MeshCollider>();
			}
		}

		struct surface
		{
			public int index;
			public int texFlags;
			public int texID;
			public short dispinfo;
			
			public Vector3[] points;
			public Vector2[] uv;
			public Vector2[] uv2;
			public Color32[] cols;
			public int[] triangles;
			
			public int lightMapW;
			public int lightMapH;
			
			public Vector2 lightmapScale;
			public Vector2 lightmapOffset;
		}
		
		surface BuildFace(int index)
		{
			List<Vector3> verts=new List<Vector3>();
			List<Vector2> UVs=new List<Vector2>();
			List<Vector2> UV2s=new List<Vector2>();
			List<int> tris = new List<int>();

			bspface curface = map.facesLump[index];
			int startEdge = curface.firstedge;
			int nEdges = curface.numedges;
			//Debug.Log("texinfo "+curface.texinfo+"/"+map.texinfosLump.Length);
			int tiFlags = map.texinfosLump [curface.texinfo].flags;

			for(int i = startEdge; i<startEdge+nEdges; i++)
			{
				verts.Add( map.surfedgesLump[i]>0 ? 
				          map.vertexesLump[map.edgesLump[Mathf.Abs(map.surfedgesLump[i])][0]] : 
				          map.vertexesLump[map.edgesLump[Mathf.Abs (map.surfedgesLump[i])][1]]);
			}

			for(int i = 1; i < verts.Count - 1; i++)
			{
				tris.Add (0);
				tris.Add (i);
				tris.Add (i+1);
			}

			bsptexinfo texInfo = map.texinfosLump [map.facesLump [index].texinfo];

			float scales = map.texdataLump[texInfo.texdata].width * uSrcSettings.Inst.worldScale;
			float scalet = map.texdataLump[texInfo.texdata].height * uSrcSettings.Inst.worldScale;
			
			for(int i = 0; i < verts.Count; i++)
			{
				float tU = Vector3.Dot(verts[i] * uSrcSettings.Inst.worldScale, texInfo.texvecs) + (texInfo.texoffs * uSrcSettings.Inst.worldScale);
				float tV = Vector3.Dot(verts[i] * uSrcSettings.Inst.worldScale, texInfo.texvect) + (texInfo.texofft * uSrcSettings.Inst.worldScale);
				UVs.Add( new Vector2(tU / scales, tV / scalet));
			}

			int lightmapW = (map.facesLump[index].LightmapTextureSizeInLuxels[0]+1);
			int lightmapH = (map.facesLump[index].LightmapTextureSizeInLuxels[1]+1);

			for (int i=0; i<verts.Count; i++) 
			{
				float U = Vector3.Dot(verts[i],texInfo.lightvecs) + texInfo.lightoffs+0.5f - map.facesLump[index].LightmapTextureMinsInLuxels[0];
				float V = Vector3.Dot(verts[i],texInfo.lightvect) + texInfo.lightofft+0.5f - map.facesLump[index].LightmapTextureMinsInLuxels[1];
				//U=(U*(float)((float)lightmapW/((float)lightmapW+2f)))+(1f/((float)lightmapW+2f));
				//V=(V*(float)((float)lightmapH/((float)lightmapH+2f)))+(1f/((float)lightmapH+2f));
				UV2s.Add (new Vector2(U/(lightmapW), V/(lightmapH)));
			}

			for(int i=0;i<verts.Count;i++)
			{
				verts[i] *= uSrcSettings.Inst.worldScale;
			}

			int texID = map.texdataLump [map.texinfosLump [map.facesLump [index].texinfo].texdata].nameStringTableID;

			surface f = new surface ();
			f.index = index;
			f.texFlags = tiFlags;
			f.texID = texID;
			f.dispinfo=curface.dispinfo;

			f.points = verts.ToArray ();
			f.uv = UVs.ToArray ();
			f.uv2 = UV2s.ToArray ();
			f.triangles = tris.ToArray ();

			f.lightMapW = lightmapW;
			f.lightMapH = lightmapH;
			
			return f;
		}

		surface BuildDispFace(int faceIndex, int model, short dispinfoId)
		{
			Vector3[] vertices = new Vector3[4];
			List<Vector3> disp_verts = new List<Vector3>();
			List<Vector2> UVs = new List<Vector2>();
			List<Color32> cols=new List<Color32>();
			List<int> indices = new List<int>();

			bspface curFace = map.facesLump[faceIndex];

			bsptexinfo curTexInfo = map.texinfosLump[curFace.texinfo];
			bsptexdata curTexData = map.texdataLump[curTexInfo.texdata];


			int fEdge = curFace.firstedge;

			for(int i = 0; i<curFace.numedges; i++)
			{
				vertices[i] = (map.surfedgesLump[fEdge+i]>0 ? 
					map.vertexesLump[map.edgesLump[Mathf.Abs (map.surfedgesLump[fEdge+i])][0]] : 
					map.vertexesLump[map.edgesLump[Mathf.Abs (map.surfedgesLump[fEdge+i])][1]]);
			}

			bspdispinfo curDisp = map.dispinfoLump [dispinfoId];
			Vector3 startPos = curDisp.startPosition;

			float dist;
			float minDist = 0.1f;
			int minIndex = 0;

			for (int i=0; i<4; i++) 
			{
				dist=Vector3.Distance(startPos,vertices[i]);

				if(dist<minDist)
				{
					minDist=dist;
					minIndex=i;
				}
			}

			Vector3 temp;

			for (int i=0; i<minIndex; i++) 
			{
				temp = vertices[0];
				vertices[0] = vertices[1];
				vertices[1] = vertices[2];
				vertices[2] = vertices[3];
				vertices[3] = temp;
			}

			Vector3 leftEdge = vertices[1] - vertices[0];
			Vector3 rightEdge = vertices[2] - vertices[3];


			int numEdgeVertices = (1 << curDisp.power) + 1;

			float subdivideScale=1.0f/(float)(numEdgeVertices-1);

			Vector3 leftEdgeStep = leftEdge * subdivideScale;
			Vector3 rightEdgeStep = rightEdge * subdivideScale;

			int firstVertex=0;

			Vector3 leftEnd;
			Vector3 rightEnd;
			Vector3 leftRightSeg;
			Vector3 leftRightStep;

			int dispVertIndex;
			bspDispVert dispVert;

			Vector3 flatVertex;
			Vector3 dispVertex;

			float scaleU = (float)1f/curTexData.width;
			float scaleV = (float)1f/curTexData.height;

			for(int i=0; i<numEdgeVertices; i++)
			{
				leftEnd = leftEdgeStep*(float)i;
				leftEnd += vertices[0];
				rightEnd = rightEdgeStep*(float)i;
				rightEnd += vertices[3];

				leftRightSeg=rightEnd-leftEnd;
				leftRightStep=leftRightSeg*subdivideScale;

				for(int j=0; j<numEdgeVertices; j++)
				{
					dispVertIndex=curDisp.DispVertStart;
					dispVertIndex+=i*numEdgeVertices+j;
					dispVert=map.dispVertsLump[dispVertIndex];

					flatVertex=leftEnd+(leftRightStep*(float)j);

					dispVertex=dispVert.vec*(dispVert.dist/* *scale*/);
					dispVertex+=flatVertex;

					disp_verts.Add (dispVertex);


					float tU = Vector3.Dot(flatVertex, curTexInfo.texvecs) + (curTexInfo.texoffs);
					float tV = Vector3.Dot(flatVertex, curTexInfo.texvect) + (curTexInfo.texofft);
					UVs.Add( new Vector2(tU * scaleU, tV * scaleV));
			
					cols.Add(new Color32((byte)(dispVert.alpha),0,0,0));
				}
			}

			int curIndex;

			for (int i=0; i<numEdgeVertices-1; i++) 
			{
				for (int j=0; j<numEdgeVertices-1; j++) 
				{
					curIndex = i * numEdgeVertices + j;

					if((curIndex % 2)==1)
					{
						curIndex += firstVertex;

						indices.Add(curIndex + 1);
						indices.Add(curIndex );
						indices.Add(curIndex + numEdgeVertices);
						indices.Add(curIndex + numEdgeVertices + 1);
						indices.Add(curIndex + 1);
						indices.Add(curIndex + numEdgeVertices);
					}
					else
					{
						curIndex += firstVertex;

						indices.Add(curIndex );
						indices.Add(curIndex + numEdgeVertices);
						indices.Add(curIndex + numEdgeVertices + 1);
						indices.Add(curIndex + 1);
						indices.Add(curIndex );
						indices.Add(curIndex + numEdgeVertices + 1);
					}
				}
			}


			for(int i=0;i<disp_verts.Count;i++)
			{
				disp_verts[i] *= uSrcSettings.Inst.worldScale;
			}
			
			
			surface f = new surface ();
			f.index = faceIndex;
			//f.flags = flags;

			f.dispinfo = dispinfoId;

			f.points = disp_verts.ToArray();
			f.uv = UVs.ToArray();
			//f.uv2 = UV2s;
			f.cols=cols.ToArray();
			f.triangles = indices.ToArray ();

			//f.lightMapW = lightmapW;
			//f.lightMapH = lightmapH;

			return f;
		}

		Texture2D CreateLightmapTex(surface f)
		{
			int rowColors=f.lightMapW;
			Texture2D tex = new Texture2D (f.lightMapW, f.lightMapH, TextureFormat.RGB24, false,true);
			//tex.filterMode = FilterMode.Point;
			Color32[] colors = new Color32[(f.lightMapW) * (f.lightMapH)];

			int Offset = map.facesLump [f.index].lightofs/4;

			if(map.facesLump [f.index].lightofs<0 | map.lightingLump.Length == 0)
				return null;

			int o = 0;
			int j = 0;
			for(int y=0; y<f.lightMapH; y++)
			{
				o=(rowColors*(y));
				for(int x=0; x<f.lightMapW; x++)
				{
					colors[o++]=map.lightingLump[Offset + j++];
				}
			}
			//=======fill borders================
			/*
			for(int y=0; y<f.lightMapH+1;y++)
			{
				o=rowColors * y;
				colors[o] = colors[o+1];
				
				o=(rowColors * (y+1))-1;
				colors[o] = colors[o-1];
			}
			
			int end=(f.lightMapW+2)*(f.lightMapH+2);
			for(int x=0; x<f.lightMapW+2;x++)
			{
				colors[x] = colors[x+rowColors];
				colors[(end-rowColors)+x] = colors[(end-(rowColors*2)+x)];
			}
			*/
			//=====================================

			tex.SetPixels32 (colors);
			tex.Apply ();
			return tex;
		}
	}
}
