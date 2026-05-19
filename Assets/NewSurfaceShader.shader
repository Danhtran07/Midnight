Shader "Custom/BetterTreeWind"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45

        _WindStrength ("Wind Strength", Range(0,1)) = 0.05

        _WindSpeed ("Wind Speed", Range(0,10)) = 1.2

        _Brightness ("Brightness", Range(0,3)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
            "DisableBatching"="True"
        }

        LOD 200

        Cull Off

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow

        #pragma target 3.0

        sampler2D _MainTex;

        float _Cutoff;
        float _WindStrength;
        float _WindSpeed;
        float _Brightness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float time =
            _Time.y * _WindSpeed;

            float worldOffset =
            (v.vertex.x + v.vertex.z) * 0.5;

            float wave1 =
            sin(time + worldOffset);

            float wave2 =
            cos(time * 0.7 + v.vertex.z);

            float flutter =
            sin(time * 3.0 + v.vertex.x * 5.0) * 0.15;

            float wave =
            (wave1 + wave2 + flutter) * 0.33;

            float heightMask =
            saturate(v.vertex.y);

            heightMask =
            pow(heightMask, 1.5);

            v.vertex.x +=
            wave *
            _WindStrength *
            heightMask;

            v.vertex.z +=
            wave *
            (_WindStrength * 0.5) *
            heightMask;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c =
            tex2D(_MainTex, IN.uv_MainTex);

            clip(c.a - _Cutoff);

            o.Albedo =
            c.rgb * _Brightness;

            o.Alpha = c.a;

            o.Metallic = 0;

            o.Smoothness = 0;
        }

        ENDCG
    }

    FallBack "Transparent/Cutout/Diffuse"
}