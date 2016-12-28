using System;
using System.Collections.Generic;
using UnityEngine;

namespace uSrcTools
{
	public class SourceBSPStructs
	{
		public const int VBSP = 0x50534256;
		
		public const int GAMELUMP_STATIC_PROPS = 1936749168; // 'sprp';
		
		public const int HEADER_LUMPS 		= 64; 	//Num Lumps
		public const int LUMP_ENTITIES 		= 0; 	//Map entities
		public const int LUMP_PLANES 		= 1; 	//Plane array
		public const int LUMP_TEXDATA 		= 2; 	//Index to texture names
		public const int LUMP_VERTEXES 		= 3; 	//Vertex array
		public const int LUMP_VISIBILITY	= 4; 	//Compressed visibility bit arrays
		public const int LUMP_NODES 		= 5; 	//BSP tree nodes
		public const int LUMP_TEXINFO 		= 6; 	//Face texture array
		public const int LUMP_FACES			= 7; 	//Face array
		public const int LUMP_LIGHTING 		= 8; 	//Lightmap samples
		public const int LUMP_OCCLUSION 	= 9; 	//Occlusion polygons and vertices
		public const int LUMP_LEAFS 		= 10; 	//BSP tree leaf nodes
		public const int LUMP_FACEIDS 		= 11; 	//Correlates between dfaces and Hammer face IDs. Also used as random seed for detail prop placement.
		public const int LUMP_EDGES 		= 12; 	//Edge array
		public const int LUMP_SURFEDGES 	= 13; 	//Index of edges
		public const int LUMP_MODELS 		= 14; 	//Brush models (geometry of brush entities)
		public const int LUMP_WORLDLIGHTS	= 15;	//Internal world lights converted from the entity lump
		public const int LUMP_LEAFFACES 	= 16;	//Index to faces in each leaf
		public const int LUMP_LEAFBRUSHES	= 17;	//Index to brushes in each leaf
		public const int LUMP_BRUSHES 		= 18;	//Brush array
		public const int LUMP_BRUSHSIDES	= 19;	//Brushside array
		public const int LUMP_AREAS 		= 20;	//Area array
		public const int LUMP_AREAPORTALS	= 21;	//Portals between areas
		
		
		public const int LUMP_DISPINFO 		= 26; 	//Displacement surface array 
		
		
		public const int LUMP_DISP_LIGHTMAP_ALPHAS = 32; 	//Displacement lightmap alphas (unused/empty since Source 2006) 
		public const int LUMP_DISP_VERTS 	= 33;	//Vertices of displacement surface meshes 
		public const int LUMP_DISP_LIGHTMAP_SAMPLE_POSITIONS = 34;	//Displacement lightmap sample positions 
		public const int LUMP_GAME_LUMP 	= 35;	//Game-specific data lump 
		
		
		public const int LUMP_PAKFILE 		= 40; 	//Embedded uncompressed Zip-format file 
		
		public const int LUMP_CUBEMAPS			  = 42;	//env_cubemap location array
		public const int LUMP_TEXDATA_STRING_DATA = 43;	//Texture name data
		public const int LUMP_TEXDATA_STRING_TABLE= 44;	//Index array into texdata string data
		
		
		public const int LUMP_DISP_TRIS 	= 48;	//Displacement surface triangles 
		
		public const int LUMP_LIGHTING_HDR 	= 53;	//HDR lightmap samples 

		public const int LUMP_FACES_HDR		= 58; 	//HDR maps may have different face data


		public const int SURF_LIGHT	=	0x0001;		// value will hold the light strength
		public const int SURF_SKY2D	=	0x0002;		// don't draw, indicates we should skylight + draw 2d sky but not draw the 3D skybox
		public const int SURF_SKY	=	0x0004;		// don't draw, but add to skybox
		public const int SURF_WARP	=	0x0008;		// turbulent water warp
		public const int SURF_TRANS	=	0x0010;
		public const int SURF_NOPORTAL=	0x0020;	// the surface can not have a portal placed on it
		public const int SURF_TRIGGER=	0x0040;	// FIXME: This is an xbox hack to work around elimination of trigger surfaces, which breaks occluders
		public const int SURF_NODRAW=	0x0080;	// don't bother referencing the texture

		public const int SURF_HINT	=	0x0100;	// make a primary bsp splitter

