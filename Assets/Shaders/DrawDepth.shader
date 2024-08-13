Shader "ScreenSpaceFluids/DrawDepth"
{
    SubShader
    {
        Pass
        {
            ZTest LEqual
            Cull Off
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            StructuredBuffer<float4> _Positions;
            StructuredBuffer<float4> _Vertices;
            
            float _PointRadius;

            struct v2f
            {
                float4 position : POSITION;
                float2 tex : TEXCOORD0;
                float3 viewPos: TEXCOORD1;
            };
            
            v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
            {
                v2f o;

                // Quad vertex view space pos
                float4 viewPos = mul(UNITY_MATRIX_V, float4(_Positions[inst].xyz, 1.0)) +
                    float4(_Vertices[id].x * _PointRadius, _Vertices[id].y * _PointRadius, 0.0, 0.0);

                o.position = mul(UNITY_MATRIX_P, viewPos);
                // [-0.5, 0.5] -> [0, 1], which is uv coordinate of the quad defined in local space
                o.tex = _Vertices[id] + 0.5;
                o.viewPos = viewPos.xyz;
                
                return o;
            }

            struct fragOut
            {
                float color : COLOR;
                float depth : DEPTH;
            };

            fragOut frag(v2f i)
            {
                fragOut OUT;

                float3 normal;
                normal.xy = i.tex * 2.0 - 1.0;
                float r2 = dot(normal.xy, normal.xy);
                if (r2 > 1.0)
                    discard; // kill pixels outside circle

                normal.z = sqrt(1.0 - r2);

                float3 eyePos = i.viewPos + normal * _PointRadius;
                float4 ndcPos = mul(UNITY_MATRIX_P, float4(eyePos, 1.0));
                ndcPos.z /= ndcPos.w;
                float depth = ndcPos.z;
                
                // When UNITY_REVERSED_Z is enabled, Unity modifies the projection matrix
                #if defined(UNITY_REVERSED_Z)
                    OUT.color = 1.0 - depth;
                #else
				    OUT.color = depth;
                #endif

                // Unity has automatically flipped projection matrix if UNITY_REVERSED_Z is enabled
                // So we don't have to reverse depth by ourselves
                OUT.depth = depth;
                
                return OUT;
            }
            ENDCG
        }
    }

    Fallback off
}