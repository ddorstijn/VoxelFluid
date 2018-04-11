// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Simulation/VisualizeWater"
{
	Properties
	{
		_MainTex("HeightMap", 2D) = "white" {}
		_ScaleY("Y scale", float) = 1.0
		_MinWaterHeight("Minimum Water Height", Float) = 0.1
		_FresnelFactor("FresnelFactor", Float) = 4.0
		_WaterAbsorption("WaterAbsorption", Vector) = (0.259, 0.086, 0.113, 2000.0)
		_DiffuseColor("Diffuse Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_SpecColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Shininess("Shininess", Float) = 0.1
	}

	SubShader
	{
		// Draw ourselves after all opaque geometry
		Tags { "Queue" = "Transparent" }

		// Grab the screen behind the object into _BackgroundTexture
		GrabPass
		{
			"_BackgroundTexture"
		}

		// Render the object with the texture generated above, and invert the colors
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
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
				float4 grabPos : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
				float4 pos : SV_POSITION;
			};

			uniform sampler2D _MainTex, _WaterMap, _CameraDepthTexture;
			uniform float _ScaleY, _TexSize, _MinWaterHeight;
			uniform float4 _MainTex_ST, _WaterAbsorption;

			uniform float4 _LightColor0, _DiffuseColor, _SpecColor;
			uniform float _Shininess, _FresnelFactor;

			v2f vert(appdata v)
			{
				v2f o;
				
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				v.tangent = float4(1, 0, 0, 1);

				// Add the terrain height and water height to the flat plane
				v.vertex.y += tex2Dlod(_MainTex, float4(v.uv, 0.0, 0.0)) * _ScaleY;
				v.vertex.y += tex2Dlod(_WaterMap, float4(v.uv, 0.0, 0.0)).x * _ScaleY;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				
				// Get the world position to calculate the normal and the screen position for the grabpass
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.grabPos = ComputeGrabScreenPos(o.pos);
				
				return o;
			}

			float3 FindNormal(float2 uv, float u)
			{
				// Compare all the heights and return the cross-product of the normalized direction vector
				float heightL = tex2D(_MainTex, uv + float2(-u, 0));
				float heightR = tex2D(_MainTex, uv + float2(u, 0));
				float heightB = tex2D(_MainTex, uv + float2(0, -u));
				float heightT = tex2D(_MainTex, uv + float2(0, u));

				heightL += tex2D(_WaterMap, uv + float2(-u, 0)).x;
				heightR += tex2D(_WaterMap, uv + float2(u, 0)).x;
				heightB += tex2D(_WaterMap, uv + float2(0, -u)).x;
				heightT += tex2D(_WaterMap, uv + float2(0, u)).x;

				float2 _step = float2(1.0, 0.0);

				float3 va = normalize(float3(_step.xy, heightR - heightL));
				float3 vb = normalize(float3(_step.yx, heightB - heightT));

				return cross(va, vb);
			}

			sampler2D _BackgroundTexture;

			float4 frag(v2f i) : SV_Target
			{
				// Get the waterheight and don't render it if it is below the threshold
				float height = tex2D(_WaterMap, i.uv).x;
				if (height < _MinWaterHeight) discard;

				float3 normal = FindNormal(i.uv, 1.0 / _TexSize);
				float3 cameraAngle = normalize(_WorldSpaceCameraPos - i.worldPos.xyz).xzy;

				// Change the opacity based on the viewing angle
				float fresnel = exp(-max(dot(cameraAngle, normal), 0.0) * _FresnelFactor);

				// This is some colorfilter  that is based on how water reflects light
				float3 Absorption = _WaterAbsorption.rgb * _WaterAbsorption.a;
				float3 bgcolor = tex2Dproj(_BackgroundTexture, i.grabPos);
				float3 col = bgcolor * exp(-Absorption * Absorption);

				// Get angle information for lighting 
				float3 lightDir = _WorldSpaceLightPos0.xyz - i.worldPos.xyz * _WorldSpaceLightPos0.w;
				float3 normalDir = normalize(normal);
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.pos.xyz);

				// Blinn-phong
				half3 halfDir = normalize(lightDir + viewDir);
				float specAngle = max(dot(halfDir, normalDir), 0.0);
				float specularAmount = pow(specAngle, 1.0 / _Shininess);
				float3 specularLight = _SpecColor.rgb * _LightColor0.rgb * specularAmount;

				// Normal dot LightAble
				float NdotL = max(0.0, dot(normal, lightDir));

				float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb* _DiffuseColor;
				float3 diffuse = _DiffuseColor * _LightColor0.rgb * NdotL;

				return float4(lerp(col, float4(0.0, 0.0, 1.0, 1.0), fresnel*0.4) + ambient + specularLight, 1.0);
			}
		
			ENDCG
		}
	}
}