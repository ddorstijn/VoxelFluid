Shader "Simulation/VisualizeTerrain"
{
	Properties
	{
		_MainTex ("HeightMap", 2D) = "white" {}
		_ScaleY("Y scale", float) = 1.0
		_DiffuseColor("Diffuse Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_SpecColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Shininess("Shininess", float) = 0.1
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode" = "ForwardBase"}
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
				float4 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			uniform float _ScaleY, _Shininess, _TexSize;

			uniform float4 _LightColor0;
			uniform float4 _DiffuseColor, _MainTex_ST, _SpecColor;
			
			v2f vert (appdata v)
			{
				v2f o;

				v.tangent = float4(1, 0, 0, 1);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				v.vertex.y += tex2Dlod(_MainTex, float4(v.uv, 0.0, 0.0)) * _ScaleY;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);

				return o;
			}

			float3 FindNormal(float2 uv, float u)
			{
				float heightL = tex2D(_MainTex, uv + float2(-u, 0));
				float heightR = tex2D(_MainTex, uv + float2(u, 0));
				float heightB = tex2D(_MainTex, uv + float2(0, -u));
				float heightT = tex2D(_MainTex, uv + float2(0, u));

				float2 _step = float2(1.0, 0.0);

				float3 va = normalize(float3(_step.xy, heightR - heightL));
				float3 vb = normalize(float3(_step.yx, heightB - heightT));

				return cross(va, vb);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 normal = FindNormal(i.uv, 1.0 / _TexSize);
				float3 lightDir = _WorldSpaceLightPos0.xyz - i.worldPos.xyz * _WorldSpaceLightPos0.w;
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);

				// Blinn-phong
				half3 halfDir = normalize(lightDir + viewDir);
				float specAngle = max(dot(halfDir, normal), 0.0);
				float specularAmount = pow(specAngle, 1.0 / _Shininess);
				float3 specularLight = _SpecColor.rgb * _LightColor0.rgb * specularAmount;

				float NdotL = max(0.0, dot(normal, lightDir));

				float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb* _DiffuseColor;
				float3 diffuse = _DiffuseColor * _LightColor0.rgb * NdotL;

				return float4(diffuse + ambient + specularLight, 1.0);
			}
			ENDCG
		}
	}
}
