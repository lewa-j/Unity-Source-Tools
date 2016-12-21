using UnityEngine;
using System.Collections;
using System.IO;

namespace uSrcTools
{

	public class ConvertUtils
	{
		public static Vector3 StringToVector(string input)
		{
			Vector3 origin = Vector3.zero;;
			string[] posStrings = input.Split(new char[]{' '});
			if(posStrings.Length<3)
				Debug.LogError("String "+input+" is not Vector3");
			try{
			origin = new Vector3(float.Parse(posStrings[0]),float.Parse(posStrings[2]),float.Parse(posStrings[1]))*uSrcSettings.Inst.worldScale;
			}
			catch(System.FormatException e)
			{
				Debug.LogError("StringToVector error "+e.Message);
				origin = Vector3.zero;
			}
			return origin;
		}

	public static Vector2 ReadVector2 (BinaryReader BR)
	{
		return new Vector2 (BR.ReadSingle(), BR.ReadSingle());
	}

	public static Vector3 ReadVector3 (BinaryReader BR)
	{
		return new Vector3 (BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
	}

	public static Vector4 ReadVector4 (BinaryReader BR)
	{
		return new Vector4 (BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
	}

	public static Quaternion ReadQuat (BinaryReader BR)
	{
		return new Quaternion (BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
	}
	
	public static void WriteVector3(BinaryWriter bw, Vector3 v)
	{
		bw.Write (v.x);
		bw.Write (v.y);
		bw.Write (v.z);
	}
	
	public static void WriteVector2(BinaryWriter bw, Vector2 v)
	{
		bw.Write (v.x);
		bw.Write (v.y);
	}

	public static string ReadNullTerminatedString(BinaryReader br, int offset)
	{
		br.BaseStream.Seek (offset, SeekOrigin.Begin);
		return ReadNullTerminatedString (br);
	}

	public static string ReadNullTerminatedString(BinaryReader br)
	{
		string str = "";
		while (true) 
		{
			char c=br.ReadChar();
			if(c=='\0')break;
			str+=c;
		}
		return str;
	}

	public static Vector3 FlipVector (Vector3 inp)
	{
		return new Vector3 (inp.x, inp.z, inp.y);
	}

	public static Color32 stringToColor(string input, byte alpha)
	{
		string[] colStrings = input.Trim().Split(' ');
		if(input =="0"||colStrings.Length==1)
			return new Color32(255,255,255,alpha);
		return new Color32((byte)short.Parse(colStrings[0]),(byte)short.Parse(colStrings[1]), (byte)short.Parse(colStrings[2]),alpha);
	}

	public static float[] Vector3ArrayToFloatArray(Vector3[] vecIn)
	{
		float[] fOut=new float[vecIn.Length*3];

		for(int i=0;i<vecIn.Length;i++)
		{
			fOut[i*3]=vecIn[i].x;
			fOut[(i*3)+1]=vecIn[i].y;
			fOut[(i*3)+2]=vecIn[i].z;
		}

		return fOut;
	}
}

}
