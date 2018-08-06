Shader "uSrcTools/WorldVertexTransition" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_MainTex2 ("Base 2 (RGB)", 2D) = "white" {}
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _MainTex2;

		struct Input 
		{
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			//todo: seems to be broken? check this out
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			half4 c2 = tex2D (_MainTex2, IN.uv_MainTex);
			float alpha=IN.color.x;
			o.Albedo = lerp(c.rgb,c2.rgb,alpha);
			o.Alpha = lerp(c.a,c2.a,alpha);
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
