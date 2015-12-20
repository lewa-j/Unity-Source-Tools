using System;
using UnityEngine;

class SourceMDLStructs
{
	public const int MAX_NUM_BONES_PER_VERT = 3;
}

[Serializable]
public struct studiohdr_t
{
	public char[] id;//4
	public int version;
	
	public int checksum; // this has to be the same in the phy and vtx files to load!
	
	public string Name;//64
	public int length; // Data size of MDL file in bytes.
	
	public Vector3 eyeposition;
	public Vector3 illumposition;
	public Vector3 hullmin;
	public Vector3 hullmax;
	public Vector3 view_bbmin;
	public Vector3 view_bbmax;
	
	public int flags;
	
	public int bonesnum;
	public int boneoffs;
	
	public int bonecontrollernum;
	public int bonecontrolleroffs;
	
	public int hitboxnum;
	public int hitboxoffs;
	
	public int localanimnum;
	public int localanimoffs;
	
	public int localseqnum;
	public int localseqoffs;
	
	public int activitylistversion;
	public int eventsindexed;
	
	public int texturenum;
	public int textureoffs;
	
	public int texturedirnum;
	public int texturediroffs;
	
	public int skinrefnum;
	public int skinfamilynum;
	public int skinfamilyoffs;
	
	public int bodypartnum;
	public int bodypartoffs;
	
	public int attachmentnum;
	public int attachmentoffs;
	
	public int localnodenum;
	public int localnodeoffs;
	public int localnodenameoffs;
	
	public int flexdescnum;
	public int flexdescoffs;
	
	public int flexcontrollernum;
	public int flexcontrolleroffs;
	
	public int flexrulesnum;
	public int flexrulesoffs;
	
	public int ikchainnum;
	public int ikchainoffs;
	
	public int mouthsnum;
	public int mouthsoffs;
	
	public int localposeparamnum;
	public int localposeparamoffs;
	
	public int surfacepropoffs;
	
	public int keyvalueoffs;
	public int keyvaluenum;
	
	public int iklocknum;
	public int iklockoffs;
	
	public float mass;
	public int contents;
	
	public int includemodelnum;
	public int includemodeloffs;
	
	public int virtualModel;
	
	public int animblocksnameoffs;
	public int animblocksnum;
	public int animblocksoffs;
	
	public int animblockModel;
	
	public int bonetablenameoffs;
	
	public int vertex_base;
	public int offset_base;
	
	public byte directionaldotproduct;
	
	public byte rootLod;
	
	public byte numAllowedRootLods;
	
	public byte unused;
	
	public int unused1;
	
	public int flexcontrolleruinum;
	public int flexcontrolleruioffs;
	
	public long unused2;
	
	public int studiohdr2offs;
	
	public int unused3;
}

struct studiohdr2_t
{
	public int srcbonetransformnum;
	public int srcbonetransformoffs;
	
	public int illumpositionattachmentindex;
	
	public float flMaxEyeDeflection;
	
	public int linearbone_index;
	
	public int[] unknown;//64
}

[Serializable]
public struct studiobone_t
{
	public string pszName;
	public int			sznameindex;
	//inline char * const pszName( void ) const { return ((char *)this) + sznameindex; }
	public int		 	parent;		// parent bone
	public int[]		bonecontroller;//[6];	// bone controller index, -1 == none
	
	// default values
	public Vector3		pos;
	public Quaternion	quat;
	public /*RadianEuler*/Vector3	rot;
	// compression scale
	public Vector3		posscale;
	public Vector3		rotscale;
	
	public /*matrix3x4_t*/float[]	poseToBone; //12
	public Quaternion	qAlignment;
	public int			flags;
	public int			proctype;
	public int			procindex;		// procedural rule
	public /*mutable*/ int	physicsbone;	// index into physically simulated bone
	//inline void *pProcedure( ) const { if (procindex == 0) return NULL; else return  (void *)(((byte *)this) + procindex); };
	public int			surfacepropidx;	// index into string tablefor property name
	//inline char * const pszSurfaceProp( void ) const { return ((char *)this) + surfacepropidx; }
	public int			contents;		// See BSPFlags.h for the contents flags
	
	//public int[]		unused;//[8];		// remove as appropriate
	
	//public mstudiobone_t(){}
};

[Serializable]
public struct studiobodypart
{
	public string name;
	//
	public int nameindex;
	public int nummodels;
	public int base_;
	public int modelindex;

	public studiomodel[] models;
	
	public studiobodypart(int nameindex,int nummodels,int base_,int modelindex)
	{
		this.nameindex=nameindex;
		this.nummodels=nummodels;
		this.base_=base_;
		this.modelindex = modelindex;
		
		this.models = new studiomodel[nummodels];
		this.name = "";
	}
}

