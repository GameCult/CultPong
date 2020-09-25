Shader "Aetheria/VolumeSun"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_ColorRamp ("Color Ramp", 2D) = "white" {}
		_Albedo ("Albedo", CUBE) = "black" {}
		_Offset ("Offset", CUBE) = "gray" {}
		_RayStepSize("Ray Step Size", Float) = .01
		_FirstOffsetDistance("First Offset Distance", Float) = .01
		_FirstOffsetFadePower("First Offset Fade Power", Float) = 1
		_FirstOffsetRotationFadePower("First Offset Rotation Fade Power", Float) = 1
		_SecondOffsetDistance("Second Offset Distance", Float) = .01
		_SecondOffsetFadePower("Second Offset Fade Power", Float) = 1
		_SecondOffsetRotationFadePower("Second Offset Rotation Fade Power", Float) = 1
		_EmissionFadePower("Emission Fade Power", Float) = 1
			//_LimbDarkening ("Limb Darkening", Float) = 1
			_AlphaPower("Alpha Fill Power", Float) = 1
			_Emission("Emission", Float) = 1
			_DepthBoost("Depth Boost", Float) = 1
			_DepthBoostPower("Depth Boost Power", Float) = 1
	}
		SubShader
		{
			Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 200

			CGPROGRAM

			#pragma surface surf Standard fullforwardshadows vertex:vert// alpha:fade
			// finalcolor:ApplyFog
		#pragma target 3.0
		#pragma multi_compile _ VOLUMETRIC_FOG

		#if VOLUMETRIC_FOG
			#include "../../VolumetricFog/Shaders/VolumetricFog.cginc"
		#endif
 
		sampler2D _CameraDepthTexture;
		sampler2D _ColorRamp;
		samplerCUBE _Albedo;
		samplerCUBE _Offset;
 
		struct Input {
			float2 uv_MainTex;
			float4 screenPos;
			float3 worldNormal;
			float3 viewDir;
			float3 objPos;
		};
 
		void ApplyFog(Input IN, SurfaceOutputStandard o, inout fixed4 color)
		{
			#if VOLUMETRIC_FOG
				half3 uvscreen = IN.screenPos.xyz/IN.screenPos.w;
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

		float _RayStepSize;
		float _FirstOffsetDistance;
		float _SecondOffsetDistance;
		float _FirstOffsetFadePower;
		float _SecondOffsetFadePower;
		float _LimbDarkening;
		float _EmissionFadePower;
		float _AlphaPower;
		float _Emission;
		float _DepthBoost;
		float _DepthBoostPower;

		float4x4 _AlbedoRotation;
		float4x4 _FirstOffsetDomainRotation;
		float4x4 _FirstOffsetRotation;
		float4x4 _SecondOffsetDomainRotation;
		float4x4 _SecondOffsetRotation;

		float _FirstOffsetRotationFadePower;
		float _SecondOffsetRotationFadePower;


		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.objPos = v.vertex;
		}

		const int raySteps = 16;
 
		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			float rim = saturate(dot(IN.viewDir,IN.worldNormal));
			float3 rayPos = IN.worldNormal;
			float3 rayStep = normalize(IN.viewDir)*_RayStepSize*(1.25-rim);
			float3 accum = 0;
			for(int i=0;i<32;i++){
				float surfDist = 1 - length(rayPos);

				float3 offset =  mul((float3x3)_FirstOffsetRotation  * (2 - pow(length(rayPos),_FirstOffsetRotationFadePower)),  
									 normalize(texCUBElod(_Offset, float4(mul((float3x3)_FirstOffsetDomainRotation,  normalize(rayPos         )), i/4)).rgb - float3(.5,.5,.5))) *
									 _FirstOffsetDistance * (1 - pow(length(rayPos),_FirstOffsetFadePower));

				float3 offset2 = mul((float3x3)_SecondOffsetRotation * (2 - pow(length(rayPos),_SecondOffsetRotationFadePower)), 
									 normalize(texCUBElod(_Offset, float4(mul((float3x3)_SecondOffsetDomainRotation, normalize(rayPos + offset)), i/4)).rgb - float3(.5,.5,.5))) * 
									 _SecondOffsetDistance * (1 - pow(length(rayPos),_SecondOffsetFadePower));

				float boor = texCUBElod (_Albedo, float4(mul((float3x3)_AlbedoRotation,normalize(rayPos + offset + offset2)) ,i/4)).x;
				accum += tex2D(_ColorRamp, boor.xx + pow(max(surfDist,0),_DepthBoostPower)*_DepthBoost) * pow(max(surfDist,0),_EmissionFadePower);
				rayPos += rayStep;
			}

			o.Emission = accum*_Emission;
			o.Alpha = 1-pow(1-rim,_AlphaPower);
		}
		ENDCG
	}
	FallBack "Standard"
}
