Shader "Simulation/WaterInput" 
{
	Properties 
	{
		_MainTex ("WaterMap (RGB)", 2D) = "white" {}
	}
	SubShader 
	{
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			uniform float2 _InputUV;
			uniform float _Radius, _Amount;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f o;
    			o.pos = UnityObjectToClipPos(v.vertex);
    			o.uv = v.texcoord.xy;
    			return o;
			}
			
			float GetGaussFactor(float2 diff, float rad2) 
			{
				return exp(-(diff.x*diff.x+diff.y*diff.y)/rad2);
			}
			
			float4 frag(v2f i) : COLOR
			{
				// Gaussian blur for soft transition from input point to outside radius
				float gauss = GetGaussFactor(_InputUV - i.uv, _Radius*_Radius);
				
				float waterAmount = gauss * _Amount;
				
				return tex2D(_MainTex, i.uv) + float4(waterAmount,0,0,0);
			}
			
			ENDCG

    	}
	}
}