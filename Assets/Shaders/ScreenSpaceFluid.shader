Shader "ScreenSpaceFluids/ScreenSpaceFluid"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;
            float4 _Specular;
            float4 _Absorption;

            float _XFactor;
            float _YFactor;
            float _Shininess;
            float _Reflection;
            float _Refraction;
            float _Thickness;
            float _IndexOfRefraction;

            sampler2D _MainTex;
            sampler2D _BlurredDepthTex;
            sampler2D _ThicknessTex;

            float4 _MainTex_TexelSize;

            samplerCUBE _Cube;

            uniform sampler2D _CameraDepthTexture;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            struct fragOut
            {
                float4 color : COLOR;
                float depth : DEPTH;
            };

            float3 uvToEye(float2 texCoord, float z)
            {
                // Convert texture coordinate to homogeneous space
                float zFar = _ProjectionParams.z;
                float zNear = _ProjectionParams.y;

                float2 xyPos = texCoord * 2.0 - 1.0;
                float a = zFar / (zFar - zNear);
                float b = zFar * zNear / (zNear - zFar);
                float rd = b / (z - a);
                return float3(xyPos.x, xyPos.y, -1.0) * rd;
            }

            // Schlick's approximation
            float fresnel(float R0, float cosTheta)
            {
                return R0 + (1.0f - R0) * pow(1.0f - cosTheta, 5);
            }

            fragOut frag(v2f i)
            {
                fragOut OUT;

                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                    if (_MainTex_TexelSize.y < 0)
                        uv.y = 1 - uv.y;
                #endif

                float4 sceneCol = tex2D(_MainTex, i.uv);

                float depth = tex2D(_BlurredDepthTex, uv);
                float sceneDepth = tex2D(_CameraDepthTexture, uv.xy);
                float thickness = tex2D(_ThicknessTex, uv.xy);

                #if defined(UNITY_REVERSED_Z)
                    sceneDepth = 1.0f - sceneDepth;
                #endif

                // Reconstruct eye space pos from depth
                float3 eyePos = uvToEye(uv, depth);

                float2 texCoord1 = float2(uv.x + _XFactor, uv.y);
                float2 texCoord2 = float2(uv.x - _XFactor, uv.y);

                float3 ddx1 = uvToEye(texCoord1, tex2D(_BlurredDepthTex, texCoord1.xy)) - eyePos;
                float3 ddx2 = eyePos - uvToEye(texCoord2, tex2D(_BlurredDepthTex, texCoord2.xy));
                // Normal may not be well-defined at edges, use difference in opposite direction (hack!)
                if (abs(ddx1.z) > abs(ddx2.z))
                {
                    ddx1 = ddx2;
                }

                texCoord1 = float2(uv.x, uv.y + _YFactor);
                texCoord2 = float2(uv.x, uv.y - _YFactor);

                float3 ddy1 = uvToEye(texCoord1, tex2D(_BlurredDepthTex, texCoord1.xy)) - eyePos;
                float3 ddy2 = eyePos - uvToEye(texCoord2, tex2D(_BlurredDepthTex, texCoord2.xy));
                // Normal may not be well-defined at edges, use difference in opposite direction (hack!)
                if (abs(ddy1.z) > abs(ddy2.z))
                {
                    ddy1 = ddy2;
                }

                float3 normal = cross(ddx1, ddy1);
                normal = normalize(normal);

                // AMBIENT
                float3 ambient = _Color.rgb * UNITY_LIGHTMODEL_AMBIENT.rgb;

                // DIFFUSE
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float diffuseMul = max(0.0, dot(normal, lightDir)) * 0.5 + 0.5;
                float3 diffuse = _Color.rgb * diffuseMul * unity_LightColor0;

                // SPEC
                float3 v = normalize(-eyePos);
                float3 h = normalize(lightDir + v);
                float specularMul = pow(max(0.0, dot(normal, h)), _Shininess);
                float3 specular = _Specular.xyz * specularMul;

                // REFLECTION
                float3 reflectVec = reflect(-v, normal);
                float3 reflection = texCUBE(_Cube, reflectVec) * fresnel(0.02, dot(normal, v)) * _Reflection;

                // REFRACTION
                float distortionScale = (1.0 - 1.0 / _IndexOfRefraction) * _Refraction;
                float2 uvOffset = normal.xy * distortionScale * thickness;
                float3 refraction = tex2D(_MainTex, i.uv + uvOffset) * _Refraction; // Automatically handle clamp for me

                // COMPOSE
                // Beer's Law in volumetric rendering
                // Light decays exponentially with distance
                // Use different constant k for each color channel
                float3 alpha = exp(-thickness * _Absorption);
                float3 fluidCol = diffuse + ambient + specular + reflection + refraction;
                
                // If the fluid particle is behind a solid object in the scene, it has no contribution
                OUT.color = sceneDepth < depth ? sceneCol : float4(lerp(fluidCol.r, sceneCol.r, alpha.r),
                                                                   lerp(fluidCol.g, sceneCol.g, alpha.g),
                                                                   lerp(fluidCol.b, sceneCol.b, alpha.b), 1.0);
                OUT.depth = sceneDepth;

                return OUT;
            }
            ENDCG
        }
    }
}