		public const int SURF_SKIP	=	0x0200;	// completely ignore, allowing non-closed brushes
		public const int SURF_NOLIGHT=	0x0400;	// Don't calculate light
		public const int SURF_BUMPLIGHT=0x0800;	// calculate three lightmaps for the surface for bumpmapping
		public const int SURF_NOSHADOWS=0x1000;	// Don't receive shadows
		public const int SURF_NODECALS=	0x2000;	// Don't receive decals
		public const int SURF_NOCHOP =	0x4000;	// Don't subdivide patches on this surface 
		public const int SURF_HITBOX =	0x8000;	// surface is part of a hitbox
		

	}
	
	[Serializable]
	public struct bspheader
	{
		public int ident;
		public int version;
		public bsplump[] lumps;
		public int mapRevision;
	} //1036 bytes
	
	[Serializable]
	public struct bsplump
	{
		public int fileofs;
		public int filelen;
		public int version;
		public string fourCC; //4 chars
	}//16 bytes

	public struct bspplane
	{
		public Vector3 normal;
		public float dist;
		public int type;
	}//20 bytes

	//vertex - Vector3 //12 bytes

	//edge - ushort v[2] //4 bytes

	//surfedge - int //4 bytes

	[Serializable]
	public struct bspface
	{
		public ushort planenum;
		public byte	side;
		public byte onNode;
		public int firstedge;
		public short numedges;
		public short texinfo;
		public short dispinfo;
		public short surfaceFogVolumeID;
		public byte[] styles;//4
		public int lightofs;
		public float area;
		public int[] LightmapTextureMinsInLuxels;//2  	// texture lighting info
		public int[] LightmapTextureSizeInLuxels;//2 	// texture lighting info
		public int origFace;		// original face this was split from
		public ushort numPrims;		// primitives
		public ushort firstPrimID;
		public uint	smoothingGroups;	// lightmap smoothing group
	} //56 bytes

	[Serializable]
	public struct bspbrush
	{
		public int	firstside;	// first brushside
		public int	numsides;	// number of brushsides
		public int	contents;	// contents flags
	}//12 byte

	[Serializable]
	public struct bspbrushside
	{
		public ushort planenum;	// facing out of the leaf
		public short texinfo;	// texture info
		public short dispinfo;	// displacement info
		public short bevel;		// is the side a bevel plane?
	}//8 byte

	[Serializable]
	public struct bspnode
	{
		public int planenum;		// index into plane array
		public int[] children;//2	// negative numbers are -(leafs + 1), not nodes
		public short[] mins;//3		// for frustum culling
		public short[] maxs;//3
		public ushort firstface;	// index into face array
		public ushort numfaces;		// counting both sides
		public short area;			// If all leaves below this node are in the same area, then// this is the area index. If not, this is -1.
		public short paddding;		// pad to 32 bytes length
	}//32 bytes

	[Serializable]
	public struct bspleaf
	{
		public int contents;		// OR of all brushes (not needed?)
		public short cluster;		// cluster this leaf is in
		
		//public short area;//:9;???			// area this leaf is in
		//public short flags;//:7;???		// flags
		public byte area;
		public byte flags;
		
		public short[] mins;//3		// for frustum culling
		public short[] maxs;//3
		
		public ushort firstleafface;		// index into leaffaces
		public ushort numleaffaces;
		public ushort firstleafbrush;		// index into leafbrushes
		public ushort numleafbrushes;
		public short leafWaterDataID;	// -1 for not in water
		
		//skip 2 byte
		
		//!!! NOTE: for maps of version 19 or lower uncomment this block
		/*
			CompressedLightCube	ambientLighting;	// Precaculated light info for entities.
			short			padding;		// padding to 4-byte boundary
			*/
	}//56 byte?????
	//32 byte!!!!!


	//leafFace - ushort

	//leadBrush - ushort

	[Serializable]
	public struct bsptexinfo
	{
		public Vector3 texvecs;
		public float texoffs;
		public Vector3 texvect;
		public float texofft;
		
		public Vector3 lightvecs;
		public float lightoffs;
		public Vector3 lightvect;
		public float lightofft;
		
		public int flags;
		public int texdata;
	}//72 bytes

	//u = tv0,0 * x + tv0,1 * y + tv0,2 * z + tv0,3
	//v = tv1,0 * x + tv1,1 * y + tv1,2 * z + tv1,3
	//(ie. The dot product of the vectors with the vertex plus the offset in that direction. Where tvA,B is textureVecs[A][B].

