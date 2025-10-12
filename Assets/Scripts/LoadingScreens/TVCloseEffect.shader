Shader "Unlit/TVCloseEffect"
{
    Properties
    {
        _MainTex("Source Texture", 2D) = "white" {}
        _ShrinkY("Vertical Shrink", Range(0, 1)) = 1.0
        _ShrinkX("Horizontal Shrink", Range(0, 1)) = 1.0
        _EdgeDarkness("Edge Darkness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "TVCloseEffect"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _ShrinkY;
            float _ShrinkX;
            float _EdgeDarkness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv      = input.uv;
                float2 centered= uv - 0.5;
                float2 scaled  = centered * float2(_ShrinkX, _ShrinkY) + 0.5;

                // 1) if we’re outside the shrunken rect → solid black
                if (scaled.x < 0.0 || scaled.x > 1.0 ||
                    scaled.y < 0.0 || scaled.y > 1.0)
                {
                    return half4(0,0,0,1);
                }

                // 2) sample the “real” camera image
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scaled);

                // 3) (optional) a thin dark edge *inside* the bars
                //    this only ramps over a small region at the very border
                float2 absC = abs(centered);
                float2 halfSize = float2(_ShrinkX, _ShrinkY) * 0.5;

                // how far into the black bar we are [0→1] over a normalized width
                float yBar = smoothstep(halfSize.y, halfSize.y + 0.01, absC.y);
                float xBar = smoothstep(halfSize.x, halfSize.x , absC.x);

                // combine only when that axis is currently shrinking
                float barDarkY = yBar * saturate((1.0 - _ShrinkY) * _EdgeDarkness);
                float barDarkX = xBar * saturate((1.0 - _ShrinkX) * _EdgeDarkness);

                color.rgb *= 1.0 - max(barDarkX, barDarkY);

                return color;
            }
            ENDHLSL
        }
    }
}
