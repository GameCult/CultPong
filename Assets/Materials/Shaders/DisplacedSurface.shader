Shader "Custom/DisplacedSurface" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_EmissionColor ("Emission Color", Color) = (1,1,1,1)
		_Emission ("Emission Strength", Float) = 0
        _EmissionFresnel ("Emission Fresnel", Float) = 1.0
		_MainTex ("World Albedo (RGB)", 2D) = "white" {}
		_MainTexTiling ("World Albedo Tiling", Float) = 1.0
		_MetallicGlossMap("World Metal/Gloss", 2D) = "white" {}
		_MetallicGlossTiling ("World Metal/Gloss Tiling", Float) = 1.0
		_BumpMap ("World Normalmap", 2D) = "bump" {}
		_BumpMapTiling ("World Normalmap Tiling", Float) = 1.0
		_AlphaMask ("Alpha Mask (R)", 2D) = "white" {}
		_AlphaTex ("World Alpha (R)", 2D) = "white" {}
		_AlphaTexTiling ("World Alpha Tiling", Float) = 1.0
		_AlphaRange("Alpha Range", Float) = 50
		_AlphaRangeFeather("Alpha Range Feather", Float) = 10
		_AlphaRangeCenter("Alpha Range Center", Float) = -60
        _DispTex ("Displacement Texture (R)", 2D) = "gray" {}
        _Displacement ("Displacement", Float) = 1.0
		_NormalStrength ("Normal Strength", Float) = 8
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
		Cull Off
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:fade finalcolor:ApplyFog vertex:disp nolightmap

		#pragma multi_compile _ VOLUMETRIC_FOG

		#if VOLUMETRIC_FOG
			#include "../../VolumetricFog/Shaders/VolumetricFog.cginc"
		#endif

		#pragma target 4.6

        struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

		sampler2D _MainTex;
		sampler2D _BumpMap;
        sampler2D _DispTex;
        sampler2D _AlphaTex;
        sampler2D _AlphaMask;
		sampler2D _MetallicGlossMap;
		
		//float4 _MainTex_ST;
		//float4 _AlphaTex_ST;
		float4 _DispTex_TexelSize;
		half4 _EmissionColor;
        float _Displacement;
        float _AlphaRange;
        float _AlphaRangeFeather;
        float _AlphaRangeCenter;
		float _MainTexTiling;
		float _MetallicGlossTiling;
		float _BumpMapTiling;
		float _AlphaTexTiling;

		float _NormalStrength;
		float _Emission;
        float _EmissionFresnel;

		float4 ComputeNormalsVS(in float2 uv:TEXCOORD0)
		{
			float tl = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2(-1, -1),0,0)).x);   // top left
			float  l = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2(-1,  0),0,0)).x);   // left
			float bl = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2(-1,  1),0,0)).x);   // bottom left
			float  t = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2( 0, -1),0,0)).x);   // top
			float  b = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2( 0,  1),0,0)).x);   // bottom
			float tr = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2( 1, -1),0,0)).x);   // top right
			float  r = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2( 1,  0),0,0)).x);   // right
			float br = abs(tex2Dlod (_DispTex, float4(uv + _DispTex_TexelSize.xy * float2( 1,  1),0,0)).x);   // bottom right
 
			// Compute dx using Sobel:
			//           -1 0 1 
			//           -2 0 2
			//           -1 0 1
			float dX = tr + 2*r + br -tl - 2*l - bl;
 
			// Compute dy using Sobel:
			//           -1 -2 -1 
			//            0  0  0
			//            1  2  1
			float dY = bl + 2*b + br -tl - 2*t - tr;
 
			// Build the normalized normal
			float4 N = float4(normalize(float3(dX, 1.0f / _NormalStrength, dY)), 1.0f);
 
			//convert (-1.0 , 1.0) to (0.0 , 1.0), if needed
			return N * 0.5f + 0.5f;
		}

        void disp (inout appdata v)
        {
            float d = tex2Dlod(_DispTex, float4(v.texcoord.xy,0,0)).r * _Displacement;
            v.vertex.y += d;/*
			float3 normal = ComputeNormalsVS(v.texcoord.xy);
			TANGENT_SPACE_ROTATION;
			float3 tangentNormal = mul(rotation, normal);
			v.normal = tangentNormal;*/
        }

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float4 screenPos;
			float3 viewDir;
			float3 worldNormal; 
			INTERNAL_DATA
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
		
		float3 calcNormal (Input IN)
		{
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.worldPos.xz*_BumpMapTiling));
			float me = tex2D(_DispTex,IN.uv_MainTex).x;
			float n = tex2D(_DispTex,float2(IN.uv_MainTex.x,IN.uv_MainTex.y+_DispTex_TexelSize.y)).x;
			float s = tex2D(_DispTex,float2(IN.uv_MainTex.x,IN.uv_MainTex.y-_DispTex_TexelSize.y)).x;
			float e = tex2D(_DispTex,float2(IN.uv_MainTex.x-_DispTex_TexelSize.x,IN.uv_MainTex.y)).x;
			float w = tex2D(_DispTex,float2(IN.uv_MainTex.x+_DispTex_TexelSize.x,IN.uv_MainTex.y)).x;
			float3 norm = normal;
			float3 temp = norm; //a temporary vector that is not parallel to norm
			if(norm.x==1)
				temp.y+=0.5;
			else
				temp.x+=0.5;
			//form a basis with norm being one of the axes:
			float3 perp1 = normalize(cross(norm,temp));
			float3 perp2 = normalize(cross(norm,perp1));
			//use the basis to move the normal in its own space by the offset
			float3 normalOffset = -_NormalStrength * ( ( (n-me) - (s-me) ) * perp1 + ( ( e - me ) - ( w - me ) ) * perp2 );
			norm += normalOffset;
			norm = normalize(norm);
			return norm;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.worldPos.xz*_MainTexTiling) * _Color;
			o.Albedo = c.rgb;

			// Metallic and smoothness come from a texture
			float2 metalSmooth = tex2D(_MetallicGlossMap, IN.worldPos.xz*_MetallicGlossTiling).ra;
			o.Metallic = metalSmooth.x;
			o.Smoothness = metalSmooth.y * _Glossiness;

			o.Alpha = tex2D (_AlphaMask, IN.uv_MainTex).r * tex2D (_AlphaTex, IN.worldPos.xz*_AlphaTexTiling).r * _Color.a * (1-saturate(max(0,abs(IN.worldPos.y-_AlphaRangeCenter)-_AlphaRange)/_AlphaRangeFeather));

			o.Normal = calcNormal(IN);
			
			half rim = saturate(dot (normalize(IN.viewDir), o.Normal));
			o.Emission = _Emission * _EmissionColor * pow(rim,_EmissionFresnel);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
