// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particles/Soft Additive Rotating Morph" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_Speed ("Morph Speed", Float) = .01
	_Offset ("Offset", 2D) = "gray" {}
	_OffsetDistance ("Offset Distance", Float) = 1
	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend One OneMinusSrcColor
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}

	// ---- Fragment program cards
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_particles

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _Offset;
			fixed4 _TintColor;
			float _OffsetDistance;
			float _Speed;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD1;
				#endif
			};

			float4 _MainTex_ST;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				#endif
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			sampler2D _CameraDepthTexture;
			float _InvFade;
			
			fixed4 frag (v2f i) : COLOR
			{
				#ifdef SOFTPARTICLES_ON
				float sceneZ = LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
				float partZ = i.projPos.z;
				float fade = saturate (_InvFade * (sceneZ-partZ));
				i.color.a *= fade;
				#endif
				
				float2 offset = 0;
				i.texcoord -= float2(.5,.5);
				
				float2 texcoord1 = i.texcoord;
				texcoord1.x = i.texcoord.x*cos(_Time.x*_Speed) - i.texcoord.y*sin(_Time.x*_Speed);
				texcoord1.y = i.texcoord.y*cos(_Time.x*_Speed) + i.texcoord.x*sin(_Time.x*_Speed);
				texcoord1 += float2(.5,.5);
				offset += tex2D(_Offset, texcoord1).xy*_OffsetDistance;
				
				float2 texcoord2 = i.texcoord;
				texcoord2.x = i.texcoord.x*cos(-_Time.x*_Speed) - i.texcoord.y*sin(-_Time.x*_Speed);
				texcoord2.y = i.texcoord.y*cos(-_Time.x*_Speed) + i.texcoord.x*sin(-_Time.x*_Speed);
				texcoord2 /= 2;
				texcoord2 += float2(.5,.5);
				offset += tex2D(_Offset, texcoord2).xy*_OffsetDistance;
				
				i.texcoord += float2(.5,.5);
				half4 prev = i.color * tex2D(_MainTex, i.texcoord+offset);
				prev.rgb *= prev.a;
				return prev;
			}
			ENDCG 
		}
	} 	

	// ---- Dual texture cards
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary
			}
			SetTexture [_MainTex] {
				combine previous * previous alpha, previous
			}
		}
	}
	
	// ---- Single texture cards (does not do particle colors)
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * texture alpha, texture
			}
		}
	}
}
}