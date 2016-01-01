using System;
using System.IO;
using UnityEngine;

namespace uSrcTools
{
	[Serializable]
	public class BSPFile
	{
		private string LevelName;
		BinaryReader br;
		public string tempLog;

		public bspheader header;
		[HideInInspector]
		public string entitiesLump;
		//bspplane[] planesLump;
		public bsptexdata[] texdataLump;
		public Vector3[] vertexesLump;
		//bspvis[] visibilityLump;
		//bspnode[] nodesLump;
		public bsptexinfo[] texinfosLump;
		public bspface[] facesLump;
		public RGBExp32[] lightingLump;
		//
		//bspleaf[] leafsLump;
		//
		public ushort[][] edgesLump;
		public int[] surfedgesLump;
		public bspmodel[] modelsLump;
		//
		//
		public bspdispinfo[] dispinfoLump;
		//
		//public byte[] dispAlphasLumps;
		public bspDispVert[] dispVertsLump;
		//
		//
		public int gameLumpCount;
		public bspgamelump[] gameLump;
			public int staticPropDictCount;
			public string[] staticPropDict;
			public int staticPropLeafCount;
			public ushort[] staticPropLeaves;
			public int StaticPropCount;
			public bspStaticPropLump[] StaticPropLump;
		
		//
		//
		//
		public char[] texdataStringDataLump;
		public string texdataString;
		public int[] texdataStringTableLump;

		public bool staticPropsReaded=false;
		public bool hasLightmaps;

		public BSPFile (BinaryReader nbr, string name)
		{
			LevelName=name;
			br = nbr;

			header = Readheader ();
			if(uSrcSettings.Inst.entities)
				entitiesLump = ReadEntities ();
			//ParseEntities (entitiesLump);
			
			//planesLump = ReadPlanes ();
			texdataLump = ReadTexData ();
			vertexesLump = ReadVertexes ();
			//
			//
			texinfosLump = ReadTexInfos ();
			facesLump = ReadFaces ();
			if (uSrcSettings.Inst.lightmaps)
				lightingLump = ReadLighting ();
			else
				hasLightmaps = false;
			//
			//
			//
			edgesLump = ReadEdges ();
			surfedgesLump = ReadSurfedges ();
			modelsLump = ReadModels ();
			//Debug.Log ("World lights lump length: " + header.lumps [SourceBSPStructs.LUMP_WORLDLIGHTS].filelen);
			if (uSrcSettings.Inst.displacements) 
			{
				dispinfoLump = ReadDispInfo ();
				//
				//dispAlphasLumps= ReadDispAlphas();
				dispVertsLump = ReadDispVerts ();
				//
			}
			if (uSrcSettings.Inst.props) 
			{
				gameLump = ReadGameLump ();

				ParseGameLumps ();
			}
			//
			//
			//
			if(uSrcSettings.Inst.textures)
				readTexdataString ();

		}


		//==========================================================================
		//                      BSP File Read Functions
		//==========================================================================
		
		
		bspheader Readheader()
		{
			br.BaseStream.Seek (0, SeekOrigin.Begin);
			bspheader header = new bspheader ();
			header.magic = new string (br.ReadChars(4));
			header.version = br.ReadInt32 ();
			header.lumps=new bsplump[SourceBSPStructs.HEADER_LUMPS];
			for(int i=0;i<SourceBSPStructs.HEADER_LUMPS;i++)
			{
				header.lumps[i] = new bsplump();
				if(!Test.Inst.isL4D2)
				{
					header.lumps[i].fileofs = br.ReadInt32 ();
					header.lumps[i].filelen = br.ReadInt32 ();
					header.lumps[i].version = br.ReadInt32 ();
					header.lumps[i].fourCC = new string(br.ReadChars(4));
				}
				else
				{
					header.lumps[i].version = br.ReadInt32 ();
					header.lumps[i].fileofs = br.ReadInt32 ();
					header.lumps[i].filelen = br.ReadInt32 ();
					header.lumps[i].fourCC = new string(br.ReadChars(4));
				}
			}
			Debug.Log (header.magic+" version: "+header.version);
			return header;
		}
		
		string ReadEntities()
		{
			br.BaseStream.Seek (header.lumps [0].fileofs, SeekOrigin.Begin);
			return new string(br.ReadChars (header.lumps[SourceBSPStructs.LUMP_ENTITIES].filelen));
		}
		
