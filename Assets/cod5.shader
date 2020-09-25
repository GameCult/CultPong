Shader "cods/cod5"
{
	Properties
	{
		_lightPos("Point Light Position", Vector) = (1,1,1,1)
		_lightDir("Directional Light Direction", Vector) = (1,1,1,1)
		_C("Point Light Color", Color) = (1,1,1,1)
		_C2("Directional Light Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct input
			{
				float4 p : POSITION;
				float3 n : NORMAL;
			};

			struct v2f
			{
				float4 p : SV_POSITION;
				float4 c : COLOR;
			};

			uniform float3 _lightPos;
			uniform float3 _lightDir;
			uniform float4 _C;
			uniform float4 _C2;
			
			v2f vert (input i)
			{
				v2f o;
				o.p = UnityObjectToClipPos(i.p);
				float3 n = normalize( mul(unity_ObjectToWorld, float4(i.n, 0.0)).xyz);
				float3 v1 = -mul(UNITY_MATRIX_M, i.p).xyz + _lightPos;
				float distance = length(v1);
				float it1 = max(dot(n, v1/distance), 0.0) / (distance*distance);
				float it2 = max(dot(n, normalize(_lightDir))+.5, 0.0);
				o.c = _C*it1 + _C2*it2;

				return o;
			}
			
			float4 frag(v2f i) : COLOR
			{
				return i.c;
			}
			ENDCG
		}
	}
}
