Shader "Simulation/UpdateWater" 
{
	Properties 
	{
    	_MainTex("WaterMap", 2D) = "black" { }
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
			
			sampler _MainTex;
			uniform sampler2D _FlowMap;
			uniform float _TexSize, _TimeStep, _Length;
		
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
			
			float frag(v2f i) : COLOR
			{
				// One texel
				float u = 1.0f/_TexSize;
				
				float field = tex2D(_MainTex, i.uv).x;
				
				float4 flow = tex2D(_FlowMap, i.uv);
				float4 flowL = tex2D(_FlowMap, i.uv + float2(-u, 0));
				float4 flowR = tex2D(_FlowMap, i.uv + float2(u, 0));
				float4 flowT = tex2D(_FlowMap, i.uv + float2(0, u));
				float4 flowB = tex2D(_FlowMap, i.uv + float2(0, -u));
								
				// left(x), right(y), top(z), bottom(w)
				float flowIn = flowL.y + flowR.x + flowT.w + flowB.z;
				float flowOut = flow.x + flow.y + flow.z + flow.w;
				
				// Volume change for this timestep
				float deltaVolume = _TimeStep * (flowIn - flowOut);
				
				// The water height is the previously calculated heightt plus the net volume change divided by length squared
				field = max(0, field + deltaVolume / (_Length*_Length));
				
				return field;

			}
			
			ENDCG

    	}
	}
}