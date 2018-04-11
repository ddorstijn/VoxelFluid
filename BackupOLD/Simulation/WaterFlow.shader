Shader "Simulation/Flow" {
	Properties {
		_MainTex("FlowMap (RGB)", 2D) = "white" {}
	}
	SubShader {
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
			uniform float _TexSize, _TimeStep, _Length, _Area, _Gravity;

			struct v2f
			{
				float4  pos : SV_POSITION;
				float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
				OUT.uv = v.texcoord.xy;
				return OUT;
			}

			float4 frag(v2f IN) : COLOR
			{
				float u = 1.0f / _TexSize;

				float ht = tex2D(_TerrainMap, IN.uv);
				float htL = tex2D(_TerrainMap, IN.uv + float2(-u, 0));
				float htR = tex2D(_TerrainMap, IN.uv + float2(u, 0));
				float htT = tex2D(_TerrainMap, IN.uv + float2(0, u));
				float htB = tex2D(_TerrainMap, IN.uv + float2(0, -u));

				float field = tex2D(_WaterMap, IN.uv).x;
				float fieldL = tex2D(_WaterMap, IN.uv + float2(-u, 0)).x;
				float fieldR = tex2D(_WaterMap, IN.uv + float2(u, 0)).x;
				float fieldT = tex2D(_WaterMap, IN.uv + float2(0, u)).x;
				float fieldB = tex2D(_WaterMap, IN.uv + float2(0, -u)).x;

				float4 flow = tex2D(_MainTex, IN.uv);

				ht += field;

				//deltaHX is the height diff between this cell and neighbour X
				float deltaHL = ht - htL - fieldL;
				float deltaHR = ht - htR - fieldR;
				float deltaHT = ht - htT - fieldT;
				float deltaHB = ht - htB - fieldB;

				//new flux value is old value + delta time * area * ((gravity * delta ht) / length)
				//max 0, no neg values
				//left(x), right(y), top(z), bottom(w)
				float flowL = max(0.0, flow.x + _TimeStep * _Area * ((_Gravity * deltaHL) / _Length));
				float flowR = max(0.0, flow.y + _TimeStep * _Area * ((_Gravity * deltaHR) / _Length));
				float flowT = max(0.0, flow.z + _TimeStep * _Area * ((_Gravity * deltaHT) / _Length));
				float flowB = max(0.0, flow.w + _TimeStep * _Area * ((_Gravity * deltaHB) / _Length));

				//If the sum of the outflow flux exceeds the water amount of the
				//cell, flux value will be scaled down by a factor K to avoid negative
				//updated water height
				float K = min(1.0, (field * _Length*_Length) / ((flowL + flowR + flowT + flowB) * _TimeStep));

				return float4(flowL, flowR, flowT, flowB) * K;
			}

			ENDCG
		}
	}
}