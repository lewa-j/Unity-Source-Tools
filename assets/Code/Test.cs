using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace uSrcTools
{

	public class Test : MonoBehaviour 
	{
		public static Test Inst;

		public SourceBSPLoader bsp;
		public SourceStudioModel model;
		public Material testMaterial;
		public Texture cameraTexture; 

		public string mapName;
		public string modelName;
		public bool skinnedModel=false;
		public Transform player;
		public Transform playerCamera;
		public Transform skyCamera;
		public Light light_environment;
		public Vector3 skyCameraOrigin;
		public Vector3 startPos;

		public bool loadMap = true;
		public bool loadModel= false;
		public bool exportMap = false;
		public bool isL4D2=false;

		void Awake()
		{
			Inst = this;
		}

		void Start () 
		{
			//player.transform.position = GameObject.Find ("info_player_start").transform.position;
		
			if (loadMap) 
			{
				if(bsp==null)
					bsp=GetComponent<SourceBSPLoader>();
			
				bsp.Load (mapName);
				if(exportMap)
				{
					COLLADAExport.Geometry g = bsp.map.BSPToGeometry();
					COLLADAExport.Export(@"I:\uSource\test\"+mapName+".dae",g,false,false);
				}
			}

			if(loadModel)
			{
				GameObject modelObj=new GameObject("TestModel");
				model.Load (@"models/"+modelName+".mdl");
				//model.GetInstance(modelObj,skinnedModel);
				model.GetInstance(modelObj,skinnedModel,0);
				//modelObj.transform.localEulerAngles=new Vector3(270,0,0);
			}
		}

		void Update()
		{
			//BSP.DrawDebugObjects (player.position);
		}

		void OnDrawGizmos()
		{
			if(model!=null)
				model.OnDrawGizmos ();
		}
	}

}