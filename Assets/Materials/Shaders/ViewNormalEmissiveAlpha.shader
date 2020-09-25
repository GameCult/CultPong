// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Aetheria/View Dependent Emissive Alpha" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_BumpPower ("Bump Power", Range (0.01,5)) = 1
		_Rotation ("Normal Rotation", Float) = 0
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		_EmissionColor ("Emission Color", Color) = (1,1,1,1)
		_Emission ("Emission Strength", Float) = 0
        _EmissionFresnel ("Emission Fresnel", Float) = 1.0
		[MaterialToggle] _ReverseRim("ReverseRim", Float) = 0
	}
	SubShader {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:fade finalcolor:ApplyFog

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma multi_compile _ VOLUMETRIC_FOG

		#if VOLUMETRIC_FOG
		#include "../../VolumetricFog/Shaders/VolumetricFog.cginc"
		#endif

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float4 screenPos;
			float3 viewDir;
			float3 worldNormal; 
			INTERNAL_DATA
		};

		void ApplyFog(Input IN, SurfaceOutputStandard o, inout fixed4 color)
		{
			#if VOLUMETRIC_FOG
			half3 uvscreen = IN.screenPos.xyz / IN.screenPos.w;
			half linear01Depth = Linear01Depth(uvscreen.z);
			fixed4 fog = Fog(linear01Depth, uvscreen.xy);

			// Always apply fog attenuation - also in the forward add pass.
			color.rgb *= fog.a;

			// Alpha premultiply mode (used with alpha and Standard lighting function, or explicitly alpha:premul)
			// uses source blend factor of One instead of SrcAlpha. `color` is compensated for it, so we need to compensate
			// the amount of inscattering too. A note on why this works: below.
			#if _ALPHAPREMULTIPLY_ON
			fog.rgb *= o.Alpha;
			#endif

			// Add inscattering only once, so in forward base, but not forward add.
			#ifndef UNITY_PASS_FORWARDADD
			color.rgb += fog.rgb;
			#endif
			#endif
		}

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		fixed4 _EmissionColor;
		float _BumpPower;
		float _Rotation;
		float _Emission;
        float _EmissionFresnel;
		half _ReverseRim;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;

            float sinX = sin ( _Rotation );
            float cosX = cos ( _Rotation );
            float sinY = sin ( _Rotation );
            float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);

			fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			normal.xy = mul ( normal.xy, rotationMatrix);
			normal.z = normal.z / _BumpPower;
			normal = normalize(normal);
			o.Normal = normal;

			half rim = saturate(dot (normalize(IN.viewDir), o.Normal));
			if(_ReverseRim > .5)
				rim = 1 - rim;
			o.Emission = _Emission * _EmissionColor * pow(rim,_EmissionFresnel);

			// Metallic and smoothness come from a texture
			float2 metalSmooth = tex2D (_MetallicGlossMap, IN.uv_MainTex).ra;
			o.Metallic = metalSmooth.x;
			o.Smoothness = metalSmooth.y * _Glossiness;

			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
