Shader "Custom/Limit_Shader"
{
	Properties
	{
		_MainTex("Albedo Texture", 2D) = "white" {}
		_TintColor("Tint Color", Color) = (1,1,1,1)
		_TransparencyDistance("Transparency Distance", Float) = 1
	}

	SubShader
	{
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 position : SV_Position;
				float2 texcoord : TEXCOORD0;
				float distance : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _TintColor;
			float _TransparencyDistance;

			Varyings vert(appdata v)
			{
				Varyings o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.uv, _MainTex);
				o.distance = length(ObjSpaceViewDir(v.vertex));
				return o;
			}

			fixed4 frag(Varyings i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.texcoord) * float4(_TintColor.rgb, 1);
				col.a *= (1.0 - pow(clamp(i.distance, 0.0, _TransparencyDistance) / _TransparencyDistance, 2));
				return col;
			}
			ENDCG
		}
	}
}