[Serializable]
public struct studiomodel //148 byte
{
	public string name; //64
	public int type;
	public float boundingradius;
	public int meshnum;
	public int meshoffs;
	public int verticesnum;
	public int verticesoffs;
	public int tangentsoffs;
	public int attachmentnum;
	public int attachmentoffs;
	public int eyeballnum;
	public int eyeballoffs;
	public mstudio_modelvertexdata_t vertexdata;
	//skip 32
	
	//
	public studiomesh[] meshes;
	//
	
	public studiomodel( string name,int type,float boundingradius,int meshnum,int meshoffs,
	                   int verticesnum,int verticesoffs,int tangentsoffs,int attachmentnum,
	                   int attachmentoffs,int eyeballnum,int eyeballoffs,mstudio_modelvertexdata_t vd)
	{
		this.name=name;
		this.type=type;
		this.boundingradius=boundingradius;
		this.meshnum = meshnum;
		this.meshoffs=meshoffs;
		this.verticesnum=verticesnum;
		this.verticesoffs=verticesoffs;
		this.tangentsoffs=tangentsoffs;
		this.attachmentnum=attachmentnum;
		this.attachmentoffs=attachmentoffs;
		this.eyeballnum=eyeballnum;
		this.eyeballoffs=eyeballoffs;
		this.vertexdata = vd;
		
		this.meshes = new studiomesh[meshnum];
	}
}

public struct mstudio_modelvertexdata_t
{
	//Vector				*Position( int i ) const;
	//Vector				*Normal( int i ) const;
	//Vector4D			*TangentS( int i ) const;
	//Vector2D			*Texcoord( int i ) const;
	//mstudioboneweight_t	*BoneWeights( int i ) const;
	//mstudiovertex_t		*Vertex( int i ) const;
	//bool				HasTangentData( void ) const;
	//int					GetGlobalVertexIndex( int i ) const;
	//int					GetGlobalTangentIndex( int i ) const;
	
	// base of external vertex data stores
	//const void			*pVertexData;
	//const void			*pTangentData;
	public int pVertexData;
	public int pTangentData;

	public mstudio_modelvertexdata_t(int vd,int td)
	{
		pVertexData = vd;
		pTangentData = td;
	}
}

[Serializable]
public struct studiomesh
{
	public int material;
	
	public int modelindex;
	
	public int vertexnum;		// number of unique vertices/normals/texcoords
	public int vertexoffs;		// vertex mstudiovertex_t
	
	public int flexenum;			// vertex animation
	public int flexoffs;
	
	// special codes for material operations
	public int materialtype;
	public int materialparam;
	
	// a unique ordinal for this mesh
	public int meshid;
	
	public Vector3 center;
	
	public studio_meshvertexdata vertexdata;
	
	//public int[] unused;//8 // remove as appropriate 
	//32 skip
	
	public studiomesh(int material, int modelindex, int vertexnum, int vertexoffs,
	                  int flexenum, int flexoffs, int materialtype, int materialparam,
	                  int meshid, Vector3 center, studio_meshvertexdata vertexdata)
	{
		this.material=material;
		this.modelindex=modelindex;
		this.vertexnum=vertexnum;		// number of unique vertices/normals/texcoords
		this.vertexoffs=vertexoffs;		// vertex mstudiovertex_t
		this.flexenum=flexenum;			// vertex animation
		this.flexoffs=flexoffs;
		this.materialtype=materialtype;
		this.materialparam=materialparam;
		this.meshid=meshid;
		this.center=center;
		this.vertexdata=vertexdata;
	}
}

[Serializable]
public struct studio_meshvertexdata
{
	public int skip; //4
	public int[] numLODVertexes;//8
	
	public studio_meshvertexdata(int skip, int[] numLODVertices)
	{
		this.skip=skip;
		this.numLODVertexes = numLODVertices;
	}
}

[Serializable]
public struct studiotexture_t
{
	public string textureName;
	//

	public int name_offset;
	public int flags;
	public int used;
	
	public int unused;
	
	public int material;
	public int client_material;
	
	public int[] unused2;//10
}//64

public struct studioboneweight
{
	public float[] weight; //3
	public byte[] bone; //3
	public byte numbones;
	
	//	byte	material;
	//	short	firstref;
	//	short	lastref;
}

struct studiovertex
{
	public studioboneweight BoneWeights;
	public Vector3 vecPosition;
	public Vector3 vecNormal;
	public Vector2 vecTexCoord;
}

struct studioiface
{
	public ushort a,b,c;// Indices to vertices
}

//=======================================================================
//=======================VVD=============================================
//=======================================================================

[Serializable]
public struct vvdheader
{
	public char[] id;//4
	public int version;
	public int checksum;
	public int numLODs;
	public int[] numLODVertexes;//8
	public int numFixup;
	public int fixupTableStart;
	public int vertexDataStart;
	public int tangentDataStart;
}

