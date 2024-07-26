Shader "PBDFluid/VoxelBlock" 
{
	Properties
	{
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		
		// Physically based Standard lighting model
		#pragma surface surf Standard fullforwardshadows
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup

		float4 _Color;
		float _Diameter;
		half _Glossiness;
		half _Metallic;

		float _Clip;
		
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<float4> _Positions;
#endif

		void setup()
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			float3 pos = _Positions[unity_InstanceID];
			float d = _Diameter;

			unity_ObjectToWorld._11_21_31_41 = float4(d, 0, 0, 0);
			unity_ObjectToWorld._12_22_32_42 = float4(0, d, 0, 0);
			unity_ObjectToWorld._13_23_33_43 = float4(0, 0, d, 0);
			unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);

			unity_WorldToObject = unity_ObjectToWorld;
			unity_WorldToObject._14_24_34 *= -1;
			unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;

			_Clip = -1.0 + _Positions[unity_InstanceID].w;
#endif
		}

		struct Input 
		{
			float4 color;
		};

		void surf(Input IN, inout SurfaceOutputStandard o) 
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			clip(_Clip);
#endif
			o.Albedo = _Color.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
		}
		
		ENDCG
	}
}