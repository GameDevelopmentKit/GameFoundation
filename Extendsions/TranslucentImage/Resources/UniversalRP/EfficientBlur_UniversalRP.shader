Shader "Hidden/EfficientBlur_UniversalRP"
{
    HLSLINCLUDE
    //HLSLcc is not used by default on gles
    #pragma prefer_hlslcc gles
    //SRP don't support dx9
    #pragma exclude_renderers d3d11_9x

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "./lib.hlsl"


    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    uniform half4 _MainTex_TexelSize;
    uniform half _Radius;

    struct v2f
    {
        half4 vertex   : SV_POSITION;
        half4 texcoord : TEXCOORD0;
#if STEREO_INSTANCING_ENABLED
        uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
#endif
    };

    //From postprocessing. Don't want import
#if defined(UNITY_SINGLE_PASS_STEREO)
    float4 UnityStereoAdjustedTexelSize(float4 texelSize) // Should take in _MainTex_TexelSize
    {
        texelSize.x = texelSize.x * 2.0; // texelSize.x = 1/w. For a double-wide texture, the true resolution is given by 2/w.
        texelSize.z = texelSize.z * 0.5; // texelSize.z = w. For a double-wide texture, the true size of the eye texture is given by w/2.
        return texelSize;
    }
#else
    float4 UnityStereoAdjustedTexelSize(float4 texelSize)
    {
        return texelSize;
    }
#endif

    v2f vert(minimalVertexInput v)
    {
        v2f o;

        o.vertex = half4(v.position.xy, 0.0, 1.0);

        half4 offset = half2(-0.5h, 0.5h).xxyy; //-x, -y, x, y
        offset *= UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xyxy;
        offset *= _Radius;

        o.texcoord = VertexToUV(v.position.xy).xyxy + offset;

        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
//        half4 o =
//             SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.xw);
//        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.zw);
//        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.xy);
//        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.zy);
//        o /= 4.0;


        //Pray to the compiler god these will MAD
        half4 o =
             SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.xw) / 4.0h;
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.zw) / 4.0h;
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.xy) / 4.0h;
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord.zy) / 4.0h;
        return o;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            //Crop before blur
            #pragma vertex vertCrop
            #pragma fragment frag

            half4 _CropRegion;

			half2 getNewUV(half2 oldUV)
			{
			    return lerp(_CropRegion.xy, _CropRegion.zw, oldUV);
			}

			v2f vertCrop(minimalVertexInput v)
            {
                v2f o = vert(v);

                o.texcoord.xy = getNewUV(o.texcoord.xy);
                o.texcoord.zw = getNewUV(o.texcoord.zw);

                return o;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
