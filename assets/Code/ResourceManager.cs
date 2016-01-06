using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace uSrcTools
{
	public class ResourceManager : MonoBehaviour
	{
		private static ResourceManager inst;
		public static ResourceManager Inst
		{
			get
			{
				return inst??(inst=new GameObject("ResourceManager").AddComponent<ResourceManager>());
			}
		}

		public Dictionary <string, SourceStudioModel> models 		= new Dictionary<string, SourceStudioModel>();
		public Dictionary <string, Texture> Textures 				= new Dictionary<string, Texture> ();
		public Dictionary <string, Material> Materials 				= new Dictionary<string, Material> ();
		public Dictionary <string, VMTLoader.VMTFile> VMTMaterials 	= new Dictionary<string, VMTLoader.VMTFile> ();

		void Awake()
		{
			inst = this;
		}

		public SourceStudioModel GetModel(string modelName)
		{
			modelName = modelName.ToLower ();
			if(models.ContainsKey(modelName))
			{
				return models[modelName];
			}
			else
			{
				SourceStudioModel tempModel = new SourceStudioModel().Load(modelName);
				models.Add (modelName, tempModel);
				return tempModel;
			}

		}

		public VMTLoader.VMTFile GetVMTMaterial(string materialName)
		{
			VMTLoader.VMTFile vmtFile=null;
			if (!VMTMaterials.ContainsKey (materialName)) 
			{
				vmtFile = VMTLoader.ParseVMTFile (materialName);
				VMTMaterials.Add (materialName, vmtFile);
			}
			else 
			{
				vmtFile = VMTMaterials [materialName];
			}
			return vmtFile;
		}

		public Texture GetTexture(string textureName)
		{
			textureName=textureName.ToLower();
			if(textureName.Contains("_rt_camera"))
			{
				if (!Textures.ContainsKey (textureName))
					Textures.Add (textureName,Test.Inst.cameraTexture);
				return Textures [textureName];
			}
			else
			{
				if (!Textures.ContainsKey (textureName))
					Textures.Add (textureName, VTFLoader.LoadFile (textureName));
				return Textures [textureName];
			}
		}

		public Material GetMaterial(string materialName)
		{
			Material tempmat=null;
			
			if(Materials.ContainsKey (materialName))
				return Materials[materialName];
			
			//VMT
			VMTLoader.VMTFile vmtFile = GetVMTMaterial (materialName);
			
			//Material
			if (vmtFile != null)
			{
				if(vmtFile.shader=="lightmappedgeneric")
				{
					if(vmtFile.selfillum)
						tempmat = new Material(uSrcSettings.Inst.sSelfillum);
					else if(!vmtFile.translucent && !vmtFile.alphatest)
						tempmat = new Material(uSrcSettings.Inst.sDiffuse);
					else
						tempmat = new Material(uSrcSettings.Inst.sTransparent);
				}
				else if(vmtFile.shader=="unlitgeneric")
				{
					if(vmtFile.additive)
					{
						tempmat = new Material(uSrcSettings.Inst.sAdditive);
						tempmat.SetColor("_TintColor",Color.white);
					}
					else if(!vmtFile.translucent && !vmtFile.alphatest)
						tempmat = new Material(uSrcSettings.Inst.sUnlit);
					else
						tempmat = new Material(uSrcSettings.Inst.sUnlitTransparent);
				}
				else if(vmtFile.shader=="unlittwotexture")
				{
					tempmat = new Material(uSrcSettings.Inst.sUnlit);
				}
				else if(vmtFile.shader=="vertexlitgeneric")
				{
					if(vmtFile.selfillum)
						tempmat = new Material(uSrcSettings.Inst.sSelfillum);
					else if(vmtFile.alphatest)
						tempmat = new Material(uSrcSettings.Inst.sAlphatest);
					else if(vmtFile.translucent) 
						tempmat = new Material(uSrcSettings.Inst.sTransparent);
					else
						tempmat = new Material(uSrcSettings.Inst.sVertexLit);
				}
				else if(vmtFile.shader=="refract")
				{
					tempmat = new Material(uSrcSettings.Inst.sRefract);
				}
				else if(vmtFile.shader=="worldvertextransition")
				{
					tempmat = new Material(uSrcSettings.Inst.sWorldVertexTransition);

					string bt2=vmtFile.basetexture2;
					Texture tex2=GetTexture(bt2);
					tempmat.SetTexture("_MainTex2",tex2);
					if(tex2==null)
						Debug.LogWarning("Error loading second texture "+bt2+" from material "+materialName);
				}
				else if(vmtFile.shader=="water")
				{
					Debug.LogWarning("Shader "+vmtFile.shader+" from VMT "+materialName+" not suported");
					tempmat = new Material(uSrcSettings.Inst.sTransparent);
					tempmat.color=new Color(1,1,1,0.3f);
				}
				else if(vmtFile.shader=="black")
				{
					tempmat = new Material(uSrcSettings.Inst.sUnlit);
					tempmat.color=Color.black;
				}
				else if(vmtFile.shader=="infected")
				{
					tempmat = new Material(uSrcSettings.Inst.sDiffuse);
				}
				/*else if(vmtFile.shader=="eyerefract")
				{
					Debug.Log ("EyeRefract shader not done. Used Diffuse");
					tempmat = new Material(uSrcSettings.Inst.sDiffuse);
				}*/
				else
				{
					Debug.LogWarning("Shader "+vmtFile.shader+" from VMT "+materialName+" not suported");
					tempmat = new Material(uSrcSettings.Inst.sDiffuse);
				}
				
				tempmat.name = materialName;
				
				string textureName = vmtFile.basetexture;

				if(textureName!=null)
				{
					textureName = textureName.ToLower();

					Texture mainTex=GetTexture(textureName);
					tempmat.mainTexture = mainTex;
					if(mainTex==null)
						Debug.LogWarning("Error loading texture "+textureName+" from material "+materialName);
				}
				else
				{
					//tempmat.shader = Shader.Find ("Transparent/Diffuse");
					//tempmat.color = new Color (1, 1, 1, 0f);
				}
				
				if(vmtFile.dudvmap!=null&vmtFile.shader=="refract")
				{
					string dudv=vmtFile.dudvmap.ToLower ();
					Texture dudvTex=GetTexture(dudv);
					tempmat.SetTexture("_BumpMap",dudvTex);
					if(dudvTex==null)
						Debug.LogWarning("Error loading texture "+dudv+" from material "+materialName);
				}
					
				Materials.Add (materialName,tempmat);
				return tempmat;
			}
			else
			{
				//Debug.LogWarning("Error loading "+materialName);
				Materials.Add (materialName, Test.Inst.testMaterial);
				return Test.Inst.testMaterial;
			}
		}

		public static string GetPath(string filename)
		{
			filename=filename.Replace ("\\","/");
			filename=filename.Replace ("//","/");
			string path="";
			if(uSrcSettings.Inst.haveMod)
			{
				if(uSrcSettings.Inst.mod!="none"||uSrcSettings.Inst.mod!="")
				{
					path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.mod + "/";
					if(CheckFile(path + filename))
					{
						return path + filename;
					}
				}
			}
			
			path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/";
			if(CheckFile(path + filename))
			{
				return path + filename;
			}
			else if(CheckFullFiles(filename))
			{
				return path + filename;
			}
			
			Debug.LogWarning (uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/" + filename+": Not Found");
			return null;
		}
	
		static bool CheckFullFiles(string filename)
		{
			string path=uSrcSettings.Inst.path + "/"+uSrcSettings.Inst.game+"full/";
			
			if(!CheckFile(path + filename))
				return false;
			
			string dirpath=uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/";
			
			Debug.LogWarning ("Copying: "+path + filename+" to "+dirpath+filename);

			
			if(!Directory.Exists (dirpath + filename.Remove(filename.LastIndexOf("/"))))
				Directory.CreateDirectory(dirpath + filename.Remove(filename.LastIndexOf("/")));
				
			File.Copy ( path + filename, dirpath+ filename);
		
			return true;
		
		}
	
		public static string FindModelMaterialFile(string filename, string[] dirs)
		{
			filename=filename.Replace ("\\","/");
			filename=filename.Replace ("//","/");
			filename+=".vmt";
			string path="";
			
			path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/materials/";
			if(CheckFile(path + filename))
			{
				return filename;
			}
			else if(CheckFullFiles("materials/"+filename))
			{
				return filename;
			}
			
			for(int i=0;i<dirs.Length;i++)
			{
				path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/materials/"+dirs[i];
				if(CheckFile(path + filename))
				{
					return dirs[i] + filename;
				}
				else if(CheckFullFiles("materials/"+dirs[i]+filename))
				{
					return dirs[i] + filename;
				}
			}
			
			//Debug.LogWarning ("Model material "+dirs[0]+filename+": Not Found");
			return dirs[0]+filename;
		}
	
		static bool CheckFile(string path)
		{
			if(Directory.Exists (path.Remove(path.LastIndexOf("/"))))
			{
				if(File.Exists (path))
				{
					return true;
				}
			}
			return false;
		}
	}
}