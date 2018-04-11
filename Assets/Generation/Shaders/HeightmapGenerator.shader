Shader "Hidden/HeightmapGenerator"
{
	Properties
	{
		_MainTex ("HeightMap", 2D) = "white" {}

        _X ("Seed X", float) = 1.0
        _Y ("Seed Y", float) = 1.0

        _Octaves ("Octaves", int) = 3
        _Gain ("Gain", float) = 1.0
        _Lacunarity ("Lacunarity", float) = 1.0

		_Height ("Mesh Height", float) = 1.0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			// 1 / 289
			#define NOISE_SIMPLEX_1_DIV_289 0.00346020761245674740484429065744f

			float mod289(float x)
			{
				return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
			}

			float2 mod289(float2 x)
			{
				return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
			}

			float3 mod289(float3 x)
			{
				return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
			}


			// ( x*34.0 + 1.0 )*x = 
			// x*x*34.0 + x
			float3 permute(float3 x)
			{
				return mod289(
					x*x*34.0 + x
				);
			}

			float snoise(float2 v)
			{
				const float4 C = float4(
					0.211324865405187, // (3.0-sqrt(3.0))/6.0
					0.366025403784439, // 0.5*(sqrt(3.0)-1.0)
					-0.577350269189626, // -1.0 + 2.0 * C.x
					0.024390243902439  // 1.0 / 41.0
					);

				// First corner
				float2 i = floor(v + dot(v, C.yy));
				float2 x0 = v - i + dot(i, C.xx);

				// Other corners
				// float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
				// Lex-DRL: afaik, step() in GPU is faster than if(), so:
				// step(x, y) = x <= y
				int xLessEqual = step(x0.x, x0.y); // x <= y ?
				int2 i1 =
					int2(1, 0) * (1 - xLessEqual) // x > y
					+ int2(0, 1) * xLessEqual // x <= y
					;
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;

				// Permutations
				i = mod289(i); // Avoid truncation effects in permutation
				float3 p = permute(
					permute(
						i.y + float3(0.0, i1.y, 1.0)
					) + i.x + float3(0.0, i1.x, 1.0)
				);

				float3 m = max(
					0.5 - float3(
						dot(x0, x0),
						dot(x12.xy, x12.xy),
						dot(x12.zw, x12.zw)
						),
					0.0
				);
				m = m * m;
				m = m * m;

				// Gradients: 41 points uniformly over a line, mapped onto a diamond.
				// The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)

				float3 x = 2.0 * frac(p * C.www) - 1.0;
				float3 h = abs(x) - 0.5;
				float3 ox = floor(x + 0.5);
				float3 a0 = x - ox;

				// Normalise gradients implicitly by scaling m
				// Approximation of: m *= inversesqrt( a0*a0 + h*h );
				m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h * h);

				// Compute final noise value at P
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot(m, g);
			}


            sampler2D _MainTex;

            float _X;
            float _Y;

            int _Octaves;
	        float _Gain;
	        float _Lacunarity;

			float _Height;

			float frag (v2f i) : SV_Target
			{
                i.uv += float2(_X, _Y);

				float height = 0;

				float amplitude = 0.5;
                float frequency = 1;

				for (int idx = 0; idx < _Octaves; idx++) {
					height += snoise(i.uv * frequency) * amplitude + 0.04;

					amplitude *= _Gain;
					frequency *= _Lacunarity;
				}

				return height * _Height;
			}

			ENDCG
		}
	}
}
