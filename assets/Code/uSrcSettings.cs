using UnityEngine;

namespace uSrcTools
{
	public class uSrcSettings : MonoBehaviour
	{
		private static uSrcSettings inst;
		public static uSrcSettings Inst
		{
			get
			{
				return inst??(inst=new GameObject("uSrcSettings").AddComponent<uSrcSettings>());
			}
		}

		public string path = @"I:\uSource";
		//public string TempPath = @"I:\Program Files/Half-Life 2";
		public string game = @"hl2";
		public bool haveMod=false;
		public string mod = @"portal";
		public float worldScale = 0.026f;
		
		public bool textures = true;
		public bool lightmaps = false;
		public bool displacements = false;
		public bool props = true;
		public bool entities = true;
		public bool showTriggers = false;
		public bool genColliders=false;
		
		public Shader sDiffuse;
		public Shader sTransparent;
		public Shader sAlphatest;
		public Shader sUnlit;
		public Shader sUnlitTransparent;
		public Shader sVertexLit;
		public Shader sSelfillum;
		public Shader sAdditive;
		public Shader sRefract;
		public Shader sWorldVertexTransition;

		/*public Shader Lmap;
		public Shader LmapAlpha;
		public Shader Sky;
		public Shader Solid;*/

		void Awake()
		{
			inst = this;
			
			if(sDiffuse==null)
				sDiffuse=Shader.Find("Diffuse");
		}
	}
}