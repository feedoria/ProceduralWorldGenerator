Shader "Foam" {
	Properties {
		_Offset ("Offset", Range (0.00,1.00)) = 1.000
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Cutout ("Mask (A)", 2D) = "white" {}
	}

	SubShader {
		// URP version of the old foam shader
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-110" "RenderType" = "Transparent" }
		ZWrite Off
		Offset -1, -1
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB
		Cull Off

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			TEXTURE2D(_Cutout);  SAMPLER(sampler_Cutout);

			CBUFFER_START(UnityPerMaterial)
				float _Offset;
				half4 _Color;
				float4 _MainTex_ST;
				float4 _Cutout_ST;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uvMain : TEXCOORD0;
				float2 uvMask : TEXCOORD1;
			};

			Varyings vert (Attributes v) {
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				// offset/scale come from WaveAnimation.cs
				o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvMask = TRANSFORM_TEX(v.uv, _Cutout);
				return o;
			}

			half4 frag (Varyings i) : SV_Target {
				half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvMain);
				half mask = SAMPLE_TEXTURE2D(_Cutout, sampler_Cutout, i.uvMask).a;
				// same as the old "texture * color double" combine
				half4 col = tex * _Color * 2;
				col.a *= mask;
				return col;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
