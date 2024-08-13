Shader "ScreenSpaceFluids/DrawThickness"
{
    SubShader
    {
        Pass
        {
            Blend One One // Additive
            
            ZTest Always
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            StructuredBuffer<float4> _Positions;
            StructuredBuffer<float4> _Vertices;

            uniform sampler2D _MainTex;
            uniform sampler2D _CameraDepthTexture;

            float _PointRadius;
            float _Thickness;
            float _Softness;

            struct v2f
            {
                float4 position : POSITION;
                float2 tex : TEXCOORD0;
                float3 viewPos: TEXCOORD1;
                float4 projPos : TEXCOORD2; // screen position of vertex
            };

            v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
            {
                v2f o;

                float4 viewPos = mul(UNITY_MATRIX_V, float4(_Positions[inst].xyz, 1.0)) +
                    float4(_Vertices[id].x * _PointRadius, _Vertices[id].y * _PointRadius, 0.0, 0.0);

                o.position = mul(UNITY_MATRIX_P, viewPos);

                o.tex = _Vertices[id] + 0.5;

                o.viewPos = viewPos.xyz;
                o.projPos = ComputeScreenPos(o.position);
                
                return o;
            }

            float frag(v2f i) : COLOR
            {
                // _CameraDepthTexture records the scene depth before drawing fluid
                float sceneDepth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r;
                #if defined(UNITY_REVERSED_Z)
                    sceneDepth = 1.0f - sceneDepth;
                #endif

                // Calculate eye-space sphere normal from texture coordinates
                float3 N;
                N.xy = i.tex * 2.0 - 1.0;
                float r2 = dot(N.xy, N.xy);
                if (r2 > 1.0)
                    discard; // kill pixels outside circle
                N.z = sqrt(1.0 - r2);
                
                float alpha = N.z;

                float3 eyePos = i.viewPos + N * _PointRadius;
                float4 ndcPos = mul(UNITY_MATRIX_P, float4(eyePos, 1.0));
                ndcPos.z /= ndcPos.w;

                // If the fluid particle is behind a solid object in the scene, it has no contribution
                // We're using additive blend mode, so thickness would accumulate
                float depth = ndcPos.z;
                return sceneDepth < depth ? 0 : alpha;
            }
            ENDCG
        }
    }

    Fallback off
}