	[Serializable]
	public class bsptexdata
	{
		public Vector3	reflectivity;		// RGB reflectivity
		public int	nameStringTableID;	// index into TexdataStringTable
		public int	width;
		public int	height;		// source image
		public int	view_width;
		public int  view_height;

		public int numVerts;
		public int numInds;
	}//32 bytes

	[Serializable]
	public struct bspmodel
	{
		public Vector3 mins;
		public Vector3 maxs;
		public Vector3 origin;
		public int headnode;
		public int firstface;
		public int numfaces;
	}//48 bytes

	public struct bspvis
	{
		public int numclusters;
		public int[][] byteofs;//numclusters  2
	}

	public struct bspcubemapsample
	{
		public int[] origin;//3
		public int size;
	}

	public struct RGBExp32
	{
		public byte r, g, b;
		public sbyte exp;
	}

	[Serializable]
	public struct bspdispinfo
	{
		public Vector3		startPosition;		// start position used for orientation
		public int			DispVertStart;		// Index into LUMP_DISP_VERTS.
		public int			DispTriStart;		// Index into LUMP_DISP_TRIS.
		public int			power;			// power - indicates size of surface (2^power	1)
		public int			minTess;		// minimum tesselation allowed
		public float		smoothingAngle;		// lighting smoothing angle
		public int			contents;		// surface contents
		public /*ushort*/uint		MapFace;		// Which map face this displacement comes from.
		public int			LightmapAlphaStart;	// Index into ddisplightmapalpha.
		public int			LightmapSamplePositionStart;	// Index into LUMP_DISP_LIGHTMAP_SAMPLE_POSITIONS.
		//46b
		public DisplaceNeighbor[]		EdgeNeighbors;	//[4]  Indexed by NEIGHBOREDGE_ defines.  //40b
		public DisplaceCornerNeighbors[]	CornerNeighbors;	//[4]  Indexed by CORNER_ defines. //36b
		//122
		public uint[]		AllowedVerts;	//[10]  active verticies //40
	}//162b

	public struct DisplaceNeighbor
	{
		public DisplaceSubNeighbor[] m_SubNeighbors; //[2]
	}//10b

	public struct DisplaceSubNeighbor
	{
		public ushort	Neighbor_index;		// This indexes into ddispinfos.
												// 0xFFFF if there is no neighbor here.
		
		public char		neighbor_orient;		// (CCW) rotation of the neighbor wrt this displacement.
		
		// These use the NeighborSpan type.
		public char		local_span;						// Where the neighbor fits onto this side of our displacement.
		public char		neighbor_span;				// Where we fit onto our neighbor.
	}//5b

	public class DisplaceCornerNeighbors
	{
		public ushort[]	neighbor_indices;//[MAX_DISP_CORNER_NEIGHBORS] 4	  indices of neighbors.
		public byte	neighbor_count;
	}//9b

	[Serializable]
	public struct bspDispVert
	{
		public Vector3	vec;	// Vector field defining displacement volume.
		public float	dist;	// Displacement distances.
		public float	alpha;	// "per vertex" alpha values.
	}




	[Serializable]
	public struct bspgamelump
	{
		public int		id;		// gamelump ID
		public ushort	flags;		// flags
		public ushort	version;	// gamelump version
		public int		fileofs;	// offset to this gamelump
		public int		filelen;	// length
	}

	[Serializable]
	public struct bspStaticPropLump
	{
		// v4
		public Vector3		Origin;		 // origin
		public Vector3		Angles;		 // orientation (pitch roll yaw)
		public int	PropType;	 // index into model name dictionary
		public ushort	FirstLeaf;	 // index into leaf array
		public ushort	LeafCount;
		public char	Solid;		 // solidity type
		public char	Flags;
		public int		Skin;		 // model skin numbers
		public float		FadeMinDist;
		public float		FadeMaxDist;
		public Vector3		LightingOrigin;  // for lighting
		// since v5
		public float		ForcedFadeScale; // fade distance scale
		// v6 and v7 only
		public ushort  MinDXLevel;      // minimum DirectX version to be visible
		public ushort  MaxDXLevel;      // maximum DirectX version to be visible

		// since v8
		public byte   minCPULevel;
		public byte   maxCPULevel;
		public byte   minGPULevel;
		public byte   maxGPULevel;
		public Color32 diffuseModulation;
		/*// since v7
		public Color32         DiffuseModulation; // per instance color and alpha modulation
		// since v10
		public float           unknown; 
		*/
		// since v9
		public bool            DisableX360;     // if true, don't show on XBox 360
		//3 unknown bytes
	}
}
