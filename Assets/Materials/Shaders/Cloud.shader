Shader "Custom/Cloud" {
	Properties {
		_Displace ("Displacement (RGB)", 2D) = "white" {}
		_Texture ("Texture (R)", 2D) = "white" {}
		_Ramp ("Color (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" }
		ZWrite Off Lighting Off Cull Off
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert
		#pragma target 3.0
		#pragma profileoption MaxTexIndirections=16

		sampler2D _Displace;
		sampler2D _Texture;
		sampler2D _Ramp;
		
		uniform float3 _CameraForward;
		uniform float3 _CameraUp;
		uniform float3 _CameraPos;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half3 up = _CameraUp; //UNITY_MATRIX_IT_MV[1].xyz;
			half3 forward = _CameraForward;
			forward.y = 0;
			normalize(forward);
			
			half upness = dot(up,IN.viewDir);
			half forwardness = dot(forward,IN.viewDir);
			
			half2 wpos = half2(IN.worldPos.x+IN.worldPos.y,IN.worldPos.z+IN.worldPos.y)/1000;
			half2 texco = wpos + half2(upness/500,forwardness/500);
			half2 displacement = tex2D (_Displace, wpos)+_CameraPos.xz/1000;
			half shading = tex2D (_Texture, wpos+displacement).r;
			
			o.Albedo = tex2D (_Ramp, half2(shading,0));
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
