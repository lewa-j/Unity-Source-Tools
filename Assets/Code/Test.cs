using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace uSrcTools
{
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(Test))]
    public class MapLoader : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Test script = (Test)target;
            if (GUILayout.Button("Load"))
            {
                script.Load();
            }
        }
    }
#endif

    public class Test : MonoBehaviour
		{
        private static Test inst;
        public static Test Inst
        {
            get
            {
                return inst ?? (inst = GameObject.FindGameObjectWithTag("WorldManager").GetComponent<Test>());
            }
        }

        public SourceBSPLoader bsp;
			public SourceStudioModel model;
			public Material testMaterial;
			public Texture cameraTexture;

			public string exportLocation = "D:\\uSource\\Example\\";
			public string mapName;
			public string modelName;
			public bool skinnedModel = false;
			public Transform player;
			public Transform playerCamera;
			public Transform skyCamera;
			public Light light_environment;
			public Vector3 skyCameraOrigin;
			public Vector3 startPos;

			public bool loadMap = true;
			public bool loadModel = false;
			public bool exportMap = false;
			public bool isL4D2 = false;
			public bool forceHDR = false;
			public bool skipSky = true;



        void Start()
        {
            Load();
        }

        public void Load()
        {
            //player.transform.position = GameObject.Find ("info_player_start").transform.position;

            if (loadMap)
            {
                if (bsp == null)
                    bsp = GetComponent<SourceBSPLoader>();

                bsp.Load(mapName);
                if (exportMap)
                {
                    COLLADAExport.Geometry g = bsp.map.BSPToGeometry();
                    print("Exporting map.");
                    //COLLADAExport.Export(@"I:\uSource\test\"+mapName+".dae",g,false,false);
                    COLLADAExport.Export(exportLocation + mapName + ".dae ", g, false, false);
                }
            }

            if (loadModel)
            {
                GameObject modelObj = new GameObject("TestModel ");
                model.Load(@"models / " + modelName + ".mdl ");
                //model.GetInstance(modelObj,skinnedModel);
                model.GetInstance(modelObj, skinnedModel, 0);
                //modelObj.transform.localEulerAngles=new Vector3(270,0,0);
            }
        }

		void Update ()
		{
			//BSP.DrawDebugObjects (player.position);
		}

		void OnDrawGizmos ()
		{
			if (model!=null)
				model.OnDrawGizmos ();
		}
	}

}