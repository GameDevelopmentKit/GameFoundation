Shader "Hidden/FillCrop_UniversalRP"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
            //HLSLcc is not used by default on gles
            #pragma prefer_hlslcc gles
            //SRP don't support dx9
            #pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "./lib.hlsl"

			minimalVertexOutput vert (minimalVertexInput v)
			{
				minimalVertexOutput o;
				o.position = v.position;
				o.texcoord = VertexToUV(v.position.xy);
				return o;
			}

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
			float4 _CropRegion;

			half2 getCroppedCoord(half2 screenCoord)
            {
                return (screenCoord - _CropRegion.xy)/(_CropRegion.zw - _CropRegion.xy);
            }

			half4 frag (minimalVertexOutput v) : SV_Target
			{
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, getCroppedCoord(v.texcoord));
			}
			ENDHLSL
		}
	}
}
