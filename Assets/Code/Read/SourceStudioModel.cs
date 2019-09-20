using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace uSrcTools
{

    [System.Serializable]
    public class SourceStudioModel// : MonoBehaviour
    {


        public string logname; // model logging for uSrcSettings.Inst.extraDebugging
        public string curModelName;

        public studiohdr_t mdlHeader;
        public vvdheader vvdHeader;
        public vtxHeader vtxHeader;

        public int lodLevel = 0;
        int skin;
        int numSkinRefs;
        public int submeshCount = 0;
        public Material tempMat;

        public int vertCount;
        public Vector3[] vertArray;
        public Vector3[] Normals;
        public Vector2[] UV;
        public BoneWeight[] boneWeights;
        public int[] indexArray;
        public Dictionary<int, int[]> trisArrays;

        public studiobone_t[] mdlBones;

        public studiobodypart[] mdlBodyParts;

        public vtxBodypartHeader[] vtxBodyParts;

        public studiotexture_t[] mdlTextures;
        public string[] mdlTexturePaths;
        public short[] mdlSkinFamilies;

        public Transform[] Bones;
        Mesh modelMesh;
        bool used;
        public bool loaded = false;
        public bool drawArmature = false;
        Material[] TempMats;

        private enum SourceVTXType
        {
            NONE,
            SW_VTX,
            DX7,
            DX8,
            DX9
        };

        SourceVTXType VTXType;

        public bool hasVVD = false;


        public void LogModelData(string[] data)
        {
            logname = Application.dataPath + "/" + "log_model.txt";
            if (!File.Exists(Application.dataPath + "/" + "log_model.txt"))
                File.Create(Application.dataPath + "/" + "log_model.txt");

            string text = Environment.NewLine + "MDL Name: " + data[2] + "\nMDL Version: " + data[0] + Environment.NewLine + "MDL VTX: " + VTXType + Environment.NewLine + "MDL Length: " + data[3] + Environment.NewLine + "MDL Checksum: " + data[1] + Environment.NewLine + "MDL Minimum Hull:" + data[4] + Environment.NewLine + "MDL Maximum Hull: " + data[5] + Environment.NewLine + "MDL Bone Count: " + data[6] + Environment.NewLine + "MDL Texture Count: " + data[7];
            File.AppendAllText(logname, Environment.NewLine + text);
        }


        public SourceStudioModel Load(string ModelName)
        {

            if (uSrcSettings.Inst.extraDebugging)
                UnityEngine.Debug.unityLogger.logEnabled = true;

            curModelName = ModelName;

            if (ResourceManager.GetPath(curModelName) == null)
                return null;
            if (ResourceManager.GetPath(curModelName.Replace(".mdl", ".vvd")) != null)
                hasVVD = true;
            if (ResourceManager.GetPath(curModelName.Replace(".mdl", ".dx90.vtx")) == null)
            {
                if (ResourceManager.GetPath(curModelName.Replace(".mdl", ".dx80.vtx")) != null)
                    VTXType = SourceVTXType.DX8;
                else
                {
                    if (ResourceManager.GetPath(curModelName.Replace(".mdl", ".dx7_2bone.vtx")) == null)
                    {
                        VTXType = SourceVTXType.DX7;
                    }
                    else
                    {
                        if (ResourceManager.GetPath(curModelName.Replace(".mdl", ".sw.vtx")) == null)
                            VTXType = SourceVTXType.SW_VTX;
                        else
                        {
                            Debug.LogError("Model has no VTX data!");
                            VTXType = SourceVTXType.NONE;
                        }
                        return null;
                    }
                }
            }
            else
            {
                VTXType = SourceVTXType.DX9;
            }

            //studiobodypart[] mdlBodyParts;
            if (!ParseMdl(curModelName))
            {
                return null;
            }


            LoadSkin(0);

            //public int vertCount;
            //Vector3[] vertArray;
            //Vector3[] Normals;
            //Vector2[] UV;
            //int[] indexArray;

            if (hasVVD)
                ParseVvd(curModelName);
            if (VTXType != SourceVTXType.NONE)
                ParseVtx(curModelName);

            if (uSrcSettings.Inst.extraDebugging)
            {
                string[] data = new string[10];

                data[0] = mdlHeader.version.ToString();
                data[1] = mdlHeader.checksum.ToString();
                data[2] = mdlHeader.Name;
                data[3] = mdlHeader.length.ToString();
                data[4] = mdlHeader.hullmin.ToString();
                data[5] = mdlHeader.hullmax.ToString();
                data[6] = mdlHeader.bonesnum.ToString();
                data[7] = mdlHeader.texturenum.ToString();

                LogModelData(data);
            }

            loaded = true;

            return this;
        }
        /*
                public void GetInstance(GameObject go, bool skinned)
                {
                    BuildMeshObject (curModelName, go,skinned);
                }

                public void GetInstance(GameObject go)
                {
                    BuildMeshObject (curModelName, go,false);
                }*/

        public void GetInstance(GameObject go, bool skinned, int bp)
        {
            BuildMeshObject(curModelName, go, skinned, bp);
        }

        public void OnDrawGizmos()
        {
            /*for (int i=0; i<vertArray.Length; i++) 
			{
				Gizmos.DrawCube(vertArray[i],Vector3.one*0.2f);
			}*/
            if (loaded & used & drawArmature)
            {
                for (int i = 0; i < Bones.Length; i++)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(Bones[i].position, Vector3.one * 0.02f);
                    if (mdlBones[i].parent != -1)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(Bones[i].position, Bones[mdlBones[i].parent].position);
                    }
                }
            }
        }

        void BuildMeshObjectOLD(string modelName, GameObject go, bool skinned)
        {
            if (!loaded)
            {
                Debug.LogWarning("Can't get Instance of model " + modelName + " because it's not loaded");
                return;
            }

            if (used)
            {
                Bones = new Transform[mdlHeader.bonesnum];
                for (int i = 0; i < mdlHeader.bonesnum; i++)
                {
                    GameObject bone = new GameObject(mdlBones[i].pszName);
                    if (mdlBones[i].parent >= 0)
                    {
                        bone.transform.SetParent(Bones[mdlBones[i].parent]);
                    }
                    else
                    {
                        bone.transform.SetParent(go.transform);
                    }
                    Vector3 temp = Vector3.zero;
                    //if(i==0)
                    temp = new Vector3(mdlBones[i].pos.x, mdlBones[i].pos.z, mdlBones[i].pos.y);
                    //else
                    //	temp=new Vector3(mdlBones[i].pos.x,mdlBones[i].pos.y,-mdlBones[i].pos.z);

                    bone.transform.localPosition = temp * uSrcSettings.Inst.worldScale;
                    Vector3 rot = new Vector3(-mdlBones[i].rot.x * Mathf.Rad2Deg, -mdlBones[i].rot.z * Mathf.Rad2Deg, -mdlBones[i].rot.y * Mathf.Rad2Deg);
                    bone.transform.localEulerAngles = rot;
                    Bones[i] = bone.transform;
                }

                if (skinned)
                {
                    go.AddComponent<MeshFilter>().mesh = modelMesh;
                    SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
                    smr.material = tempMat;
                    smr.sharedMesh = modelMesh;
                    smr.bones = Bones;
                    smr.rootBone = Bones[0];
                    smr.updateWhenOffscreen = true;

                }
                else
                {
                    go.AddComponent<MeshFilter>().mesh = modelMesh;
                    go.AddComponent<MeshRenderer>().material = tempMat;
                }
                return;
            }

            //==========================================================
            Bones = new Transform[mdlHeader.bonesnum];
            for (int i = 0; i < mdlHeader.bonesnum; i++)
            {
                GameObject bone = new GameObject(mdlBones[i].pszName);
                if (mdlBones[i].parent >= 0)
                {
                    bone.transform.SetParent(Bones[mdlBones[i].parent]);
                }
                else
                {
                    bone.transform.SetParent(go.transform);
                }
                Vector3 temp = Vector3.zero;
                //if(i==0)
                temp = new Vector3(mdlBones[i].pos.x, mdlBones[i].pos.z, mdlBones[i].pos.y);
                //else
                //	temp=new Vector3(mdlBones[i].pos.x,mdlBones[i].pos.y,-mdlBones[i].pos.z);

                bone.transform.localPosition = temp * uSrcSettings.Inst.worldScale;
                Vector3 rot = new Vector3(-mdlBones[i].rot.x * Mathf.Rad2Deg, -mdlBones[i].rot.z * Mathf.Rad2Deg, -mdlBones[i].rot.y * Mathf.Rad2Deg);
                bone.transform.localEulerAngles = rot;
                Bones[i] = bone.transform;
            }
            //============================================================================================

            string materialName = "";

            if (mdlTexturePaths.Length == 1)
            {
                materialName += mdlTexturePaths[0];
                materialName += mdlTextures[mdlBodyParts[0].models[0].meshes[0].material].textureName;
            }
            else
            {
                string path = ResourceManager.GetPath("materials/" + mdlTexturePaths[0]
                       + mdlTextures[mdlBodyParts[0].models[0].meshes[0].material].textureName + ".vmt");

                if (path == null)
                {
                    Debug.Log("Try second path");
                    path = ResourceManager.GetPath("materials/" + mdlTexturePaths[1]
                       + mdlTextures[mdlBodyParts[0].models[0].meshes[0].material].textureName + ".vmt");
                    if (path != null)
                    {
                        materialName = mdlTexturePaths[1]
                        + mdlTextures[mdlBodyParts[0].models[0].meshes[0].material].textureName;
                    }
                }
            }

            if (uSrcSettings.Inst.textures)
                tempMat = ResourceManager.Inst.GetMaterial(materialName);
            else
                tempMat = Test.Inst.testMaterial;

            //============================================================================================

            //GameObject testMesh = new GameObject (curModelName);
            //testMesh.transform.position = position;
            modelMesh = new Mesh();
            modelMesh.name = curModelName;

            modelMesh.vertices = vertArray;
            modelMesh.normals = Normals;
            modelMesh.uv = UV;
            modelMesh.boneWeights = boneWeights;
            //======================================================
            Matrix4x4[] bindPoses = new Matrix4x4[Bones.Length];
            for (int i = 0; i < bindPoses.Length; i++)
            {
                //Bones[i].localPosition = Vector3.zero;
                bindPoses[i] = Bones[i].worldToLocalMatrix * go.transform.localToWorldMatrix;
            }
            modelMesh.bindposes = bindPoses;

            //====================================================================
            //Debug.Log (indexArray.Length+" indices");

            modelMesh.triangles = indexArray;
            /*
			int numMeshes = vtxBodyParts [0].models [0].lods [lodLevel].numMeshes;
			modelMesh.subMeshCount = numMeshes;
			int indexCount = 0;
			int indexOffset = 0;
			for(int i=0; i<numMeshes; i++)
			{
				indexOffset=indexCount;//vtxBodyParts [0].models [0].lods [lodLevel].meshes[i].stripGroups[0].strips[0].indexOffset;
				indexCount = vtxBodyParts [0].models [0].lods [lodLevel].meshes[i].stripGroups[0].strips[0].numIndices;
				int[] tempIndex = new int[indexCount];
				System.Buffer.BlockCopy(indexArray,indexOffset*4,tempIndex,0,indexCount*4);
				Debug.Log("Mesh "+i+" index count is "+indexCount+" index offset "+indexOffset);
				modelMesh.SetTriangles(tempIndex, i);
			}
			*/
            //modelMesh.subMeshCount = 1;
            /*int indexCount = indexArray.Length;

			int[] tempIndex = new int[indexCount];
			System.Buffer.BlockCopy(indexArray,0,tempIndex,0,indexCount*4);
			//modelMesh.SetTriangles(tempIndex, 0);
			modelMesh.triangles = tempIndex;*/
            //===================================================================

            modelMesh.RecalculateBounds();
            //modelMesh.Optimize ();

            if (skinned)
            {
                go.AddComponent<MeshFilter>().mesh = modelMesh;
                SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.material = tempMat;
                smr.sharedMesh = modelMesh;
                smr.bones = Bones;
                smr.rootBone = Bones[0];
                smr.updateWhenOffscreen = true;
            }
            else
            {
                go.AddComponent<MeshFilter>().mesh = modelMesh;
                go.AddComponent<MeshRenderer>().material = tempMat;
            }

            for (int i = 0; i < mdlHeader.bonesnum; i++)
            {
                Vector3 temp = Vector3.zero;
                //if(i==0)
                temp = new Vector3(mdlBones[i].pos.x, mdlBones[i].pos.z, mdlBones[i].pos.y);
                //else
                //	temp=new Vector3(mdlBones[i].pos.x,mdlBones[i].pos.y,-mdlBones[i].pos.z);
                Bones[i].transform.localPosition = temp * uSrcSettings.Inst.worldScale;
                Vector3 rot = new Vector3(-mdlBones[i].rot.x * Mathf.Rad2Deg, -mdlBones[i].rot.z * Mathf.Rad2Deg, -mdlBones[i].rot.y * Mathf.Rad2Deg);
                Bones[i].transform.localEulerAngles = rot;
            }

            //tempMat = new Material (Shader.Find("Diffuse"));

            /*for (int i=0; i<mdlBodyParts.Length; i++) 
			{
				GameObject bodyPartObj=new GameObject(mdlBodyParts[i].name);
				bodyPartObj.transform.SetParent(go.transform);

				for (int m=0; m<mdlBodyParts[i].models.Length; m++) 
				{
					GameObject modelObj=new GameObject(mdlBodyParts[i].models[m].name);
					modelObj.transform.SetParent(bodyPartObj.transform);

					for (int meshId=0; meshId<mdlBodyParts[i].models[m].meshnum; meshId++) 
					{
						GameObject meshObj=new GameObject("Mesh "+meshId);
						meshObj.transform.SetParent(modelObj.transform);

						//Mesh mesh = new Mesh ();
						//mesh.name = mdlBodyParts[i].models[m].name+" Mesh "+meshId;

						//mesh.vertices = vertArray;
						//mesh.normals = Normals;
						//mesh.uv = UV;
						//Debug.Log (indexArray.Length+" indices");
						//mesh.triangles = indexArray;
						
						//meshObj.AddComponent<MeshFilter> ().mesh = mesh;
						//meshObj.AddComponent<MeshRenderer> ().material = tempMat;
					}
				}
			}*/

            used = true;
        }

        void BuildMeshObject(string modelName, GameObject go, bool skinned, int bp)
        {
            if (!loaded)
            {
                Debug.LogWarning("Can't get Instance of model " + modelName + " because it's not loaded");
                return;
            }

            if (skinned)
            {
                Bones = new Transform[mdlHeader.bonesnum];
                for (int i = 0; i < mdlHeader.bonesnum; i++)
                {
                    GameObject bone = new GameObject(mdlBones[i].pszName);
                    if (mdlBones[i].parent >= 0)
                    {
                        bone.transform.SetParent(Bones[mdlBones[i].parent]);
                    }
                    else
                    {
                        bone.transform.SetParent(go.transform);
                    }
                    Vector3 temp = Vector3.zero;
                    //if(i==0)
                    temp = new Vector3(mdlBones[i].pos.x, mdlBones[i].pos.z, mdlBones[i].pos.y);
                    //else
                    //	temp=new Vector3(mdlBones[i].pos.x,mdlBones[i].pos.y,-mdlBones[i].pos.z);

                    bone.transform.localPosition = temp * uSrcSettings.Inst.worldScale;
                    Vector3 rot = new Vector3(-mdlBones[i].rot.x * Mathf.Rad2Deg, -mdlBones[i].rot.z * Mathf.Rad2Deg, -mdlBones[i].rot.y * Mathf.Rad2Deg);
                    bone.transform.localEulerAngles = rot;
                    Bones[i] = bone.transform;
                }
            }

            if (!used)
            {
                modelMesh = new Mesh();
                modelMesh.name = curModelName;

                modelMesh.vertices = vertArray;
                if (modelMesh.vertices.Length == 0)
                    Debug.LogWarning("Error with model " + modelName);

                modelMesh.normals = Normals;
                modelMesh.uv = UV;
                modelMesh.boneWeights = boneWeights;

                modelMesh.subMeshCount = submeshCount;
                //Debug.Log ("submeshCount " + submeshCount);
                //Debug.Log ("indexCount "+indexArray.Length);
                //Debug.Log ("Bodypart count "+vtxBodyParts.Length);
                int curmeshId = 0;
                int curIndexOffset = 0;


                if (TempMats == null)
                    TempMats = new Material[submeshCount];

                //for(int bpId=0; bpId<vtxBodyParts.Length; bpId++)
                //{
                int bpId = 0;

                vtxBodypartHeader vtxBodyPart = vtxBodyParts[bpId];
                studiobodypart mdlBodyPart = mdlBodyParts[bpId];
                for (int modelId = 0; modelId < vtxBodyPart.models.Length; modelId++)
                {
                    vtxModelHeader vtxModel = vtxBodyPart.models[modelId];
                    studiomodel mdlModel = mdlBodyPart.models[modelId];

                    vtxModelLODheader _lod = vtxModel.lods[lodLevel];

                    for (int meshId = 0; meshId < _lod.numMeshes; meshId++)
                    {
                        vtxMeshHeader vtxMesh = _lod.meshes[meshId];
                        studiomesh mdlMesh = mdlModel.meshes[meshId];

                        int materialId = mdlMesh.material + (numSkinRefs * skin);
                        string material = mdlTextures[mdlSkinFamilies[materialId]].textureName;
                        //Debug.Log ("Mesh "+curmeshId+" material is "+material);

                        if (TempMats[curmeshId] == null)
                        {
                            string materialName = ResourceManager.FindModelMaterialFile(material, mdlTexturePaths);

                            if (materialName != null)
                                TempMats[curmeshId] = ResourceManager.Inst.GetMaterial(materialName);
                            else
                                TempMats[curmeshId] = ResourceManager.Inst.GetMaterial(material);
                        }

                        for (int stripGroupId = 0; stripGroupId < vtxMesh.numStripGroups; stripGroupId++)
                        {
                            vtxStripGroupHeader stripGroup = vtxMesh.stripGroups[stripGroupId];

                            //for(int stripId=0; stripId<stripGroup.numStrips;stripId++)
                            //{
                            //====================
                            //vtxStripGroupHeader stripGroup=vtxBodyParts[0].models[0].lods[lodLevel].meshes[0].stripGroups[0];
                            //vtxStripHeader strip=stripGroup.strips[0];
                            //====================

                            //vtxStripHeader strip=stripGroup.strips[stripId];

                            //int indexOffset=stripGroup.indexOffs+strip.indexOffset;
                            //int numIndices=strip.numIndices;
                            int indexOffset = curIndexOffset;
                            int numIndices = stripGroup.numIndices * 4;

                            //Debug.Log("Mesh "+curmeshId+" index count is "+numIndices/4+" index offset "+indexOffset/4);

                            int[] tempIndex = new int[stripGroup.numIndices];
                            System.Buffer.BlockCopy(indexArray, indexOffset, tempIndex, 0, numIndices);
                            modelMesh.SetTriangles(tempIndex, curmeshId);
                            if (modelMesh.GetTriangles(curmeshId).Length == 0)
                                Debug.LogWarning("Error with model " + modelName + ": setting tris for " + curmeshId + " submesh");

                            curmeshId++;
                            curIndexOffset += stripGroup.numIndices * 4;
                            //}
                        }
                    }//mesh
                }//model
                 //}//bp
            }//!used

            //Vector3 rot=new Vector3(-mdlBones[i].rot.x*Mathf.Rad2Deg,-mdlBones[i].rot.z*Mathf.Rad2Deg,-mdlBones[i].rot.y*Mathf.Rad2Deg);
            Vector3 rootrot = new Vector3(0, -mdlBones[0].rot.x * Mathf.Rad2Deg, 0);

            go.transform.localEulerAngles += rootrot;

            if (go.GetComponent<MeshFilter>())
                go.GetComponent<MeshFilter>().mesh = modelMesh;
            else
                go.AddComponent<MeshFilter>().mesh = modelMesh;

            MeshRenderer mr;

            if (go.GetComponent<MeshRenderer>() == null)
                mr = go.AddComponent<MeshRenderer>();
            else
                mr = go.GetComponent<MeshRenderer>();

            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; //Making model shadows two-sided for better quality.

            modelMesh.RecalculateBounds();
            mr.materials = TempMats;
            mr.lightmapIndex = 255;

            used = true;
        }

        void LoadSkin(int skinId)
        {
            skin = skinId;

            int skinTableOffset = numSkinRefs * skinId;
            for (int i = 0; i < numSkinRefs; i++)
            {
                int texId = mdlSkinFamilies[skinTableOffset + i];
                string textureName = "";
                if (texId < mdlTexturePaths.Length)
                    textureName += mdlTexturePaths[texId];
                else
                    textureName += mdlTexturePaths[mdlTexturePaths.Length - 1];
                textureName += mdlTextures[texId].textureName;
                //LoadMaterial(textureName)
                //Debug.Log ("Need to load material "+textureName);
            }
        }

        public bool ParseMdl(string name)
        {
            string path = "";

            path = ResourceManager.GetPath(name);

            //mdlBodyParts = null;

            BinaryReader BR = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));

            //studiohdr_t Header = ReadHeader (BR);
            mdlHeader = ReadHeader(BR);

            if (mdlHeader.version < 44)
            {
                Debug.LogError("Model file is too old, are you trying to load from a 2003 leak?");
                return false;
            }

            if (new string(mdlHeader.id) != "IDST")
            {
                Debug.LogWarning("Header id is not IDST");
                BR.BaseStream.Close();
                return false;
            }


            mdlBones = ReadStudioBones(BR, mdlHeader.boneoffs, mdlHeader.bonesnum);

            mdlTextures = ReadStudioTextures(BR, mdlHeader.textureoffs, mdlHeader.texturenum);

            mdlTexturePaths = ReadTexturePaths(BR, mdlHeader.texturediroffs, mdlHeader.texturedirnum);

            numSkinRefs = mdlHeader.skinrefnum;
            mdlSkinFamilies = ReadSkinFamilies(BR, mdlHeader.skinfamilyoffs, mdlHeader.skinfamilynum, mdlHeader.skinrefnum);

            mdlBodyParts = ReadMdlBodyParts(BR, mdlHeader.bodypartoffs, mdlHeader.bodypartnum);

            //setRootLOD (mdlHeader, lodLevel);

            BR.BaseStream.Dispose();

            return true;
        }

        void setRootLOD(studiohdr_t hdr, int lod)
        {
            if (hdr.numAllowedRootLods > 0 && lod >= hdr.numAllowedRootLods)
            {
                lod = hdr.numAllowedRootLods - 1;
            }

            int vertexOffset = 0;

            for (int bpId = 0; bpId < mdlBodyParts.Length; bpId++)
            {
                studiobodypart bodypart = mdlBodyParts[bpId];
                for (int modelId = 0; modelId < bodypart.models.Length; modelId++)
                {
                    studiomodel model = bodypart.models[modelId];

                    int totalMeshVertices = 0;
                    for (int meshId = 0; meshId < model.meshnum; meshId++)
                    {
                        studiomesh mesh = model.meshes[meshId];

                        mesh.vertexnum = mesh.vertexdata.numLODVertexes[lod];
                        mesh.vertexoffs = totalMeshVertices;
                        totalMeshVertices += mesh.vertexnum;
                    }

                    model.verticesnum = totalMeshVertices;
                    model.verticesoffs = vertexOffset;
                    vertexOffset += totalMeshVertices;
                }
            }
            Debug.Log("Vertixes count " + vertexOffset);
            lodLevel = lod;
        }

        studiohdr_t ReadHeader(BinaryReader BR)
        {
            studiohdr_t hdr = new studiohdr_t();

            hdr.id = BR.ReadChars(4);
            hdr.version = BR.ReadInt32();
            hdr.checksum = BR.ReadInt32();
            hdr.Name = new string(BR.ReadChars(64));
            hdr.Name = hdr.Name.Remove(hdr.Name.IndexOf('\0'));
            //		Debug.Log("Model "+hdr.Name+" version is "+hdr.version);
            hdr.length = BR.ReadInt32();

            //header01

            hdr.eyeposition = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
            hdr.illumposition = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());

            hdr.hullmin = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
            hdr.hullmax = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());

            hdr.view_bbmin = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
            hdr.view_bbmax = new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());

            hdr.flags = BR.ReadInt32();

            hdr.bonesnum = BR.ReadInt32();
            hdr.boneoffs = BR.ReadInt32();

            hdr.bonecontrollernum = BR.ReadInt32();
            hdr.bonecontrolleroffs = BR.ReadInt32();

            hdr.hitboxnum = BR.ReadInt32();
            hdr.hitboxoffs = BR.ReadInt32();

            hdr.localanimnum = BR.ReadInt32();
            hdr.localanimoffs = BR.ReadInt32();

            hdr.localseqnum = BR.ReadInt32();
            hdr.localseqoffs = BR.ReadInt32();

            hdr.activitylistversion = BR.ReadInt32();
            hdr.eventsindexed = BR.ReadInt32();

            hdr.texturenum = BR.ReadInt32();
            hdr.textureoffs = BR.ReadInt32();

            hdr.texturedirnum = BR.ReadInt32();
            hdr.texturediroffs = BR.ReadInt32();

            hdr.skinrefnum = BR.ReadInt32();
            hdr.skinfamilynum = BR.ReadInt32();
            hdr.skinfamilyoffs = BR.ReadInt32();

            hdr.bodypartnum = BR.ReadInt32();
            hdr.bodypartoffs = BR.ReadInt32();

            hdr.attachmentnum = BR.ReadInt32();
            hdr.attachmentoffs = BR.ReadInt32();

            hdr.localnodenum = BR.ReadInt32();
            hdr.localnodeoffs = BR.ReadInt32();

            hdr.localnodenameoffs = BR.ReadInt32();

            hdr.flexdescnum = BR.ReadInt32();
            hdr.flexdescoffs = BR.ReadInt32();

            hdr.flexcontrollernum = BR.ReadInt32();
            hdr.flexcontrolleroffs = BR.ReadInt32();

            hdr.flexrulesnum = BR.ReadInt32();
            hdr.flexrulesoffs = BR.ReadInt32();

            hdr.ikchainnum = BR.ReadInt32();
            hdr.ikchainoffs = BR.ReadInt32();

            hdr.mouthsnum = BR.ReadInt32();
            hdr.mouthsoffs = BR.ReadInt32();

            hdr.localposeparamnum = BR.ReadInt32();
            hdr.localposeparamoffs = BR.ReadInt32();

            hdr.surfacepropoffs = BR.ReadInt32();

            hdr.keyvalueoffs = BR.ReadInt32();
            hdr.keyvaluenum = BR.ReadInt32();

            hdr.iklocknum = BR.ReadInt32();
            hdr.iklockoffs = BR.ReadInt32();

            hdr.mass = BR.ReadSingle();

            hdr.contents = BR.ReadInt32();

            hdr.includemodelnum = BR.ReadInt32();
            hdr.includemodeloffs = BR.ReadInt32();

            hdr.virtualModel = BR.ReadInt32();

            hdr.animblocksnameoffs = BR.ReadInt32();
            hdr.animblocksnum = BR.ReadInt32();
            hdr.animblocksoffs = BR.ReadInt32();
            hdr.animblockModel = BR.ReadInt32();

            hdr.bonetablenameoffs = BR.ReadInt32();

            hdr.vertex_base = BR.ReadInt32();
            hdr.offset_base = BR.ReadInt32();

            hdr.directionaldotproduct = BR.ReadByte();

            hdr.rootLod = BR.ReadByte();
            hdr.numAllowedRootLods = BR.ReadByte();

            BR.BaseStream.Seek(5, SeekOrigin.Current);

            hdr.flexcontrolleruinum = BR.ReadInt32();
            hdr.flexcontrolleruioffs = BR.ReadInt32();

            BR.BaseStream.Seek(8, SeekOrigin.Current);

            hdr.studiohdr2offs = BR.ReadInt32();

            BR.BaseStream.Seek(4, SeekOrigin.Current);

            return hdr;
        }

        studiobone_t[] ReadStudioBones(BinaryReader br, int offs, int num)
        {
            studiobone_t[] bones = new studiobone_t[num];

            br.BaseStream.Seek(offs, SeekOrigin.Begin);
            for (int i = 0; i < num; i++)
            {
                //BR.BaseStream.Seek (offs+(i*216), SeekOrigin.Begin);
                bones[i] = new studiobone_t();
                bones[i].sznameindex = br.ReadInt32();
                bones[i].parent = br.ReadInt32();
                bones[i].bonecontroller = new int[]{ br.ReadInt32(),br.ReadInt32(),
                    br.ReadInt32(),br.ReadInt32(),br.ReadInt32(),br.ReadInt32() };
                bones[i].pos = ConvertUtils.ReadVector3(br);
                bones[i].quat = ConvertUtils.ReadQuat(br);
                bones[i].rot = ConvertUtils.ReadVector3(br);
                bones[i].posscale = ConvertUtils.ReadVector3(br);
                bones[i].rotscale = ConvertUtils.ReadVector3(br);

                bones[i].poseToBone = new float[]{
                    br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),
                    br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),
                    br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),
                    br.ReadSingle(),br.ReadSingle(),br.ReadSingle()};

                bones[i].qAlignment = ConvertUtils.ReadQuat(br);
                bones[i].flags = br.ReadInt32();
                bones[i].proctype = br.ReadInt32();
                bones[i].procindex = br.ReadInt32();
                bones[i].physicsbone = br.ReadInt32();
                bones[i].surfacepropidx = br.ReadInt32();
                bones[i].contents = br.ReadInt32();
                br.BaseStream.Seek(32, SeekOrigin.Current);
            }

            for (int i = 0; i < num; i++)
            {
                bones[i].pszName = ConvertUtils.ReadNullTerminatedString(br, offs + (i * 216) + bones[i].sznameindex);
            }

            return bones;
        }

        studiobodypart[] ReadMdlBodyParts(BinaryReader br, int offs, int num)
        {
            studiobodypart[] bp = new studiobodypart[num];

            br.BaseStream.Seek(offs, SeekOrigin.Begin);
            //		Debug.Log ("numBodyParts: "+num);
            for (int i = 0; i < num; i++)
            {
                br.BaseStream.Seek(offs + (i * 16), SeekOrigin.Begin);
                bp[i] = new studiobodypart(br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
                //bp[i].name = ConvertUtils.ReadNullTerminatedString(BR,offs+(i*16)+bp[i].nameindex);
                bp[i].models = ReadStudioModels(br, offs + (i * 16), bp[i]);
            }

            for (int i = 0; i < num; i++)
            {
                bp[i].name = ConvertUtils.ReadNullTerminatedString(br, offs + (i * 16) + bp[i].nameindex);
            }

            return bp;
        }

        studiomodel[] ReadStudioModels(BinaryReader BR, int offset, studiobodypart bp)
        {
            BR.BaseStream.Seek(bp.modelindex + offset, SeekOrigin.Begin);
            studiomodel[] temp = new studiomodel[bp.nummodels];
            for (int i = 0; i < bp.nummodels; i++)
            {
                BR.BaseStream.Seek(bp.modelindex + offset + (i * 148), SeekOrigin.Begin);
                string tempName = new string(BR.ReadChars(64));
                if (tempName.Contains('\0'))
                    tempName = tempName.Remove(tempName.IndexOf('\0'));
                temp[i] = new studiomodel(tempName, BR.ReadInt32(), BR.ReadSingle(), BR.ReadInt32(),
                                         BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(),
                                           BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(), new mstudio_modelvertexdata_t(BR.ReadInt32(), BR.ReadInt32()));
                BR.BaseStream.Seek(32, SeekOrigin.Current);
                //length 148

                temp[i].meshes = ReadStudioMeshes(BR, bp.modelindex + offset + (i * 148), temp[i]);

            }
            return temp;
        }

        studiomesh[] ReadStudioMeshes(BinaryReader BR, int offset, studiomodel model)
        {
            studiomesh[] temp = new studiomesh[model.meshnum];

            BR.BaseStream.Seek(model.meshoffs + offset, SeekOrigin.Begin);
            for (int i = 0; i < model.meshnum; i++)
            {
                //BR.BaseStream.Seek (model.meshoffs + offset + (i*116), SeekOrigin.Begin);
                temp[i] = new studiomesh(BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(),
                                               BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(),
                                               BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32(),
                                               new Vector3(BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle()),
                                               new studio_meshvertexdata(BR.ReadInt32(),
                                               new int[]{BR.ReadInt32(),BR.ReadInt32(),BR.ReadInt32(),BR.ReadInt32(),
                                                            BR.ReadInt32(),BR.ReadInt32(),BR.ReadInt32(),BR.ReadInt32()})
                                      );
                BR.BaseStream.Seek(32, SeekOrigin.Current);
            }
            return temp;
        }

        studiotexture_t[] ReadStudioTextures(BinaryReader br, int offset, int count)
        {
            br.BaseStream.Seek(offset, SeekOrigin.Begin);

            studiotexture_t[] st = new studiotexture_t[count];

            for (int i = 0; i < count; i++)
            {
                st[i] = new studiotexture_t();
                st[i].name_offset = br.ReadInt32();
                st[i].flags = br.ReadInt32();
                st[i].used = br.ReadInt32();
                st[i].unused = br.ReadInt32();
                st[i].material = br.ReadInt32();
                st[i].client_material = br.ReadInt32();
                br.BaseStream.Seek(40, SeekOrigin.Current);
            }

            for (int i = 0; i < count; i++)
            {
                br.BaseStream.Seek(offset + st[i].name_offset + (i * 64), SeekOrigin.Begin);
                st[i].textureName = ConvertUtils.ReadNullTerminatedString(br);
                //Debug.Log ("Texture "+i+" name is "+st[i].textureName);
            }

            return st;
        }

        string[] ReadTexturePaths(BinaryReader br, int offs, int count)
        {
            int[] texturePathOffs = new int[count];
            string[] texturePaths = new string[count];
            br.BaseStream.Seek(offs, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                texturePathOffs[i] = br.ReadInt32();
            }

            for (int i = 0; i < count; i++)
            {
                string tempStr = ConvertUtils.ReadNullTerminatedString(br, texturePathOffs[i]);
                tempStr = tempStr.Replace("\\", "/");
                texturePaths[i] = tempStr;
                //Debug.Log ("texturePath "+i+" is "+texturePaths[i]);
            }
            return texturePaths;
        }

        short[] ReadSkinFamilies(BinaryReader br, int offs, int count, int refCount)
        {
            if (mdlHeader.version == 37)
            {
                Debug.LogWarning("Skinfamilies give a Unable to read beyond the end of the stream error in V37 models");
            }

            short[] skinFams = new short[count * refCount];

            br.BaseStream.Seek(offs, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                //skinFams[i] = new short[refCount];
                for (int j = 0; j < refCount; j++)
                {
                    short skinRef = br.ReadInt16();
                    //skinFams[i][j]=skinRef;
                    skinFams[(i * refCount) + j] = skinRef;
                    //Debug.Log ("skin family "+i+" ref "+j+" is "+skinRef);
                }
            }
            return skinFams;
        }

        //=============================================================================
        //===============================VVD===========================================
        //=============================================================================


        public void ParseVvd(string name)
        {
            name = name.Replace(".mdl", ".vvd");

            string path = ResourceManager.GetPath(name);

            //_vertArray = null;
            //_Normals = null;
            //_UV = null;

            BinaryReader BR = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));

            /*vvdheader */
            vvdHeader = ReadVvdHeader(BR);

            //VvdHeader = header;


            vertexFileFixup[] fixupTable = null;
            if (vvdHeader.numFixup > 0)
            {
                //Debug.Log ("VVD file have "+vvdHeader.numFixup +" fixups");
                BR.BaseStream.Seek(vvdHeader.fixupTableStart, SeekOrigin.Begin);

                fixupTable = new vertexFileFixup[vvdHeader.numFixup];

                for (int i = 0; i < vvdHeader.numFixup; i++)
                {
                    fixupTable[i] = new vertexFileFixup(BR.ReadInt32(), BR.ReadInt32(), BR.ReadInt32());
                }
            }

            if (vvdHeader.numLODs > 0)
            {
                vertCount = vvdHeader.numLODVertexes[lodLevel];
                //int vertCount = vvdHeader.numLODVertexes [0];
                //_vertArray = ParseFixup (BR, vvdHeader.fixupTableStart, vvdHeader.numFixup, vvdHeader.vertexDataStart, vvdHeader.tangentDataStart,
                //    out _Normals, out _UV, vertCount);
                ReadVvdVertexes(BR, vvdHeader.vertexDataStart, vertCount, fixupTable);
                //				Debug.Log("VertCount "+vertCount+" vertArrayLength "+vertArray.Length);
            }
            else
            {
                Debug.LogWarning("VVD file numLODs is 0");
            }

            BR.BaseStream.Dispose();
        }

        vvdheader ReadVvdHeader(BinaryReader br)
        {
            vvdheader hdr = new vvdheader();

            hdr.id = br.ReadChars(4);
            hdr.version = br.ReadInt32();
            hdr.checksum = br.ReadInt32();
            hdr.numLODs = br.ReadInt32();
            //		Debug.Log ("VDD header numLODs is "+hdr.numLODs);
            hdr.numLODVertexes = new int[]{ br.ReadInt32 (),br.ReadInt32 (),br.ReadInt32 (),br.ReadInt32 (),
                                            br.ReadInt32 (),br.ReadInt32 (),br.ReadInt32 (),br.ReadInt32 ()};
            hdr.numFixup = br.ReadInt32();
            hdr.fixupTableStart = br.ReadInt32();
            hdr.vertexDataStart = br.ReadInt32();
            hdr.tangentDataStart = br.ReadInt32();

            return hdr;
        }

        void ReadVvdVertexes(BinaryReader br, int vertOffs, int vertCount, vertexFileFixup[] fixupTable)
        {
            if (fixupTable == null)
            {
                boneWeights = new BoneWeight[vertCount];
                vertArray = new Vector3[vertCount];
                Normals = new Vector3[vertCount];
                UV = new Vector2[vertCount];

                br.BaseStream.Seek(vertOffs, SeekOrigin.Begin);
                for (int i = 0; i < vertCount; i++)
                {
                    BoneWeight bw = new BoneWeight();
                    bw.weight0 = br.ReadSingle();
                    bw.weight1 = br.ReadSingle();
                    bw.weight2 = br.ReadSingle();
                    bw.boneIndex0 = br.ReadByte();
                    bw.boneIndex1 = br.ReadByte();
                    bw.boneIndex2 = br.ReadByte();
                    /*numbones =*/
                    br.ReadByte();
                    boneWeights[i] = bw;

                    vertArray[i] = ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br)) * uSrcSettings.Inst.worldScale;
                    Normals[i] = ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br));
                    UV[i] = ConvertUtils.ReadVector2(br);
                }
            }
            else
            {
                List<Vector3> VertexView = new List<Vector3>();
                List<Vector3> NormalView = new List<Vector3>();
                List<Vector2> UVView = new List<Vector2>();
                List<BoneWeight> _boneWeights = new List<BoneWeight>();
                //int target=0;
                //int vertexArrayOffset=0;

                for (int i = 0; i < fixupTable.Length; i++)
                {
                    if (fixupTable[i].lod < lodLevel)
                        continue;

                    for (int j = 0; j < fixupTable[i].numVertexes; j++)
                    {
                        int vertexViewOffset = (int)(vertOffs + ((fixupTable[i].sourceVertexID + j) * 48));
                        br.BaseStream.Seek(vertexViewOffset, SeekOrigin.Begin);

                        //48 bytes
                        BoneWeight bw = new BoneWeight();
                        bw.weight0 = br.ReadSingle();
                        bw.weight1 = br.ReadSingle();
                        bw.weight2 = br.ReadSingle();
                        bw.boneIndex0 = br.ReadByte();
                        bw.boneIndex1 = br.ReadByte();
                        bw.boneIndex2 = br.ReadByte();
                        /*numbones =*/
                        br.ReadByte();
                        _boneWeights.Add(bw);

                        VertexView.Add(ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br)) * uSrcSettings.Inst.worldScale);
                        NormalView.Add(ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br)));
                        UVView.Add(ConvertUtils.ReadVector2(br));

                        //vertexArrayOffset++;
                    }
                    //target += fixupTable[i].numVertexes;
                }
                Normals = NormalView.ToArray();
                UV = UVView.ToArray();
                boneWeights = _boneWeights.ToArray();
                vertArray = VertexView.ToArray();
            }
        }

        /*Vector3[] ParseFixup(BinaryReader BR, int fixupOffs, int fixupCount,int VertexOffs,int tangetnOffs,
		                     out Vector3[] _Normals, out Vector2[] _UV, int vertCount)
		{
			List<Vector3> VertexView = new List<Vector3>();
			List<Vector3> NormalView = new List<Vector3>();
			List<Vector2> UVView = new List<Vector2>();
			List<BoneWeight> _boneWeights = new List<BoneWeight> ();
			_Normals = null;//new Vector3[vertCount];
			_UV = null;//new Vector2[vertCount];
			int vertexArrayOffset=0;
			vertexFileFixup[] fixupTable;

			if (fixupCount == 0) 
			{
				BR.BaseStream.Seek(VertexOffs, SeekOrigin.Begin);
				for (int i=0; i<vertCount; i++) 
				{
					//int vertexOffs = i*48;
					//BR.BaseStream.Seek(16,SeekOrigin.Current);
					BoneWeight bw = new BoneWeight();
					bw.weight0=BR.ReadSingle();
					bw.weight1=BR.ReadSingle();
					bw.weight2=BR.ReadSingle();
					bw.boneIndex0=BR.ReadByte();
					bw.boneIndex1=BR.ReadByte();
					bw.boneIndex2=BR.ReadByte();
					/*numbones = BR.ReadByte();
					_boneWeights.Add (bw);
					VertexView.Add (ConvertUtils.FlipVector(ConvertUtils.ReadVector3(BR))*Settings.Inst.worldScale);
					NormalView.Add (ConvertUtils.FlipVector(ConvertUtils.ReadVector3(BR)));
					UVView.Add (ConvertUtils.ReadVector2 (BR));
					//BR.BaseStream.Seek (8,SeekOrigin.Current);
					vertexArrayOffset++;
				}
			}
			else
			{
				//Debug.Log("FixupCount "+fixupCount+"\n"+
				//          "FixupOffs: "+fixupOffs);

				int target=0;
				fixupTable=new vertexFileFixup[fixupCount];

				BR.BaseStream.Seek(fixupOffs, SeekOrigin.Begin);
				for (int i=0; i<fixupCount; i++) 
				{
					fixupTable[i]=new vertexFileFixup(BR.ReadInt32(),BR.ReadInt32(),BR.ReadInt32());
					
					//Debug.Log ("Fixup LODs: "+fixupTable[i].lod);
					//Debug.Log ("sourceVertexID: "+fixupTable[i].sourceVertexID);
					//Debug.Log ("num vertices: "+fixupTable[i].numVertexes);
				}

				for (int i=0; i<fixupCount; i++) 
				{
					if(fixupTable[i].lod < lodLevel)
						continue;

					for (int j=0; j<fixupTable[i].numVertexes; j++) 
					{
						int vertexViewOffset=(int)(VertexOffs+((fixupTable[i].sourceVertexID+j)*48));
						BR.BaseStream.Seek(vertexViewOffset, SeekOrigin.Begin);

						//48 bytes
						//BR.BaseStream.Seek(16,SeekOrigin.Current);
						BoneWeight bw = new BoneWeight();
						bw.weight0=BR.ReadSingle();
						bw.weight1=BR.ReadSingle();
						bw.weight2=BR.ReadSingle();
						bw.boneIndex0=BR.ReadByte();
						bw.boneIndex1=BR.ReadByte();
						bw.boneIndex2=BR.ReadByte();
						/*numbones = BR.ReadByte();
						_boneWeights.Add (bw);
						VertexView.Add (ConvertUtils.FlipVector(ConvertUtils.ReadVector3(BR))*Settings.Inst.worldScale);
						NormalView.Add (ConvertUtils.FlipVector(ConvertUtils.ReadVector3(BR)));
						UVView.Add (new Vector2(BR.ReadSingle(),BR.ReadSingle()));

						vertexArrayOffset++;
					}
					target += fixupTable[i].numVertexes;
				}
			}
			_Normals = NormalView.ToArray ();
			_UV = UVView.ToArray ();
			boneWeights = _boneWeights.ToArray ();
			return VertexView.ToArray();
		}*/

        //============================================================
        //                        VTX
        //============================================================

        public void ParseVtx(string name)
        {
            switch (VTXType)
            {
                case SourceVTXType.NONE:
                    return;

                case SourceVTXType.SW_VTX:
                    name = name.Replace(".mdl", ".sw.vtx");
                    break;

                case SourceVTXType.DX7:
                    name = name.Replace(".mdl", ".dx7_2bone.vtx");
                    break;

                case SourceVTXType.DX8:
                    name = name.Replace(".mdl", ".dx80.vtx");
                    break;

                case SourceVTXType.DX9:
                    name = name.Replace(".mdl", ".dx90.vtx");
                    break;
            }

            name = name.Replace(" ", "");

            string path = ResourceManager.GetPath(name);

            //_indexArray = null;

            BinaryReader BR = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));

            /*vtxheader*/
            vtxHeader = ReadVtxHeader(BR);

            //VtxHeader = header;

            if (vtxHeader.numLODs > 0)
            {
                /*vtxBodypartHeader[]*/
                vtxBodyParts = ReadVtxBodyParts(BR, vtxHeader.bodyPartOffset, vtxHeader.numBobyParts);
                indexArray = BuildIndices(vtxBodyParts);
            }
            BR.BaseStream.Dispose();
        }

        vtxHeader ReadVtxHeader(BinaryReader BR)
        {
            vtxHeader hdr = new vtxHeader();

            hdr.version = BR.ReadInt32();
            hdr.vertCacheSize = BR.ReadInt32();
            hdr.maxBonePerStrip = BR.ReadInt16();
            hdr.maxBonePerTri = BR.ReadInt16();
            hdr.maxBonePerVert = BR.ReadInt32();
            hdr.checksum = BR.ReadInt32();
            hdr.numLODs = BR.ReadInt32();
            hdr.materialReplacementListOffset = BR.ReadInt32();
            hdr.numBobyParts = BR.ReadInt32();
            hdr.bodyPartOffset = BR.ReadInt32();

            return hdr;
        }

        vtxBodypartHeader[] ReadVtxBodyParts(BinaryReader br, int offs, int count)
        {
            vtxBodypartHeader[] bp = new vtxBodypartHeader[count];

            for (int i = 0; i < count; i++)
            {
                br.BaseStream.Seek(offs + (i * 8), SeekOrigin.Begin);
                bp[i] = new vtxBodypartHeader(br.ReadInt32(), br.ReadInt32());
                if (bp[i].numModels > 0)
                {
                    bp[i].models = ReadVtxModels(br, offs + (i * 8), bp[i]);
                }
                else
                    Debug.LogWarning("VTX Body part " + i + " models count is 0");
            }

            return bp;
        }

        vtxModelHeader[] ReadVtxModels(BinaryReader br, int offset, vtxBodypartHeader bodypart)
        {
            vtxModelHeader[] mh = new vtxModelHeader[bodypart.numModels];

            for (int i = 0; i < bodypart.numModels; i++)
            {
                br.BaseStream.Seek(bodypart.modelsOffset + offset + (i * 8), SeekOrigin.Begin);
                mh[i] = new vtxModelHeader(br.ReadInt32(), br.ReadInt32());
                if (mh[i].numLODs > 0)
                {
                    mh[i].lods = ReadVtxModelLods(br, bodypart.modelsOffset + offset + (i * 8), mh[i], i);
                }
                else
                    Debug.LogWarning("VTX model " + i + " LODs count is 0");
            }
            return mh;
        }

        vtxModelLODheader[] ReadVtxModelLods(BinaryReader br, int offset, vtxModelHeader model, int modelIndex)
        {
            vtxModelLODheader[] mlods = new vtxModelLODheader[model.numLODs];
            for (int i = 0; i < model.numLODs; i++)
            {
                br.BaseStream.Seek(model.LODOffset + offset + (i * 12), SeekOrigin.Begin);
                mlods[i] = new vtxModelLODheader(br.ReadInt32(), br.ReadInt32(), br.ReadSingle());
                if (mlods[i].numMeshes > 0)
                {
                    mlods[i].meshes = ReadVtxMeshes(br, model.LODOffset + offset + (i * 12), mlods[i]);
                }
                else
                    Debug.LogWarning("VTX model " + modelIndex + " lod " + i + " mesh count is 0");
            }
            return mlods;
        }

        vtxMeshHeader[] ReadVtxMeshes(BinaryReader br, int offset, vtxModelLODheader lod)
        {
            vtxMeshHeader[] ms = new vtxMeshHeader[lod.numMeshes];

            for (int i = 0; i < lod.numMeshes; i++)
            {
                br.BaseStream.Seek(lod.meshOffset + offset + (i * 9), SeekOrigin.Begin);
                ms[i] = new vtxMeshHeader(br.ReadInt32(), br.ReadInt32(), br.ReadByte());

                if (ms[i].numStripGroups > 0)
                {
                    ms[i].stripGroups = ReadVtxStripGroups(br, lod.meshOffset + offset + (i * 9), ms[i]);
                }
                //else
                //	Debug.LogWarning("VTX mesh "+ i+" numStripGroups is 0");
            }
            return ms;
        }

        vtxStripGroupHeader[] ReadVtxStripGroups(BinaryReader br, int offset, vtxMeshHeader mesh)
        {
            vtxStripGroupHeader[] sg = new vtxStripGroupHeader[mesh.numStripGroups];

            for (int i = 0; i < mesh.numStripGroups; i++)
            {
                br.BaseStream.Seek(mesh.stripGroupHeaderOffset + offset + (i * 25), SeekOrigin.Begin);
                sg[i] = new vtxStripGroupHeader(br.ReadInt32(), br.ReadInt32(), br.ReadInt32(),
                                                   br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadByte());
                //Debug.Log ("Strip group "+i+" vert count is "+sg[i].numVerts);
                if (sg[i].numVerts > 0)
                {
                    sg[i].verts = ReadVtxVerts(br, mesh.stripGroupHeaderOffset + offset + (i * 25), sg[i]);
                }
                if (sg[i].numIndices > 0)
                {
                    br.BaseStream.Seek(mesh.stripGroupHeaderOffset + offset + (i * 25) + sg[i].indexOffs, SeekOrigin.Begin);
                    for (int j = 0; j < sg[i].numIndices; j++)
                        sg[i].indexArray[j] = br.ReadUInt16();
                }
                if (sg[i].numStrips > 0)
                {
                    sg[i].strips = ReadVtxStrips(br, mesh.stripGroupHeaderOffset + offset + (i * 25), sg[i]);
                }
                //sg[i].strips = ReadStrips(br, mesh.stripGroupHeaderOffset+ offset + (i*25), sg[i]);
                //sg[i].verts = ReadVtxVerts(br, mesh.stripGroupHeaderOffset+ offset + (i*25),sg[i]);
                //br.BaseStream.Seek (mesh.stripGroupHeaderOffset + offset + (i*25)+sg[i].indexOffs,SeekOrigin.Begin);
                //for(int j=0; j<sg[i].numIndices;j++)
                //	sg[i].indexArray[j] = br.ReadUInt16();
            }
            return sg;
        }

        vtxVertex[] ReadVtxVerts(BinaryReader br, int offset, vtxStripGroupHeader stripGroup)
        {
            vtxVertex[] vs = new vtxVertex[stripGroup.numVerts];

            br.BaseStream.Seek(stripGroup.vertOffset + offset, SeekOrigin.Begin);

            for (int i = 0; i < stripGroup.numVerts; i++)
            {
                //br.BaseStream.Seek (stripGroup.vertOffset + offset + (i*9), SeekOrigin.Begin);
                vs[i] = new vtxVertex(br.ReadBytes(3), br.ReadByte(), br.ReadUInt16(), br.ReadBytes(3));
            }
            return vs;
        }

        vtxStripHeader[] ReadVtxStrips(BinaryReader br, int offset, vtxStripGroupHeader stripGroup)
        {
            vtxStripHeader[] st = new vtxStripHeader[stripGroup.numStrips];

            br.BaseStream.Seek(stripGroup.stripOffset + offset, SeekOrigin.Begin);

            for (int i = 0; i < stripGroup.numStrips; i++)
            {
                //br.BaseStream.Seek (stripGroup.stripOffset + offset + (i*25), SeekOrigin.Begin);
                st[i] = new vtxStripHeader(br.ReadInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadInt32(),
                                              br.ReadInt16(), br.ReadByte(), br.ReadInt32(), br.ReadInt32());
            }
            return st;
        }

        int[] BuildIndices(vtxBodypartHeader[] bodyParts)
        {
            if (lodLevel >= vvdHeader.numLODs)
                lodLevel = (vvdHeader.numLODs - 1);
            int indexCount = IterateStripGroups(lodLevel, bodyParts[0]);
            //			Debug.Log ("indexCount "+indexCount);
            int[] indices = new int[indexCount];
            int indexOffset = 0;
            submeshCount = 0;

            //for(int bpId=0; bpId<bodyParts.Length; bpId++)
            //{
            int bpId = 0;
            //Debug.Log("Bp "+bpId+" indexOffset "+indexOffset);
            vtxBodypartHeader vtxBodypart = bodyParts[bpId];
            //studiobodypart mdlBodypart = mdlBodyParts[bpId];
            for (int modelId = 0; modelId < vtxBodypart.models.Length; modelId++)
            {
                vtxModelHeader vtxModel = vtxBodypart.models[modelId];
                studiomodel mdlModel = mdlBodyParts[bpId].models[modelId];

                if (lodLevel != -1)
                {
                    vtxModelLODheader _lod = vtxModel.lods[lodLevel];
                    //Debug.Log ("Lod meshes "+_lod.meshes.Length);
                    for (int meshId = 0; meshId < _lod.numMeshes; meshId++)
                    {
                        vtxMeshHeader vtxMesh = _lod.meshes[meshId];
                        studiomesh mdlMesh = mdlModel.meshes[meshId];

                        for (int stripGroupId = 0; stripGroupId < vtxMesh.numStripGroups; stripGroupId++)
                        {
                            vtxStripGroupHeader stripGroup = vtxMesh.stripGroups[stripGroupId];
                            vtxVertex[] vertTable = stripGroup.verts;

                            for (int i = 0; i < stripGroup.numIndices; i++)
                            {
                                int vertTableIndex = stripGroup.indexArray[i];
                                int index = vertTable[vertTableIndex].origMeshVertID + mdlModel.verticesoffs + mdlMesh.vertexoffs;
                                //Debug.Log ("Index "+index);
                                if (index < vertCount)//40500)
                                    indices[indexOffset] = index;
                                else
                                    indices[indexOffset] = 0;
                                indexOffset++;
                            }
                            //submeshCount+=stripGroup.numStrips;
                            //submeshCount++;
                        }
                        submeshCount++;
                    }
                }
                else
                    Debug.LogWarning("Lod level is -1");
            }
            //}
            return indices;
        }

        /*int[] BuildIndices(vtxBodypartHeader vtxBodyPart, int mdlbpindex)
		{
			if (lodLevel >= vvdHeader.numLODs)
				lodLevel = (vvdHeader.numLODs - 1);
			int indexCount = IterateStripGroups (lodLevel, vtxBodyPart);
			//Debug.Log ("indexCount "+indexCount);
			int[] indices=new int[indexCount];
			int indexOffset=0;

			//studiobodypart mdlBodypart = mdlBodyParts[bpId];
			for(int modelId=0; modelId<vtxBodyPart.models.Length; modelId++)
			{
				vtxModelHeader vtxModel = vtxBodyPart.models[modelId];
				studiomodel mdlModel=mdlBodyParts[mdlbpindex].models[modelId];

				if(lodLevel!=-1)
				{
					vtxModelLODheader _lod = vtxModel.lods[lodLevel];
					for(int meshId=0; meshId<_lod.numMeshes; meshId++)
					{
						vtxMeshHeader vtxMesh=_lod.meshes[meshId];
						studiomesh mdlMesh=mdlModel.meshes[meshId];
						
						for(int stripGroupId=0; stripGroupId<vtxMesh.numStripGroups; stripGroupId++)
						{
							vtxStripGroupHeader stripGroup=vtxMesh.stripGroups[stripGroupId];
							vtxVertex[] vertTable = stripGroup.verts;
							
							for(int i=0; i<stripGroup.numIndices;i++)
							{
								int vertTableIndex=stripGroup.indexArray[i];
								int index = vertTable[vertTableIndex].origMeshVertID + mdlModel.verticesoffs + mdlMesh.vertexoffs;
								//Debug.Log ("Index "+index);
								if(index<40500)
									indices[indexOffset]=index;
								else
									indices[indexOffset]=0;
								indexOffset++;
							}
						}
					}
				}
				else
					Debug.LogWarning("Lod level is -1");
			}
			
			return indices;
		}*/

        int IterateStripGroups(int lodID, vtxBodypartHeader bodyPart)
        {
            int indices = 0;

            foreach (vtxModelHeader model in bodyPart.models)
            {
                if (lodID != -1 && model.lods.Length > 0)
                {
                    vtxModelLODheader lodheader = model.lods[lodID];
                    foreach (vtxMeshHeader mesh in lodheader.meshes)
                    {
                        foreach (vtxStripGroupHeader stripGroup in mesh.stripGroups)
                        {
                            //Debug.Log ("Strip group num indices "+stripGroup.numIndices);
                            indices += stripGroup.numIndices;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("model.lods.Length < 0 or lodID is -1");
                }
            }
            return indices;
        }
    }

}
