Shader "Simulation/UpdateFlow" 
{
	Properties 
	{
		_MainTex ("FlowMap", 2D) = "white" {}
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
			uniform sampler2D _TerrainMap, _WaterMap;
			uniform float _TexSize, _TimeStep, _Length, _Area, _Gravity, _Viscosity;
		
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
			
			float4 frag(v2f i) : COLOR
			{
				// One texel
				float u = 1.0f/_TexSize;
				
			    // Get the terrain height of the cell and the neighbouring cells
			    // Only 4 direction because that is easier to fit into one texture
				float height = tex2D(_TerrainMap, i.uv);
				float heightL = tex2D(_TerrainMap, i.uv + float2(-u, 0));
				float heightR = tex2D(_TerrainMap, i.uv + float2(u, 0));
				float heightT = tex2D(_TerrainMap, i.uv + float2(0, u));
				float heightB = tex2D(_TerrainMap, i.uv + float2(0, -u));

				// Get the water height of the cell and the neighbouring cells
				float waterHeight = tex2D(_WaterMap, i.uv).x;
				float waterHeightL = tex2D(_WaterMap, i.uv + float2(-u, 0)).x;
				float waterHeightR = tex2D(_WaterMap, i.uv + float2(u, 0)).x;
				float waterHeightT = tex2D(_WaterMap, i.uv + float2(0, u)).x;
				float waterHeightB = tex2D(_WaterMap, i.uv + float2(0, -u)).x;
				
				// Get the flow from the previous texture slightly damped to calculate the new flow later
				float4 flow = tex2D(_MainTex, i.uv) * _Viscosity;
				
				height += waterHeight;
				
				// The height difference between this cell and the other cell. Done for all directions
				float deltaHeightL = height - heightL - waterHeightL;
				float deltaHeightR = height - heightR - waterHeightR;
				float deltaHeightT = height - heightT - waterHeightT;
				float deltaHeightB = height - heightB - waterHeightB;
				
				// flowPrev + deltaTime * area * ((gravity * deltaHeight) / length)
				//max 0, no neg values
				//left(x), right(y), top(z), bottom(w)
				float flowL = max(0.0, flow.x + _TimeStep * _Area * ((_Gravity * deltaHeightL) / _Length));
				float flowR = max(0.0, flow.y + _TimeStep * _Area * ((_Gravity * deltaHeightR) / _Length));
				float flowT = max(0.0, flow.z + _TimeStep * _Area * ((_Gravity * deltaHeightT) / _Length));
				float flowB = max(0.0, flow.w + _TimeStep * _Area * ((_Gravity * deltaHeightB) / _Length));
							
				// Scale by the flow by K to maintain a positive height 				
				float K = min(1.0, (waterHeight * _Length*_Length) / ((flowL + flowR + flowT + flowB) * _TimeStep));
				
				return float4(flowL, flowR, flowT, flowB) * K;

			}
			
			ENDCG

    	}
	}
}