public struct vertexFileFixup
{
	public int		lod;				// used to skip culled root lod
	public int		sourceVertexID;		// absolute index from start of vertex/tangent blocks
	public int		numVertexes;

	public vertexFileFixup(int l, int s, int n)
	{
		lod = l;
		sourceVertexID = s;
		numVertexes = n;
	}
}

//===========================================================================
//===========================VTX=============================================
//===========================================================================

[Serializable]
public struct vtxHeader
{
	public int version;
	public int vertCacheSize;
	public short maxBonePerStrip;
	public short maxBonePerTri;
	public int maxBonePerVert;
	public int checksum;
	public int numLODs;
	public int materialReplacementListOffset;
	public int numBobyParts;
	public int bodyPartOffset;
}

[Serializable]
public struct vtxBodypartHeader
{
	public int numModels;
	public int modelsOffset;

	//
	public vtxModelHeader[] models;
	
	public vtxBodypartHeader(int numModels, int modelsOffset)
	{
		this.numModels=numModels;
		this.modelsOffset=modelsOffset;
		
		this.models=new vtxModelHeader[numModels];
	}
}

[Serializable]
public struct vtxModelHeader
{
	public int numLODs;
	public int LODOffset;

	//
	public vtxModelLODheader[] lods;
	
	public vtxModelHeader(int numLODs, int LODOffset)
	{
		this.numLODs=numLODs;
		this.LODOffset=LODOffset;
		
		this.lods=new vtxModelLODheader[numLODs];
	}
}

[Serializable]
public struct vtxModelLODheader
{
	public int numMeshes;
	public int meshOffset;
	public float switchPoint;

	//
	public vtxMeshHeader[] meshes;
	
	public vtxModelLODheader(int numMeshes, int meshOffset, float switchPoint)
	{
		this.numMeshes = numMeshes;
		this.meshOffset = meshOffset;
		this.switchPoint = switchPoint;
		this.meshes = new vtxMeshHeader[numMeshes];
	}
}

[Serializable]
public struct vtxMeshHeader
{
	public int numStripGroups;
	public int stripGroupHeaderOffset;
	public byte flags;

	//
	public vtxStripGroupHeader[] stripGroups;
	
	public vtxMeshHeader(int numStripGroups, int stripGroupHeaderOffset, byte flags)
	{
		this.numStripGroups = numStripGroups;
		this.stripGroupHeaderOffset = stripGroupHeaderOffset;
		this.flags = flags;
		this.stripGroups = new vtxStripGroupHeader[numStripGroups];
	}
}

[Serializable]
public struct vtxStripGroupHeader
{
	public int numVerts;
	public int vertOffset;
	public int numIndices;
	public int indexOffs;
	public int numStrips;
	public int stripOffset;
	public byte flags;
	
	public vtxVertex[] verts;
	public ushort[] indexArray;
	public vtxStripHeader[] strips;
	
	public vtxStripGroupHeader(int numVerts, int vertOffset, int numIndices,int indexOffs,
	                        int numStrips,int stripOffset,byte flags)
	{
		this.numVerts = numVerts;
		this.vertOffset = vertOffset;
		this.numIndices = numIndices;
		this.indexOffs = indexOffs;
		this.numStrips = numStrips;
		this.flags = flags;
		this.stripOffset = stripOffset;

		this.verts = new vtxVertex[numVerts];
		this.indexArray = new ushort[numIndices];
		this.strips = new vtxStripHeader[numStrips];
	}
}

[Serializable]
public struct vtxVertex
{
	public byte[] boneWeightIndex;//3
	public byte numBones;
	public int origMeshVertID;
	public byte[] boneID;//3
	
	public vtxVertex(byte[] boneWeightIndex, byte numBones,
	                 ushort origMeshVertID, byte[] boneID)
	{
		this.boneWeightIndex = boneWeightIndex;
		this.numBones = numBones;
		this.origMeshVertID = (int)origMeshVertID;
		this.boneID = boneID;
	}
}

[Serializable]
public struct vtxStripHeader
{
	public int numIndices;
	public int indexOffset;
	public int numVerts;
	public int vertOffset;
	public short numBones;
	public byte flags;
	public int numBoneStateChange;
	public int boneStateChangeOffset;
	
	public vtxStripHeader(int numIndices, int indexOffset,int numVerts, int vertOffset,
	                   short numBones, byte flags, int numBoneStateChange, int boneStateChangeOffset)
	{
		this.numIndices = numIndices;
		this.indexOffset = indexOffset;
		this.numVerts = numVerts;
		this.vertOffset = vertOffset;
		this.numBones = numBones;
		this.flags = flags;
		this.numBoneStateChange = numBoneStateChange;
		this.boneStateChangeOffset = boneStateChangeOffset;
	}
}
