using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace uSrcTools
{
	public class VMTLoader
	{
		public class VMTFile
		{
			public string shader;

			public string basetexture;
			public string basetexture2;
			public string bumpmap;
			public string surfaceprop;
			public string detil;
			public string dudvmap;
			public float detailscale;

			public bool alphatest;
			public bool translucent;
			public bool selfillum;
			public bool additive;

			public string envmap;
			public float basealphaenvmapmask;
			public float envmapcontrast;
			public float envmapsaturation;
			public Vector3 envmaptint;

		}

		public static VMTFile ParseVMTFile(string name)
		{
			VMTFile material = new VMTFile();

			string path = "";

			if (name.StartsWith("/"))
				name = name.Replace ("/", "");

			if (name.Contains ("materials/"))
				name = name.Replace ("materials/", "");

			if (name.Contains (".vmt"))
				name = name.Replace (".vmt", "");
			
			path = ResourceManager.GetPath ("materials/"+name+".vmt");
			
			if(path==null)
			{
				Debug.Log ("materials/"+name+".vmt: Not Found");
				return null;
			}


			string[] file = File.ReadAllLines (path);
			string[] temp = null;
			string line = null;
			int depth = 0;
			Dictionary<string, string> parameters = new Dictionary<string, string> ();
			string block=null;
			for (int i=0; i<file.Length; i++) 
			{
				line=file[i].Trim().Trim('\t');
				line = line.Replace("\"\"", "\" \"");

				if(string.IsNullOrEmpty(line) || line.StartsWith("//"))
					continue;

				if(line.StartsWith("{"))
				{
					depth++;
					continue;
				}

				if(depth == 0)
				{
					material.shader=line.Trim('"').ToLower();
				}
				else if(depth==1)
				{
					if(line.StartsWith("}"))
					{
						depth--;
						if(depth==0)
							break;
					}
					else
					{
						if(line.Split(new char[]{' ', '\t'}).Length<2)
						{
							block=line.Trim('"').ToLower();
							//Debug.Log("Start block "+block);
							//Debug.Log ("Line is short "+line);
						}
						else
						{
							temp=line.Trim().Split(new char[] {' ', '\t'},2);
							if(temp.Length<2)
								Debug.Log (path+" "+line);
							parameters.Add(temp[0].Trim('"').ToLower(), temp[1].Trim().Trim('"'));
						}
					}
				}
				else if(depth==2)
				{
					if(line.StartsWith("}"))
					{
						depth--;
						if(depth==0)
							break;
					}
					else
					{
						if(line.Split(new char[]{' ', '\t'}).Length<2)
						{
							//Debug.Log ("Line is short "+line);
						}
						else
						{
							if(block=="insert")
							{
								temp=line.Trim().Split(new char[] {' ', '\t'},2);
								if(temp.Length<2)
									Debug.Log (path+" "+line);
									
								if(!parameters.ContainsKey(temp[0].Trim('"').ToLower()))
									parameters.Add(temp[0].Trim('"').ToLower(), temp[1].Trim().Trim('"'));
								else
									parameters[temp[0].Trim('"').ToLower()]=temp[1].Trim().Trim('"');
							}
						}
					}
				}

				if(line.StartsWith("}"))
				{
					depth--;
					if(depth==0)
						break;
				}

			}

			if(material.shader=="patch")
			{
				if(parameters.ContainsKey("include"))
				{
					//Debug.Log (name+ " include "+parameters["include"].ToLower()); debug
					material = ParseVMTFile(parameters["include"].ToLower());
					//return ParseVMTFile(parameters["include"].ToLower());

					if(material==null)
					{
						Debug.LogError("Include \""+parameters["include"].ToLower()+"\" from material \""+name+"\" missing");
						return null;
					}
				}
				else
				{
					Debug.LogWarning("Shader is patch but has no parameter include "+path);
					for(int i=0;i<parameters.Keys.ToArray().Length;i++)
						Debug.Log (parameters.Keys.ToArray()[i]);
					return null;
				}
			}

			/*if (parameters.ContainsKey("$basetexturetransform")) 
			{
				//file[file.FindIndex (n => n.ToLower ().Contains ("$basetexturetransform"))]="";
			}*/

			if(parameters.ContainsKey("$basetexture"))
			{
				material.basetexture=parameters["$basetexture"];
			}
			else
			{
				//if(material.shader!="patch")
				//	Debug.Log ("File "+name+".vmt with shader"+material.shader+" dont contains texture name");
				if(parameters.ContainsKey("%tooltexture"))
				{
					//Debug.Log ("Split strings count: "+temp.Length);
					material.basetexture=parameters["%tooltexture"];
				}
				else if(parameters.ContainsKey("$iris"))
				{
					material.basetexture=parameters["$iris"];
				}
				//else
				//	material.basetexture = null;


			}

			if(parameters.ContainsKey("$basetexture2"))
			{
				material.basetexture2=parameters["$basetexture2"];
			}

			if(parameters.ContainsKey("$bumpmap"))
			{
				material.bumpmap=parameters["$bumpmap"];
			}

			if(parameters.ContainsKey("$surfaceprop"))
			{
				material.surfaceprop = parameters["$surfaceprop"];
			}

			if(parameters.ContainsKey("alphatest"))
			{
				if(parameters["alphatest"]=="1")
				   material.alphatest = true;
			}

			if(parameters.ContainsKey("$alphatest"))
			{
				if(parameters["$alphatest"]=="1")
					material.alphatest = true;
			}

			if(parameters.ContainsKey("$selfillum"))
			{
				if(parameters["$selfillum"]=="1")
					material.selfillum = true;
			}

			if(parameters.ContainsKey("$translucent"))
			{
				if(parameters["$translucent"]=="1")
					material.translucent = true;
			}

			if(parameters.ContainsKey("$additive"))
			{
				if(parameters["$additive"]=="1")
					material.additive = true;
			}
			
			if(parameters.ContainsKey("$dudvmap"))
			{
				material.dudvmap = parameters["$dudvmap"];
			}
			
			if(parameters.ContainsKey("$normalmap"))
			{
				material.dudvmap = parameters["$normalmap"];
			}
			
			return material;
		}


		/*public static VMTFile LoadVMTFile(string name)
		{
			//VMTFile material = new VMTFile ();

			string path = "";

			if (name.Contains ("materials/"))
				name = name.Replace ("materials/", "");

			if (name.Contains (".vmt"))
				name = name.Replace (".vmt", "");

			path = PathUtils.GetPath ("materials/" + name + ".vmt");

			if (path == null) 
			{
				//	Debug.Log ("materials/"+name+".vmt: Not Found");
				return null;
			}

			string[] file = File.ReadAllLines (path);

			return ParseLinesToVMT (file);
		}*/
		
		/*public static VMTFile ParseLinesToVMT(string[] body)
		{
			int depth = 0;
			IList<string> newBody = new List<string>();

			for (int i=0; i<body.Length; i++) 
			{
				string line=body[i].Trim ();

				if(string.IsNullOrEmpty(line)||line.StartsWith("//"))
					continue;

				bool readable = line.FirstOrDefault()!=default(char);

				if(readable && line.First ()=='{')
					depth++;

				if(depth==0)
				{
					if(line.Trim().StartsWith("\""))
						newBody.Add (line);
					else
					{}
				}
				else
				{}
			
				if(readable && line.First () == '}')
					depth--;

				//if()

			}

			return null;
		}*/


	}

}
