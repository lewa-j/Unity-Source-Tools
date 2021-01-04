using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

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

		public GameObject[] models;
		public List<GameObject> Props;

		public List<string> entities;

		//string materaialsToLoad;
		private string LevelName;

		public Type LoadType = Type.Full;
		public int faceId = 0;
		private bool loaded = false;

		public List<LightmapData> lightmapsData;
		private const int BLOCK_SIZE = 1024;
		int[] lm_allocated;
		Texture2D curLightmapTex;
		int curLightmap = 0;

		public void Load (string mapName)
		{
		
#if UNITY_WEBGL
			print("Unity Source Tools does not support WebGL due to not being able to access local files.");
			return;
#endif
			if (loaded)
			{
				Debug.LogWarning ("Already loaded");
				//return; В теории это не требуется. Да и мешается.
			}
			LevelName = mapName;

			string path = "";

			path = ResourceManager.GetPath ("maps/" + LevelName + ".bsp");

			if (path == null)
			{
				print ("No map detected. Check file path.");
				print (path);
				return;
			}

            using (BinaryReader BR = new BinaryReader(File.Open(path, FileMode.Open)))
            {

                map = new BSPFile(BR, LevelName);
                loaded = true;

                if (uSrcSettings.Inst.entities)
                {
                    ParseEntities(map.entitiesLump);
                }

                //===================================================
                mapObject = new GameObject(LevelName);
                mapObject.isStatic = true;

                modelsObject = new GameObject("models");
                modelsObject.transform.SetParent(mapObject.transform);
                modelsObject.isStatic = true;

                if (uSrcSettings.Inst.displacements)
                {
                    dispObject = new GameObject("displacements");
                    dispObject.transform.SetParent(mapObject.transform);
                }

                if (uSrcSettings.Inst.props)
                {
                    propsObject = new GameObject("props");
                    propsObject.transform.SetParent(mapObject.transform);
                    propsObject.isStatic = true;
                }

                switch (LoadType)
                {
                    case Type.Full:
                        Debug.Log("Start Loading World Faces");

                        if (uSrcSettings.Inst.lightmaps & map.hasLightmaps)
                        {
                            lightmapsData = new List<LightmapData>();
                            curLightmap = 0;
                            lm_allocated = new int[BLOCK_SIZE];
                            LM_InitBlock();
                        }

                        models = new GameObject[map.modelsLump.Length];
                        for (int i = 0; i < map.modelsLump.Length; i++)
                        {
                            CreateModelObject(i);
                        }

                        if (uSrcSettings.Inst.lightmaps & map.hasLightmaps)
                        {
                            LM_UploadBlock();
                            Debug.Log("Loading " + lightmapsData.Count + " lightmap pages");
                            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
                            LightmapSettings.lightmaps = lightmapsData.ToArray();
                            lm_allocated = null;
                        }
                        Debug.Log("Finish World Faces");
                        GC.Collect();

                        if (uSrcSettings.Inst.entities)
                        {
                            Debug.Log("Start Loading Entities");

                            entObject = new GameObject("entities");
                            entObject.transform.parent = mapObject.transform;

                            for (int i = 0; i < entities.Count; i++)
                            {
                                LoadEntity(i);
                            }
                            Debug.Log("Finish Entities");
                        }

                        if (uSrcSettings.Inst.displacements)
                        {
                            for (int m = 0; m < map.modelsLump.Length; m++)
                            {
                                for (int i = map.modelsLump[m].firstface; i < map.modelsLump[m].firstface + map.modelsLump[m].numfaces; i++)
                                {
                                    if (map.facesLump[i].dispinfo != -1)
                                    {
                                        GenerateDispFaceObject(i, m);
                                    }
                                }
                            }
                            Debug.Log("Finish Displacements");
                        }

                        //Static props
                        if (Props == null) Props = new List<GameObject>();
                        if (uSrcSettings.Inst.props && map.staticPropsReaded)
                        {
                            Debug.Log("Start Loading Static props");
                            for (int i = 0; i < map.StaticPropCount; i++)
                            {
                                bspStaticPropLump prop = map.StaticPropLump[i];
                                //Debug.Log ("static prop "+i+" model number is ("+prop.PropType+")");
                                if (prop.PropType > map.staticPropDict.Length)
                                {
                                    Debug.LogWarning("Static prop " + i + " model number is (" + prop.PropType + ")");
                                    continue;
                                }
                                string modelName = map.staticPropDict[prop.PropType];
                                //Debug.Log ("static prop "+i+" model name is "+modelName);
                                GameObject go = new GameObject();
                                //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                go.name = "prop " + modelName;
                                go.transform.parent = propsObject.transform;

                                //GameObject testGo = (GameObject)MonoBehaviour.Instantiate(TempProp) as GameObject;
                                //testGo.transform.parent=go.transform;

                                go.transform.position = prop.Origin;
                                go.transform.rotation = Quaternion.Euler(prop.Angles);
                                if (prop.UniformScale != 0.0f)
                                    go.transform.localScale = new Vector3(prop.UniformScale, prop.UniformScale, prop.UniformScale);

                                SourceStudioModel tempModel = ResourceManager.Inst.GetModel(modelName);
                                if (tempModel == null)
                                {
                                    //Debug.LogWarning("Error loading: "+modelName);
                                    GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    prim.transform.parent = go.transform;
                                    prim.transform.localPosition = Vector3.zero;
                                }
                                else
                                {
                                    tempModel.GetInstance(go, false, 0);
                                }

                                go.isStatic = true;
                                Props.Add(go);

                            }
                            Debug.Log("Finish Static Props");
                        }
                        break;

                    case Type.WithoutBatching:
                        for (int i = 0; i < map.modelsLump.Length; i++)
                        {
                            models[i] = new GameObject("*" + i);
                            models[i].transform.SetParent(modelsObject.transform);
                            for (int f = map.modelsLump[i].firstface; f < map.modelsLump[i].firstface + map.modelsLump[i].numfaces; f++)
                            {
                                GenerateFaceObject(f).transform.SetParent(models[i].transform);
                            }
                        }
                        break;

                    case Type.OnlyDisplacements:
                        if (uSrcSettings.Inst.displacements)
                        {
                            for (int i = 0; i < map.dispinfoLump.Length; i++)
                            {
                                GenerateDispFaceObject((int)map.dispinfoLump[i].MapFace, 0);
                                Debug.Log("FaceId: " + map.dispinfoLump[i].MapFace + " DispInfoId: " + i +
                                    " DispVertStart: " + map.dispinfoLump[i].DispVertStart);
                            }
                        }
                        break;

                    case Type.OneFace:
                        if (uSrcSettings.Inst.displacements & map.facesLump[faceId].dispinfo != -1)
                        {
                            GenerateDispFaceObject(faceId, 0);
                            Debug.Log("FaceId: " + faceId + " DispInfoId: " + map.facesLump[faceId].dispinfo +
                                " DispVertStart: " + map.dispinfoLump[map.facesLump[faceId].dispinfo].DispVertStart);
                        }
                        else
                        {
                            GenerateFaceObject(faceId);
                        }
                        break;
                }

            }
			GC.Collect ();
            UnsetResources();
		}

		void ParseEntities (string input)
		{
			entities = new List<string> ();
			string pattern = @"{[^}]*}";
			foreach (Match match in Regex.Matches (input, pattern, RegexOptions.IgnoreCase))
			{
				entities.Add (match.Value);
			}
		}

		void LoadEntity (int index)
		{
			List<string> data = new List<string> ();
			string pattern = "\"[^\"]*\"";

			foreach (Match match in Regex.Matches (entities[index], pattern, RegexOptions.IgnoreCase))
				data.Add (match.Value.Trim ('"'));

			int classNameIndex = data.FindIndex (n => n == "classname");
			string className = data[classNameIndex + 1];

			if (className == "worldspawn")
			{
			    if(data.Contains("skyname")) // Skybox loading
			    {
			       string sky = data[data.FindIndex(n => n == "skyname") + 1];
			       Material SkyMaterial = new Material(Shader.Find("Skybox/6 Sided"));
			       
			       Texture FrontTex = VTFLoader.LoadFile("skybox/" + sky + "ft");
                               FrontTex.wrapMode = TextureWrapMode.Repeat;
			       
			       Texture BackTex = VTFLoader.LoadFile("skybox/" + sky + "bk");
                    	       BackTex.wrapMode = TextureWrapMode.Repeat;
			       
			       Texture LeftTex = VTFLoader.LoadFile("skybox/" + sky + "lf");
                    	       LeftTex.wrapMode = TextureWrapMode.Repeat;
			       
			       Texture RightTex = VTFLoader.LoadFile("skybox/" + sky + "rt");
                               RightTex.wrapMode = TextureWrapMode.Repeat;
			       
			       Texture DownTex = VTFLoader.LoadFile("skybox/" + sky + "dn");
                    	       DownTex.wrapMode = TextureWrapMode.Repeat;
			       
			       Texture UpTex = VTFLoader.LoadFile("skybox/" + sky + "up");
                               UpTex.wrapMode = TextureWrapMode.Repeat;
			       
			       //if any of you can code it so that the up texture of the skybox
			       //gets rotated by 90 that would be perfect -Jhrino
			       
			      SkyMaterial.SetTexture("_FrontTex", FrontTex);
                   	      SkyMaterial.SetTexture("_BackTex", BackTex);
                              SkyMaterial.SetTexture("_LeftTex", LeftTex);
                              SkyMaterial.SetTexture("_RightTex", RightTex);
                              SkyMaterial.SetTexture("_DownTex", DownTex);
                              SkyMaterial.SetTexture("_UpTex", UpTex);
			      
			      RenderSettings.skybox = SkyMaterial;	       
			    }
				return;
			}

			Vector3 angles = new Vector3 (0, 0, 0);
			if (data[0] == "model")
			{
				int modelIndex = int.Parse (data[data.FindIndex (n => n == "model") + 1].Substring (1));
				GameObject obj = models[modelIndex];

				if (data.Contains ("origin"))
				{
					if (data.FindIndex (n => n == "origin") % 2 == 0)
						obj.transform.position = ConvertUtils.StringToVector (data[data.FindIndex (n => n == "origin") + 1]);
				}

				if (data.Contains ("angles"))
				{
					string[] t = data[data.FindIndex (n => n == "angles") + 1].Split (' ');
					angles = new Vector3 (-ConvertUtils.floatParse(t[2]), -ConvertUtils.floatParse(t[1]), -ConvertUtils.floatParse(t[0]));
					angles.y -= angles.y * 2;
					obj.transform.eulerAngles = angles;
				}

				if (className == "func_illusionary")
				{
					MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer> ();
					for (int i = 0; i < renderers.Length; i++)
					{
						renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					}
				}

				return;
			}
			string[] testEnts = new string[]
			{
				"info_player_start",
				"sky_camera",
				"point_camera",
				"light_environment",
				"prop_dynamic",
				"prop_dynamic_override" /*,"point_viewcontrol"*/ ,
				"info_target",
				"light_spot",
				"light",
				"info_survivor_position",
				"env_projectedtexture",
				"func_illusionary",
				"prop_button",
				"prop_floor_button",
				"prop_weighted_cube",
				"prop_physics"
			};

			if (testEnts.Contains (className))
			{
				string targetname = null;

				if (data.Contains ("targetname"))
					targetname = data[data.FindIndex (n => n == "targetname") + 1];

				if (data.Contains ("angles"))
				{
					string[] t = data[data.FindIndex (n => n == "angles") + 1].Split (' ');
					angles = new Vector3 (-ConvertUtils.floatParse(t[2]), -ConvertUtils.floatParse(t[1]), -ConvertUtils.floatParse(t[0]));
					angles.y -= angles.y * 2;
				}

				if (data.Contains ("pitch"))
					angles.x = -ConvertUtils.floatParse(data[data.FindIndex (n => n == "pitch") + 1]);

				GameObject obj = new GameObject (targetname ?? className);
				//if(className.Contains("light"))
				obj.transform.parent = entObject.transform;
				obj.transform.position = ConvertUtils.StringToVector (data[data.FindIndex (n => n == "origin") + 1]);
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

				if (className == "light_environment" & Test.Inst.light_environment != null)
				{
					Light l = Test.Inst.light_environment;
					l.color = ConvertUtils.stringToColor (data[data.FindIndex (n => n == "_light") + 1], 255);
					//float pitch = 0;
					//if(data.Contains ("pitch"))
					//	pitch = float.Parse (data[data.FindIndex (n=>n=="pitch")+1]);
					//angles.y = pitch;
					angles.y += 90;
					l.transform.eulerAngles = angles;
				}

				if (className == "sky_camera")
				{
					Test.Inst.skyCameraOrigin = obj.transform.position;
					if (Test.Inst.skyCamera != null)
					{
						//Test.Inst.skyCamera.transform.SetParent(obj.transform);
						//Test.Inst.skyCamera.transform.localPosition=Vector3.zero;
						Test.Inst.skyCamera.transform.localPosition = (Test.Inst.playerCamera.transform.position / 16) + Test.Inst.skyCameraOrigin;
						Test.Inst.skyCamera.transform.rotation = Test.Inst.playerCamera.transform.rotation;
					}
				}

				if (!Test.Inst.isL4D2)
				{
					if (className == "info_player_start")
					{
						Test.Inst.startPos = obj.transform.position;
					}
				}
				else
				{
					if (className == "info_survivor_position")
					{
						Test.Inst.startPos = obj.transform.position;
					}
				}
				if ((className == "prop_dynamic" | className == "prop_dynamic_override" | className == "prop_weighted_cube" | className == "prop_floor_button" | className == "prop_button" | className == "prop_physics") && uSrcSettings.Inst.propsDynamic)
				{
					string modelName = "";

					if (data.Contains ("model"))
						modelName = data[data.FindIndex (n => n == "model") + 1];
					else if (className == "prop_weighted_cube")
						modelName = "models/props/metal_box.mdl";
					else if (className == "prop_floor_button")
						modelName = "models/props/portal_button.mdl";
					else if (className == "prop_button")
						modelName = "models/props/switch001.mdl";

					//angles.y-=90;
					//Kostyl
					//if(modelName.Contains("signage_num0"))
					//	angles.y+=90;
					//======
					obj.transform.eulerAngles = angles;

					SourceStudioModel tempModel = ResourceManager.Inst.GetModel (modelName);
					if (tempModel == null || !tempModel.loaded)
					{
						//Debug.LogWarning("Error loading: "+modelName);
						GameObject prim = GameObject.CreatePrimitive (PrimitiveType.Cube);
						prim.name = modelName;
						prim.transform.parent = obj.transform;
						prim.transform.localPosition = Vector3.zero;
					}
					else
					{
						tempModel.GetInstance (obj, true, 0);
					}
					if(className == "prop_physics")
					{
						obj.GetComponent<MeshCollider>().convex = true;
						obj.AddComponent<Rigidbody>();
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

		void CreateModelObject (int index)
		{
			int numFaces = map.modelsLump[index].numfaces;
			int firstFace = map.modelsLump[index].firstface;
			int numVerts = 0;
			int numInds = 0;

			GameObject model = new GameObject ("*" + index);
			models[index] = model;
			model.transform.SetParent (modelsObject.transform);
			model.isStatic = true;

			if (numFaces == 0)
			{
				return;
			}

			if (uSrcSettings.Inst.lightmaps && map.hasLightmaps)
				model.layer = 8;

			bspface face;
			bsptexinfo ti;
			int i = 0;

			if (!uSrcSettings.Inst.textures)
			{
				for (i = firstFace; i < firstFace + numFaces; i++)
				{
					face = map.facesLump[i];
					ti = map.texinfosLump[face.texinfo];
					if (Test.Inst.skipSky && (ti.flags & SourceBSPStructs.SURF_SKY) != 0 || face.dispinfo != -1)
						continue;
					if (numVerts + face.numedges > 65000)
					{
						break;
					}
					numVerts += face.numedges;
					numInds += (face.numedges - 2) * 3;
				}
				if (i < firstFace + numFaces)
				{
					Debug.LogWarning ("skipped " + (firstFace + numFaces - i) + " faces. Load " + numVerts + " verts");
					numFaces = i - firstFace;
				}
				Debug.Log ("CreateModelObject " + index + " numVerts " + numVerts);

				Vector3[] verts = new Vector3[numVerts];
				Vector2[] UVs = new Vector2[numVerts];
				Vector2[] UV2s = new Vector2[numVerts];
				int[] tris = new int[numInds];

				int vertOffs = 0;
				int indsOffs = 0;
				for (i = firstFace; i < firstFace + numFaces; i++)
				{
					face = map.facesLump[i];
					ti = map.texinfosLump[face.texinfo];
					if ((Test.Inst.skipSky && (ti.flags & SourceBSPStructs.SURF_SKY) != 0) || face.dispinfo != -1)
						continue;

					if (BuildFace (i, ref verts, ref UVs, ref UV2s, ref tris, vertOffs, indsOffs))
					{
						vertOffs += face.numedges;
						indsOffs += (face.numedges - 2) * 3;
					}
					else
					{
						/*	GameObject subGO = new GameObject("lm"+curLightmap);
							subGO.transform.SetParent(model.transform);
							subGO.isStatic = true;
							MeshRenderer subMR = subGO.AddComponent<MeshRenderer>();
							subMR.material = Test.Inst.testMaterial;
							MeshFilter subMF = model.AddComponent<MeshFilter>();
							subMF.sharedMesh = new Mesh();
							subMF.sharedMesh.name = "lm"+curLightmap;
							subMF.sharedMesh.vertices = verts;
							subMF.sharedMesh.triangles = tris;
							subMF.sharedMesh.uv = UVs;
							subMF.sharedMesh.uv2 = UV2s;
							subMR.lightmapIndex = curLightmap;
							subMF.sharedMesh.RecalculateNormals ();

							LM_UploadBlock();
							LM_InitBlock();*/
					}
				}

				MeshRenderer mr = model.AddComponent<MeshRenderer> ();
				mr.material = Test.Inst.testMaterial;

				MeshFilter mf = model.AddComponent<MeshFilter> ();
				mf.sharedMesh = new Mesh ();
				mf.sharedMesh.name = "BSPModel_" + index;
				mf.sharedMesh.vertices = verts;
				mf.sharedMesh.triangles = tris;
				mf.sharedMesh.uv = UVs;
				if (uSrcSettings.Inst.lightmaps && map.hasLightmaps && curLightmap < 255)
				{
					mf.sharedMesh.uv2 = UV2s;
					mr.lightmapIndex = curLightmap;
				}

				mf.sharedMesh.RecalculateNormals ();

				if (uSrcSettings.Inst.genColliders)
				{
					model.AddComponent<MeshCollider> ();
				}

				if (!uSrcSettings.Inst.showTriggers)
				{
					face = map.facesLump[firstFace];
					ti = map.texinfosLump[face.texinfo];
					string materialName = ConvertUtils.GetNullTerminatedString (map.texdataStringDataLump, map.texdataStringTableLump[map.texdataLump[ti.texdata].nameStringTableID]);
					materialName = materialName.ToLower ();
					if ((materialName.Contains ("trigger") || materialName.Contains ("fogvolume")) && !uSrcSettings.Inst.showTriggers)
					{
						model.SetActive (false);
					}
				}
			}
			else
			{
				bsptexdata texdata;
				List<int>[] materialFaces = null;

				materialFaces = new List<int>[map.texdataLump.Length];
				for (i = firstFace; i < firstFace + numFaces; i++)
				{
					face = map.facesLump[i];
					ti = map.texinfosLump[face.texinfo];
					texdata = map.texdataLump[ti.texdata];
					if (Test.Inst.skipSky && (ti.flags & SourceBSPStructs.SURF_SKY) != 0 || face.dispinfo != -1)
						continue;

					if (materialFaces[ti.texdata] == null)
					{
						materialFaces[ti.texdata] = new List<int> ();
					}
					materialFaces[ti.texdata].Add (i);
					texdata.numVerts += face.numedges;
					texdata.numInds += (face.numedges - 2) * 3;
				}

				for (i = 0; i < map.texdataLump.Length; i++)
				{
					if (materialFaces[i] == null)
					{
						continue;
					}
					texdata = map.texdataLump[i];

					Vector3[] verts = new Vector3[texdata.numVerts];
					Vector2[] UVs = new Vector2[texdata.numVerts];
					Vector2[] UV2s = new Vector2[texdata.numVerts];
					int[] tris = new int[texdata.numInds];

					int vertOffs = 0;
					int indsOffs = 0;
					for (int j = 0; j < materialFaces[i].Count (); j++)
					{
						face = map.facesLump[materialFaces[i][j]];

						if (!BuildFace (materialFaces[i][j], ref verts, ref UVs, ref UV2s, ref tris, vertOffs, indsOffs))
						{
							LM_UploadBlock ();
							LM_InitBlock ();
							BuildFace (materialFaces[i][j], ref verts, ref UVs, ref UV2s, ref tris, vertOffs, indsOffs);
						}
						vertOffs += face.numedges;
						indsOffs += (face.numedges - 2) * 3;
					}

					string materialName = ConvertUtils.GetNullTerminatedString (map.texdataStringDataLump, map.texdataStringTableLump[texdata.nameStringTableID]);
					materialName = materialName.ToLowerInvariant ();
					
					//Leaked maps embedded texture paths are used twice in beta maps, tested on trainingroom.bsp
                       			 //-Jhrino
					 
					if(map.header.version < 19 && materialName.Contains(Test.Inst.mapName)) // only check if its actually embedded, forgot about this one
					{
				          //Some maps do not even directly reference the proper level name for embedded materials
                       			 //Tested on c17_01_13 with Leaknet + Megapatch
                        	           if (materialName.Split('/')[1] != Test.Inst.mapName)
					   {
					      materialName = materialName.Replace(materialName.Split('/')[1], Test.Inst.mapName);
					      materialName = materialName.Replace("maps/" + Test.Inst.mapName + "/", "");
					   }
					   //does the map repeat the embedded texture path TWICE
					   if(materialName.Contains("maps/" + Test.Inst.mapName + "/maps/" + Test.Inst.mapName + "/"))
					   {
					      materialName = materialName.Replace("maps/" + Test.Inst.mapName + "/maps/" + Test.Inst.mapName + "/", "");
					      materialName = materialName.Split('_')[0];
					   }
					   else // if not, its probably once?
					   {
					      if(materialName.Contains("maps/" + Test.Inst.mapName + "/"))
					      {
					        //Dont split anything and load the material with replace
						materialName = materialName.Replace("maps/" + Test.Inst.mapName + "/", "");
					      }
					   }
					}

					
					GameObject texObj = new GameObject (materialName);
					texObj.transform.SetParent (model.transform);
					texObj.isStatic = true;
					if (uSrcSettings.Inst.lightmaps && map.hasLightmaps)
						texObj.layer = 8;

					MeshRenderer mr = texObj.AddComponent<MeshRenderer> ();
					MeshFilter mf = texObj.AddComponent<MeshFilter> ();
					Mesh mesh = new Mesh ();
					mesh.name = materialName;
					mesh.vertices = verts;
					mesh.uv = UVs;
					mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; //Making model shadows two-sided for better quality.

					if (uSrcSettings.Inst.lightmaps && map.hasLightmaps && curLightmap < 255)
						mesh.uv2 = UV2s;

					mesh.triangles = tris;
					mesh.RecalculateNormals ();
					mf.sharedMesh = mesh;

					Material mat = ResourceManager.Inst.GetMaterial (materialName);
					mr.material = mat;
					

					if (uSrcSettings.Inst.lightmaps && map.hasLightmaps && curLightmap < 255)
					{
						mr.lightmapIndex = curLightmap;
					}
					if ((materialName.Contains ("trigger") || materialName.Contains ("fogvolume")) && !uSrcSettings.Inst.showTriggers)
					{
						texObj.SetActive (false);
					}

					if (uSrcSettings.Inst.genColliders)
					{
						texObj.AddComponent<MeshCollider> ();
					}
				}

				for (int t = 0; t < map.texdataLump.Length; t++)
				{
					texdata = map.texdataLump[t];

					texdata.numVerts = 0;
					texdata.numInds = 0;
				}
			}
		}

		GameObject GenerateFaceObject (int index)
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

			GameObject faceObject = new GameObject ("Face: " + index);

			MeshRenderer mr = faceObject.AddComponent<MeshRenderer> ();
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; //Making model shadows two-sided for better quality.

			//Material mat;
			MeshFilter mf = faceObject.AddComponent<MeshFilter> ();
			mf.sharedMesh = new Mesh ();
			mf.sharedMesh.name = "BSPFace " + index;
			mf.sharedMesh.vertices = f.points;
			mf.sharedMesh.triangles = f.triangles;
			mf.sharedMesh.uv = f.uv;

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
				if (map.texdataStringDataLump.Length > map.texdataStringTableLump[curTexData.nameStringTableID] + 92)
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump[curTexData.nameStringTableID], 92);
				else
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump[curTexData.nameStringTableID], map.texdataStringDataLump.Length - map.texdataStringTableLump[curTexData.nameStringTableID]);

				materialName = materialName.ToLowerInvariant ();

				if (materialName.Contains ("\0"))
					materialName = materialName.Remove (materialName.IndexOf ('\0'));

				//return VMTLoader.GetMaterial (materialName);

				Material tempmat = ResourceManager.Inst.GetMaterial (materialName);
				//=================Material End========================

				//if (tempmat.name.Contains ("trigger")||tempmat.name.Contains ("fogvolume")||tempmat.name.Contains ("tools/toolsskybox")) 
				//	mr.enabled = false;

				mr.material = tempmat;

				if (uSrcSettings.Inst.lightmaps && map.hasLightmaps) // & !vmtFile.translucent)
				{

					//Texture2D lightMap = CreateLightmapTex (f);

					mf.sharedMesh.uv2 = f.uv2;
					mr.lightmapIndex = 0;
				}
			}
			else
			{
				mr.material = Test.Inst.testMaterial;
			}

			mf.sharedMesh.RecalculateNormals ();
			//mf.mesh.RecalculateBounds ();

			faceObject.transform.parent = modelsObject.transform;

			var o_778_3_636690536766675730 = mf.sharedMesh;
			faceObject.isStatic = true;

			return faceObject;
		}

		void GenerateDispFaceObject (int index, int model)
		{
			surface f = BuildDispFace (index, model, map.facesLump[index].dispinfo);

			GameObject faceObject = new GameObject ("DispFace: " + f.index);

			MeshRenderer mr = faceObject.AddComponent<MeshRenderer> ();
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; //Making model shadows two-sided for better quality.

			//Material mat;
			MeshFilter mf = faceObject.AddComponent<MeshFilter> ();
			mf.sharedMesh = new Mesh ();
			mf.sharedMesh.name = "DispFace " + f.index;
			mf.sharedMesh.vertices = f.points;
			mf.sharedMesh.triangles = f.triangles;
			mf.sharedMesh.uv = f.uv;
			mf.sharedMesh.colors32 = f.cols;

			if (uSrcSettings.Inst.textures)
			{
				bsptexdata curTexData = map.texdataLump[map.texinfosLump[map.facesLump[index].texinfo].texdata];

				//===========================Material=============================
				string materialName = "";
				//VMTLoader.VMTFile vmtFile=null;
				//Material tempmat=null;

				//string
				if (map.texdataStringDataLump.Length > map.texdataStringTableLump[curTexData.nameStringTableID] + 92)
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump[curTexData.nameStringTableID], 92);
				else
					materialName = new string (map.texdataStringDataLump, map.texdataStringTableLump[curTexData.nameStringTableID], map.texdataStringDataLump.Length - map.texdataStringTableLump[curTexData.nameStringTableID]);

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

			mf.sharedMesh.RecalculateNormals ();

			faceObject.transform.parent = dispObject.transform;
			var o_835_3_636690536766835854 = mf.sharedMesh;
			faceObject.isStatic = true;

			if (uSrcSettings.Inst.genColliders)
			{
				faceObject.AddComponent<MeshCollider> ();
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

		surface BuildFace (int index)
		{
			return new surface (); //null
		}

		bool BuildFace (int index, ref Vector3[] verts, ref Vector2[] UVs, ref Vector2[] UV2s, ref int[] tris, int vertOffs, int triOffs)
		{
			bspface curface = map.facesLump[index];
			int startEdge = curface.firstedge;
			int nEdges = curface.numedges;
			bsptexinfo texInfo = map.texinfosLump[curface.texinfo];
			//Debug.Log("texinfo "+curface.texinfo+"/"+map.texinfosLump.Length);
			int tiFlags = texInfo.flags;

			int lightmapW = curface.LightmapTextureSizeInLuxels[0] + 1;
			int lightmapH = curface.LightmapTextureSizeInLuxels[1] + 1;

			int lmx = 0, lmy = 0;
			if (uSrcSettings.Inst.lightmaps && (tiFlags & SourceBSPStructs.SURF_NOLIGHT) == 0)
				if (!LM_AllocBlock (lightmapW, lightmapH, out lmx, out lmy))
				{
					Debug.LogWarning ("LM_AllocBlock failed on face " + index);
					return false;
				}

			for (int i = 0; i < nEdges; i++)
			{
				//verts.Add( map.surfedgesLump[i]>0 ? 
				verts[vertOffs + i] = (map.vertexesLump[map.edgesLump[Math.Abs (map.surfedgesLump[startEdge + i])][map.surfedgesLump[startEdge + i] > 0 ? 0 : 1]]);
			}
			int j = triOffs;
			for (int i = 0; i < nEdges - 2; i++)
			{
				tris[j] = vertOffs;
				tris[j + 1] = vertOffs + i + 1;
				tris[j + 2] = vertOffs + i + 2;
				j += 3;
			}

			float scales = map.texdataLump[texInfo.texdata].width * uSrcSettings.Inst.worldScale;
			float scalet = map.texdataLump[texInfo.texdata].height * uSrcSettings.Inst.worldScale;

			for (int i = 0; i < nEdges; i++)
			{
				float tU = Vector3.Dot (verts[vertOffs + i] * uSrcSettings.Inst.worldScale, texInfo.texvecs) + (texInfo.texoffs * uSrcSettings.Inst.worldScale);
				float tV = Vector3.Dot (verts[vertOffs + i] * uSrcSettings.Inst.worldScale, texInfo.texvect) + (texInfo.texofft * uSrcSettings.Inst.worldScale);
				UVs[vertOffs + i] = new Vector2 (tU / scales, tV / scalet);
			}

			for (int i = 0; i < nEdges; i++)
			{
				float U, V;
				if ((tiFlags & SourceBSPStructs.SURF_NOLIGHT) == 0)
				{
					U = Vector3.Dot (verts[vertOffs + i], texInfo.lightvecs) + texInfo.lightoffs + 0.5f - curface.LightmapTextureMinsInLuxels[0] + lmx;
					V = Vector3.Dot (verts[vertOffs + i], texInfo.lightvect) + texInfo.lightofft + 0.5f - curface.LightmapTextureMinsInLuxels[1] + lmy;
				}
				else
				{
					U = BLOCK_SIZE - 2;
					V = BLOCK_SIZE - 2;
				}

				UV2s[vertOffs + i] = new Vector2 (U / BLOCK_SIZE, V / BLOCK_SIZE);
			}

			for (int i = 0; i < nEdges; i++)
			{
				verts[vertOffs + i] *= uSrcSettings.Inst.worldScale;
			}

			if (uSrcSettings.Inst.lightmaps && (tiFlags & SourceBSPStructs.SURF_NOLIGHT) == 0)
				CreateLightmapTex (lightmapW, lightmapH, lmx, lmy, index);

			return true;
		}

		surface BuildDispFace (int faceIndex, int model, short dispinfoId)
		{
			Vector3[] vertices = new Vector3[4];
			List<Vector3> disp_verts = new List<Vector3> ();
			List<Vector2> UVs = new List<Vector2> ();
			List<Color32> cols = new List<Color32> ();
			List<int> indices = new List<int> ();

			bspface curFace = map.facesLump[faceIndex];

			bsptexinfo curTexInfo = map.texinfosLump[curFace.texinfo];
			bsptexdata curTexData = map.texdataLump[curTexInfo.texdata];

			int fEdge = curFace.firstedge;

			for (int i = 0; i < curFace.numedges; i++)
			{
				vertices[i] = (map.surfedgesLump[fEdge + i] > 0 ?
					map.vertexesLump[map.edgesLump[Mathf.Abs (map.surfedgesLump[fEdge + i])][0]] :
					map.vertexesLump[map.edgesLump[Mathf.Abs (map.surfedgesLump[fEdge + i])][1]]);
			}

			bspdispinfo curDisp = map.dispinfoLump[dispinfoId];
			Vector3 startPos = curDisp.startPosition;

			float dist;
			float minDist = 0.1f;
			int minIndex = 0;

			for (int i = 0; i < 4; i++)
			{
				dist = Vector3.Distance (startPos, vertices[i]);

				if (dist < minDist)
				{
					minDist = dist;
					minIndex = i;
				}
			}

			Vector3 temp;

			for (int i = 0; i < minIndex; i++)
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

			float subdivideScale = 1.0f / (float) (numEdgeVertices - 1);

			Vector3 leftEdgeStep = leftEdge * subdivideScale;
			Vector3 rightEdgeStep = rightEdge * subdivideScale;

			int firstVertex = 0;

			Vector3 leftEnd;
			Vector3 rightEnd;
			Vector3 leftRightSeg;
			Vector3 leftRightStep;

			int dispVertIndex;
			bspDispVert dispVert;

			Vector3 flatVertex;
			Vector3 dispVertex;

			float scaleU = (float) 1f / curTexData.width;
			float scaleV = (float) 1f / curTexData.height;

			for (int i = 0; i < numEdgeVertices; i++)
			{
				leftEnd = leftEdgeStep * (float) i;
				leftEnd += vertices[0];
				rightEnd = rightEdgeStep * (float) i;
				rightEnd += vertices[3];

				leftRightSeg = rightEnd - leftEnd;
				leftRightStep = leftRightSeg * subdivideScale;

				for (int j = 0; j < numEdgeVertices; j++)
				{
					dispVertIndex = curDisp.DispVertStart;
					dispVertIndex += i * numEdgeVertices + j;
					dispVert = map.dispVertsLump[dispVertIndex];

					flatVertex = leftEnd + (leftRightStep * (float) j);

					dispVertex = dispVert.vec * (dispVert.dist /* *scale*/ );
					dispVertex += flatVertex;

					disp_verts.Add (dispVertex);

					float tU = Vector3.Dot (flatVertex, curTexInfo.texvecs) + (curTexInfo.texoffs);
					float tV = Vector3.Dot (flatVertex, curTexInfo.texvect) + (curTexInfo.texofft);
					UVs.Add (new Vector2 (tU * scaleU, tV * scaleV));

					cols.Add (new Color32 ((byte) (dispVert.alpha), 0, 0, 0));
				}
			}

			int curIndex;

			for (int i = 0; i < numEdgeVertices - 1; i++)
			{
				for (int j = 0; j < numEdgeVertices - 1; j++)
				{
					curIndex = i * numEdgeVertices + j;

					if ((curIndex % 2) == 1)
					{
						curIndex += firstVertex;

						indices.Add (curIndex + 1);
						indices.Add (curIndex);
						indices.Add (curIndex + numEdgeVertices);
						indices.Add (curIndex + numEdgeVertices + 1);
						indices.Add (curIndex + 1);
						indices.Add (curIndex + numEdgeVertices);
					}
					else
					{
						curIndex += firstVertex;

						indices.Add (curIndex);
						indices.Add (curIndex + numEdgeVertices);
						indices.Add (curIndex + numEdgeVertices + 1);
						indices.Add (curIndex + 1);
						indices.Add (curIndex);
						indices.Add (curIndex + numEdgeVertices + 1);
					}
				}
			}

			for (int i = 0; i < disp_verts.Count; i++)
			{
				disp_verts[i] *= uSrcSettings.Inst.worldScale;
			}

			surface f = new surface ();
			f.index = faceIndex;
			//f.flags = flags;

			f.dispinfo = dispinfoId;

			f.points = disp_verts.ToArray ();
			f.uv = UVs.ToArray ();
			//f.uv2 = UV2s;
			f.cols = cols.ToArray ();
			f.triangles = indices.ToArray ();

			//f.lightMapW = lightmapW;
			//f.lightMapH = lightmapH;

			return f;
		}

		Texture2D CreateLightmapTex (surface f)
		{
			return null;
		}

		void CreateLightmapTex (int w, int h, int lx, int ly, int i)
		{
			if (map.facesLump[i].lightofs == -1 || map.lightingLump.Length == 0)
			{
				Debug.LogWarning ("Face " + i + " haven't lightmap. Flags " + map.texinfosLump[map.facesLump[i].texinfo].flags);
				return;
			}
			//Texture2D tex = new Texture2D (f.lightMapW, f.lightMapH, TextureFormat.RGB24, false,true);
			//tex.filterMode = FilterMode.Point;
			//Color32[] colors = new Color32[w * h];
			Color[] colors = new Color[w * h];

			int Offset = map.facesLump[i].lightofs / 4;

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					Color32 col = map.lightingLump[Offset + w * y + x];
					colors[w * y + x] = new Color (col.r / 255.0f, col.g / 255.0f, col.b / 255.0f, col.a / 255.0f);
				}
			}

			//tex.SetPixels32 (colors);
			//tex.Apply ();
			//return tex;

			curLightmapTex.SetPixels (lx, ly, w, h, colors);
		}

		void LM_InitBlock ()
		{
			if (lm_allocated == null)
				return;
			for (int i = 0; i < BLOCK_SIZE; i++)
			{
				lm_allocated[i] = 0;
			}
			curLightmapTex = new Texture2D (BLOCK_SIZE, BLOCK_SIZE, TextureFormat.RGB24, false, true);
		}

		bool LM_AllocBlock (int w, int h, out int x, out int y)
		{
			int i, j, best2;
			int best = BLOCK_SIZE;

			x = y = 0;

			for (i = 0; i < BLOCK_SIZE - w; i++)
			{
				best2 = 0;

				for (j = 0; j < w; j++)
				{
					if (lm_allocated[i + j] >= best)
						break;
					if (lm_allocated[i + j] > best2)
						best2 = lm_allocated[i + j];
				}

				if (j == w)
				{
					x = i;
					y = best = best2;
				}
			}

			if (best + h > BLOCK_SIZE)
				return false;

			for (i = 0; i < w; i++)
				lm_allocated[x + i] = best + h;

			return true;
		}

		void LM_UploadBlock ()
		{
			Debug.Log ("Upload lightmap block " + curLightmap);
			curLightmapTex.Apply ();
			lightmapsData.Add (new LightmapData () { lightmapColor = curLightmapTex });
			curLightmapTex = null;
			curLightmap++;
		}

        void UnsetResources()
        {
            map = null;

            mapObject = null;
            modelsObject = null;
            propsObject = null;
            dispObject = null;
            entObject = null;

            models = null;
            Props = null;
            entities = null;

            var test = Test.Inst;
            if (test != null) test.bsp = null;
        }
	}
}
