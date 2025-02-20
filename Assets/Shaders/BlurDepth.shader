Shader "ScreenSpaceFluids/BlurDepth"
{
    SubShader
    {
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            sampler2D _DepthTex;

            float blurDepthFalloff;
            float scaleX;
            float scaleY;
            int radius;

            struct v2f
            {
                float4 pos : POSITION;
                float2 coord : TEXCOORD0;
            };

            struct fragOut
            {
                float color : COLOR;
            };

            v2f vert(appdata_img v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.coord = v.texcoord.xy;

                return o;
            }


            fragOut frag(v2f i)
            {
                fragOut OUT;
                
                float depth = tex2D(_DepthTex, i.coord);

                float blurScale = 2.0 / radius;

                float sum = 0.0;
                float wsum = 0.0;
                for (int x = -radius; x <= radius; x++)
                {
                    float cur = tex2D(_DepthTex, i.coord + (float)x * float2(scaleX, scaleY));
                    // Range domain
                    float r2 = (depth - cur) * blurDepthFalloff;
                    float g = exp(-r2 * r2);
                    // Spatial domain
                    float r = (float)x * blurScale;
                    float w = exp(-r * r);
                    sum += cur * w * g;
                    wsum += w * g;
                }

                if (wsum > 0.0)
                {
                    sum /= wsum;
                }

                OUT.color = sum;

                return OUT;
            }
            ENDCG
        }
    }

    FallBack Off
}