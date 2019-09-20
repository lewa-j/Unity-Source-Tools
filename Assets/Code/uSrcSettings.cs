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
				return inst ?? (inst = GameObject.FindGameObjectWithTag("WorldManager").GetComponent<uSrcSettings>());
			}
		}
		public bool extraDebugging = false;
		public string path = @"I:\uSource";
		public string assetsPath = @"I:\uSource";
		//public string TempPath = @"I:\Program Files/Half-Life 2";
		public string game = @"hl2";
		public bool haveMod = false;
		public string mod = @"portal";
		public float worldScale = 0.026f;

		public bool textures = true;
		public bool lightmaps = false;
		public bool displacements = false;
		public bool props = true;
		public bool propsDynamic = false;
		public bool entities = true;
		public bool showTriggers = false;
		public bool genColliders = false;

		//public Shader sDiffuse;
		public Material diffuseMaterial;
		//public Shader sTransparent;
		public Material transparentMaterial;
		//public Shader sAlphatest;
		public Material transparentCutout;
		public Shader sUnlit;
		public Shader sUnlitTransparent;
		public Material vertexLitMaterial;
		//public Shader sVertexLit;
		public Shader sSelfillum;
		public Shader sAdditive;
		public Shader sRefract;
		public Shader sWorldVertexTransition;

		/*public Shader Lmap;
		public Shader LmapAlpha;
		public Shader Sky;
		public Shader Solid;*/

		void Awake ()
		{
			inst = this;

			if (diffuseMaterial == null)
				//diffuseMaterial = Shader.Find ("Diffuse");
				print ("diffuse material is not there, please fix");
		}
	}
}