		/*bspplane[] ReadPlanes()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_PLANES].fileofs, SeekOrigin.Begin);
			int numPlanes = header.lumps [SourceBSPStructs.LUMP_PLANES].filelen / 20;
			bspplane[] temp = new bspplane[numPlanes];
			for(int i=0;i<numPlanes;i++)
			{
				bspplane plane = new bspplane();
				plane.normal = ConvertUtils.ReadVector(br);
				plane.dist = br.ReadSingle ();
				plane.type = br.ReadInt32 ();
				temp[i] = plane;
			}
			tempLog+=("Load: "+numPlanes+" Planes \n");
			return temp;
		}*/
		
		bsptexdata[] ReadTexData()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_TEXDATA].fileofs, SeekOrigin.Begin);
			int numTexData = header.lumps [SourceBSPStructs.LUMP_TEXDATA].filelen / 32;
			bsptexdata[] temp = new bsptexdata[numTexData];
			for(int i=0; i < numTexData; i++)
			{
				bsptexdata texdata = new bsptexdata();
				texdata.reflectivity = ConvertUtils.ReadVector3(br);
				texdata.nameStringTableID = br.ReadInt32 ();
				texdata.width = br.ReadInt32 ();
				texdata.height = br.ReadInt32 ();
				texdata.view_width = br.ReadInt32 ();
				texdata.view_height = br.ReadInt32 ();
				temp[i] = texdata;
			}
			tempLog+=("Load: "+numTexData+" TextureData's \n");
			return temp;
		}
		
		Vector3[] ReadVertexes()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_VERTEXES].fileofs, SeekOrigin.Begin);
			int numVerts = header.lumps [SourceBSPStructs.LUMP_VERTEXES].filelen / 12;
			Vector3[] temp = new Vector3[numVerts];
			for(int i=0; i<numVerts; i++)
			{
				temp[i] = ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br));
			}
			tempLog +=("Load: "+numVerts+" Vertexes \n");
			return temp;
		}
		
		bsptexinfo[] ReadTexInfos()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_TEXINFO].fileofs, SeekOrigin.Begin);
			int numTexinfos = header.lumps [SourceBSPStructs.LUMP_TEXINFO].filelen / 72;
			bsptexinfo[] temp = new bsptexinfo[numTexinfos];
			for(int i=0;i<numTexinfos;i++)
			{
				bsptexinfo texinfo = new bsptexinfo();
				texinfo.texvecs = ConvertUtils.FlipVector (ConvertUtils.ReadVector3(br));
				texinfo.texoffs = br.ReadSingle();
				texinfo.texvect = ConvertUtils.FlipVector (ConvertUtils.ReadVector3(br));
				texinfo.texofft = br.ReadSingle();
				
				texinfo.lightvecs = ConvertUtils.FlipVector (ConvertUtils.ReadVector3(br));
				texinfo.lightoffs = br.ReadSingle();
				texinfo.lightvect = ConvertUtils.FlipVector (ConvertUtils.ReadVector3(br));
				texinfo.lightofft = br.ReadSingle();
				
				texinfo.flags = br.ReadInt32();
				texinfo.texdata = br.ReadInt32();
				temp[i] = texinfo;
			}
			tempLog += ("Load: "+numTexinfos+" TexInfos \n");
			return temp;
		}
		
		bspface[] ReadFaces()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_FACES].fileofs, SeekOrigin.Begin);
			int numFaces = header.lumps [SourceBSPStructs.LUMP_FACES].filelen / 56;
			bspface[] temp=new bspface[numFaces];
			for(int i=0;i<numFaces;i++)
			{
				bspface face = new bspface();
				face.planenum = br.ReadUInt16();
				face.side = br.ReadByte();
				face.onNode = br.ReadByte();
				face.firstedge = br.ReadInt32 ();
				face.numedges = br.ReadInt16 ();
				face.texinfo = br.ReadInt16 ();
				face.dispinfo = br.ReadInt16 ();
				face.surfaceFogVolumeID = br.ReadInt16 ();
				face.styles = br.ReadBytes (4);
				face.lightofs = br.ReadInt32 ();
				face.area = br.ReadSingle();
				face.LightmapTextureMinsInLuxels = new int[]{ br.ReadInt32(), br.ReadInt32() };
				face.LightmapTextureSizeInLuxels = new int[]{ br.ReadInt32(), br.ReadInt32() };
				face.origFace = br.ReadInt32 ();
				face.numPrims = br.ReadUInt16();
				face.firstPrimID = br.ReadUInt16();
				face.smoothingGroups = br.ReadUInt32();
				temp[i] = face;
			}
			tempLog+=("Load: "+numFaces+" Faces \n");
			//Debug.Log ("Load: "+numFaces+" Faces");
			return temp;
		}
		
		RGBExp32[] ReadLighting()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_LIGHTING].fileofs, SeekOrigin.Begin);
			int lmapCount = header.lumps[SourceBSPStructs.LUMP_LIGHTING].filelen / 4;
			Debug.Log("Lightmap lump size: "+lmapCount);

			if (lmapCount == 0)
			{
				hasLightmaps = false;
				Debug.Log ("Map haven't lightmaps");
				return null;//ReadHDRLighting();
			}
			hasLightmaps=true;

			RGBExp32[] temp = new RGBExp32[lmapCount];

			for(int i = 0; i < lmapCount; i++)
			{
				RGBExp32 col = new RGBExp32();
				col.r = br.ReadByte();
				col.g = br.ReadByte();
				col.b = br.ReadByte();
				col.exp = br.ReadSByte();
				temp[i] = col;
			}

			return temp;
		}
		
		RGBExp32[] ReadHDRLighting()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_LIGHTING_HDR].fileofs, SeekOrigin.Begin);
			int lmapCount = header.lumps[SourceBSPStructs.LUMP_LIGHTING_HDR].filelen / 4;
			Debug.Log("HDR Lightmap lump size: "+header.lumps[SourceBSPStructs.LUMP_LIGHTING_HDR].filelen );

			if (lmapCount == 0)
			{
				hasLightmaps = false;
				Debug.Log ("Map haven't hdr lightmaps");
				return ReadHDRLighting();
			}
			hasLightmaps=true;

			RGBExp32[] temp = new RGBExp32[lmapCount];

			for(int i = 0; i < lmapCount; i++)
			{
				RGBExp32 col = new RGBExp32();
				col.r = br.ReadByte();
				col.g = br.ReadByte();
				col.b = br.ReadByte();
				col.exp = br.ReadSByte();
				temp[i] = col;
			}

			return temp;
		}
		
		ushort[][] ReadEdges()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_EDGES].fileofs, SeekOrigin.Begin);
			int numEdges = header.lumps [SourceBSPStructs.LUMP_EDGES].filelen / 4;
			ushort[][] temp = new ushort[numEdges][];
			for(int i=0;i<numEdges;i++)
			{
				temp[i] = new ushort[]{ br.ReadUInt16 (), br.ReadUInt16 ()};
			}
			tempLog+= ("Load :"+numEdges+" Edges \n");
			return temp;
		}
		
		int[] ReadSurfedges ()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_SURFEDGES].fileofs, SeekOrigin.Begin);
			int numSurfEdges = header.lumps [SourceBSPStructs.LUMP_SURFEDGES].filelen / 4;
			int[] temp = new int[numSurfEdges];
			for(int i=0;i<numSurfEdges;i++)
			{
				temp[i] = br.ReadInt32 ();
			}
			tempLog += ("Load :"+numSurfEdges+" SurfEdges \n");
			return temp;
		}
		
		bspmodel[] ReadModels()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_MODELS].fileofs, SeekOrigin.Begin);
			int modelCount = header.lumps [SourceBSPStructs.LUMP_MODELS].filelen / 48;
			bspmodel[] temp = new bspmodel[modelCount];
			for(int i=0;i<modelCount;i++)
			{
				bspmodel model=new bspmodel();
				model.mins = ConvertUtils.ReadVector3(br);
				model.maxs = ConvertUtils.ReadVector3(br);
				model.origin = ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br));
				model.headnode = br.ReadInt32();
				model.firstface = br.ReadInt32 ();
				model.numfaces = br.ReadInt32 ();
				temp[i] = model;
			}
			tempLog+= ("Load :"+modelCount+" Models \n");
			return temp;
		}
		
		
		bspdispinfo[] ReadDispInfo()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_DISPINFO].fileofs, SeekOrigin.Begin);
			int dispinfoCount = header.lumps [SourceBSPStructs.LUMP_DISPINFO].filelen / 176;
			bspdispinfo[] temp = new bspdispinfo[dispinfoCount];
			for(int i=0;i<dispinfoCount;i++)
			{
				temp[i].startPosition = ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br));
				temp[i].DispVertStart = br.ReadInt32();
				temp[i].DispTriStart = br.ReadInt32();
				temp[i].power = br.ReadInt32();
				temp[i].minTess = br.ReadInt32();
				temp[i].smoothingAngle = br.ReadSingle();//32
				temp[i].contents = br.ReadInt32 ();
				temp[i].MapFace = br.ReadUInt32();
				temp[i].LightmapAlphaStart = br.ReadInt32();
				temp[i].LightmapSamplePositionStart = br.ReadInt32();//46b
				
				br.BaseStream.Seek(128,SeekOrigin.Current);
				
				//temp[i].EdgeNeighbors = ReadDispNeighbor(4);//40b
				//temp[i].CornerNeighbors = ReadDispCornerNeighbors(4);//36b
				//br.BaseStream.Seek(40,SeekOrigin.Current);//40b

			}
			tempLog+= ("Load :"+dispinfoCount+" DispInfos \n");
			Debug.Log ("Load :"+dispinfoCount+" DispInfos ");
			Debug.Log ("DispInfo lump Offset: "+header.lumps [SourceBSPStructs.LUMP_DISPINFO].fileofs+
			           " \nDispInfo lump Length: "+header.lumps [SourceBSPStructs.LUMP_DISPINFO].filelen+
			           " \nOne DispInfo Length(?): "+176+
			           " \nDispInfo lump Count: "+header.lumps [SourceBSPStructs.LUMP_DISPINFO].filelen / 176);
			return temp;
		}
		
		DisplaceNeighbor[] ReadDispNeighbor(int count)
		{
			DisplaceNeighbor[] temp = new DisplaceNeighbor[count];

			//br.BaseStream.Seek (10 * count, SeekOrigin.Current);
			for (int i=0; i<count; i++) 
			{
				//temp[i].m_SubNeighbors=ReadD
			}

			return temp;
		}
		
		DisplaceCornerNeighbors[] ReadDispCornerNeighbors(int count)
		{
			DisplaceCornerNeighbors[] temp = new DisplaceCornerNeighbors[count];
			
			br.BaseStream.Seek (9 * count, SeekOrigin.Current);
			
			return temp;
		}

		byte[] ReadDispAlphas()
		{
			br.BaseStream.Seek(header.lumps[SourceBSPStructs.LUMP_DISP_LIGHTMAP_ALPHAS].fileofs,SeekOrigin.Begin);
			int dispAlphasCount=header.lumps[SourceBSPStructs.LUMP_DISP_LIGHTMAP_ALPHAS].filelen;
			
			Debug.Log ("Load :"+dispAlphasCount+" DispAlphas ");
			return br.ReadBytes(dispAlphasCount);
		}
		
		bspDispVert[] ReadDispVerts()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_DISP_VERTS].fileofs, SeekOrigin.Begin);
			int dispVertCount = header.lumps [SourceBSPStructs.LUMP_DISP_VERTS].filelen / 20;
			bspDispVert[] temp = new bspDispVert[dispVertCount];
			for(int i=0;i<dispVertCount;i++)
			{
				bspDispVert vert=new bspDispVert();
				vert.vec = ConvertUtils.FlipVector(ConvertUtils.ReadVector3(br));
				vert.dist = br.ReadSingle();
				vert.alpha = br.ReadSingle();
				temp[i] = vert;
			}
			tempLog+= ("Load :"+dispVertCount+" DispVerts \n");
			Debug.Log ("Load :"+dispVertCount+" DispVerts ");
			return temp;
		}
		
		bspgamelump[] ReadGameLump()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_GAME_LUMP].fileofs, SeekOrigin.Begin);
			gameLumpCount = br.ReadInt32 ();
			bspgamelump[] temp = new bspgamelump[gameLumpCount];
			for(int i=0;i<gameLumpCount;i++)
			{
				bspgamelump gamelump=new bspgamelump();
				gamelump.id = br.ReadInt32();
				gamelump.flags = br.ReadUInt16 ();
				gamelump.version = br.ReadUInt16 ();
				gamelump.fileofs = br.ReadInt32 ();
				gamelump.filelen = br.ReadInt32 ();
				temp[i] = gamelump;
			}
			tempLog+= ("Load :"+gameLumpCount+" GameLumps \n");
			return temp;
		}
		
		
		void ParseGameLumps()
		{
			for (int i=0; i<gameLumpCount; i++) 
			{
				bspgamelump gl=gameLump[i];
				
				switch(gl.id)
				{
				case SourceBSPStructs.GAMELUMP_STATIC_PROPS:
					Debug.Log ("Static props version is "+gl.version);
					if(gl.version<10)
						ParseStaticProps(gl);
					break;
				}
			}
		}
		
		void ParseStaticProps(bspgamelump gl)
		{
			int offset = gl.fileofs;
			br.BaseStream.Seek (offset, SeekOrigin.Begin);
			
			staticPropDictCount = br.ReadInt32 ();
			staticPropDict=new string[staticPropDictCount];
			for (int i=0; i<staticPropDictCount; i++) 
			{
				staticPropDict [i] = new string (br.ReadChars (128));
				if (staticPropDict [i].Contains ("\0"))
					staticPropDict [i] = staticPropDict [i].Remove (staticPropDict [i].IndexOf ('\0'));
			}
			
			staticPropLeafCount = br.ReadInt32 ();
			staticPropLeaves=new ushort[staticPropLeafCount];
			for(int i=0;i<staticPropLeafCount;i++)
				staticPropLeaves[i] = br.ReadUInt16();
			
			StaticPropCount = br.ReadInt32 ();
			StaticPropLump = ReadStaticProps (gl);
			staticPropsReaded = true;
		}
		
		bspStaticPropLump[] ReadStaticProps(bspgamelump gl)
		{
			bspStaticPropLump[] temp = new bspStaticPropLump[StaticPropCount];

			for (int i=0; i<StaticPropCount; i++) 
			{
				bspStaticPropLump prop=new bspStaticPropLump();
				if(gl.version>=4)
				{
					//4
					prop.Origin = ConvertUtils.FlipVector( ConvertUtils.ReadVector3(br))*uSrcSettings.Inst.worldScale;
					Vector3 ang = ConvertUtils.ReadVector3(br);
					prop.Angles = new Vector3(-ang.z, -ang.y, -ang.x);
					prop.PropType = (int)br.ReadUInt16();
					/*prop.FirstLeaf=br.ReadUInt16();
					prop.LeafCount=br.ReadUInt16();
					prop.Solid=br.ReadChar();
					prop.Flags=br.ReadChar();*/
					br.BaseStream.Seek(6,SeekOrigin.Current);
					prop.Skin=br.ReadInt32();
					prop.FadeMinDist=br.ReadSingle();
					prop.FadeMaxDist=br.ReadSingle();
					prop.LightingOrigin=ConvertUtils.ReadVector3(br);
				}
				if(gl.version>=5)
				{
					prop.ForcedFadeScale = br.ReadSingle();
					//br.BaseStream.Seek(12,SeekOrigin.Current);
				}
				if(gl.version>=6&gl.version<8)
				{
					prop.MinDXLevel = br.ReadUInt16();
					prop.MaxDXLevel = br.ReadUInt16();
				}
				if(gl.version>=8)
				{
					prop.minCPULevel=br.ReadByte();
					prop.maxCPULevel=br.ReadByte();
					prop.minGPULevel=br.ReadByte();
					prop.maxGPULevel=br.ReadByte();
					prop.diffuseModulation=new Color32(br.ReadByte(),br.ReadByte(),br.ReadByte(),br.ReadByte());
				}
				if(gl.version>=9)
				{
					//prop.DisableX360=br.ReadBoolean();
					br.BaseStream.Seek(4,SeekOrigin.Current);
				}

				temp[i]=prop;
			}
			
			return temp;
		}
		
		void readTexdataString ()
		{
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_TEXDATA_STRING_DATA].fileofs, SeekOrigin.Begin);
			int texdataStringDataLength = header.lumps [SourceBSPStructs.LUMP_TEXDATA_STRING_DATA].filelen;
			texdataStringDataLump = br.ReadChars (texdataStringDataLength);
			
			br.BaseStream.Seek (header.lumps [SourceBSPStructs.LUMP_TEXDATA_STRING_TABLE].fileofs, SeekOrigin.Begin);
			int texdataStringTableCount = header.lumps [SourceBSPStructs.LUMP_TEXDATA_STRING_TABLE].filelen / 4;
			texdataStringTableLump=new int[texdataStringTableCount];
			for(int i=0; i<texdataStringTableCount; i++)
			{
				texdataStringTableLump[i] = br.ReadInt32 ();
			}
			tempLog+= ("Load :"+texdataStringDataLength+" TexDataStringData \n");
			tempLog+= ("Load :"+texdataStringTableCount+" TexDataStringsTable \n");
		}

		public COLLADAExport.Geometry BSPToGeometry()
		{
			COLLADAExport.Geometry geom = new COLLADAExport.Geometry(LevelName+"-mesh", LevelName);
			COLLADAExport.Source src = new COLLADAExport.Source (LevelName + "-mesh-position");
			src.positionArray = vertexesLump;
			COLLADAExport.Vertices verts = new COLLADAExport.Vertices (LevelName + "-mesh-vertices");
			int[] p = calcTrisForModel(0);
			COLLADAExport.Triangles tris = new COLLADAExport.Triangles (LevelName + "-mesh-tris", p.Length / 3, "Mat1");
			tris.p = p;
			geom.mesh = new COLLADAExport.COLLADAMesh (new COLLADAExport.Source[]{src}, verts, new COLLADAExport.Triangles[]{tris});
			
			return geom;
		}

		int calcTrisCountForModel(int m)
		{
			int numTris = 0;
			for (int f = modelsLump[m].firstface; f<modelsLump[m].firstface+modelsLump[m].numfaces; f++) 
			{
				numTris += (facesLump[f].numedges-2)*3;
			}
			return numTris;
		}

		int calcVertsCountForModel(int m)
		{
			int numVerts = 0;
			for (int f = modelsLump[m].firstface; f<modelsLump[m].firstface+modelsLump[m].numfaces; f++) 
			{
				numVerts += facesLump[f].numedges;
			}
			return numVerts;
		}

		int[] calcTrisForModel(int m)
		{
			int[] tris = new int[calcTrisCountForModel(m)];
			//int[] vertsIds = new int[calcVertsCountForModel(m)];
			
			//int fircsFace = modelsLump [modelIndex].firstface;
			//int firstEdge = 0;
			//int numEdges = 0;

			int curTriangle = 0;
			for (int f = 0; f<modelsLump[m].numfaces; f++)
			{
				int firstEdge = facesLump[f].firstedge;
				int numEdges = facesLump[f].numedges;
				int[] faceTris=new int[numEdges];
				int curFaceTriangle=0;
				for(int e = firstEdge; e<firstEdge+numEdges; e++)
				{
					int edge = surfedgesLump[e];

					//tris[curTriangle] = edge>0 ?
					faceTris[curFaceTriangle] = edge>0 ?
						edgesLump[ edge][0]:
						edgesLump[-edge][1];
					curFaceTriangle++;
					//curTriangle++;
				}
				for(int i=0; i<numEdges-2; i++)
				{
					tris[curTriangle] = faceTris[0];
					curTriangle++;
					tris[curTriangle] = faceTris[i+1];
					curTriangle++;
					tris[curTriangle] = faceTris[i+2];
					curTriangle++;
				}
			}
			return tris;
		}

		int[] calcTrisForFace(int i)
		{
			int[] tris=null;
			int[] vertsIds=null;

			//int fircsFace = modelsLump [modelIndex].firstface;
			int startEdge = 0;
			int numEdges = 0;
			int numTris = 0;
			//for (int i = 0; i<modelsLump[modelIndex].numfaces; i++) 
			//{
				startEdge = facesLump[i].firstedge;
			Debug.Log ("First edge is "+startEdge);
				numEdges = facesLump[i].numedges;
				numTris = (numEdges-2)*3;
				vertsIds = new int[numEdges];
			tris = new int[numTris];
				for(int e = 0; e<numEdges; e++)
				{
				vertsIds[e] = surfedgesLump[startEdge+e]>0 ?
					edgesLump[Mathf.Abs (surfedgesLump[startEdge+e])][0]:
					edgesLump[Mathf.Abs (surfedgesLump[startEdge+e])][1];
				}

				for(int t = 0; t < numEdges - 2; t++)
				{
				tris[t*3] = vertsIds[0];
				tris[(t*3)+1] = vertsIds[t+1];
				tris[(t*3)+2] = vertsIds[t+2];
				}
			//}
			return tris;
		}
	}

}
