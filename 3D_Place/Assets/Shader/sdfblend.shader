
Shader "Hidden/FaceShadow/SDFBlend"
{
    Properties
    {
        _MainTex ("Blit Source", 2D) = "white" {}

        _SDF0 ("SDF 180", 2D) = "black" {}
        _SDF1 ("SDF A", 2D) = "black" {}
        _SDF2 ("SDF B", 2D) = "black" {}
        _SDF3 ("SDF C", 2D) = "black" {}
        _SDF4 ("SDF D", 2D) = "black" {}
        _SDF5 ("SDF E", 2D) = "black" {}
        _SDF6 ("SDF F", 2D) = "black" {}
        _SDF7 ("SDF G", 2D) = "black" {}
        _SDF8 ("SDF H", 2D) = "black" {}

        _Delta ("Delta", Range(0, 0.05)) = 0.005
        _UseRGB ("Use RGB", Float) = 1
        _SampleCount ("Sample Count", Range(16, 256)) = 256
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            sampler2D _SDF0;
            sampler2D _SDF1;
            sampler2D _SDF2;
            sampler2D _SDF3;
            sampler2D _SDF4;
            sampler2D _SDF5;
            sampler2D _SDF6;
            sampler2D _SDF7;
            sampler2D _SDF8;

            float _Delta;
            float _UseRGB;
            float _SampleCount;

            fixed4 frag(v2f_img i) : SV_Target
            {
                // 1. 9장의 SDF 텍스처 샘플링
                float4 c0 = tex2D(_SDF0, i.uv);
                float4 c1 = tex2D(_SDF1, i.uv);
                float4 c2 = tex2D(_SDF2, i.uv);
                float4 c3 = tex2D(_SDF3, i.uv);
                float4 c4 = tex2D(_SDF4, i.uv);
                float4 c5 = tex2D(_SDF5, i.uv);
                float4 c6 = tex2D(_SDF6, i.uv);
                float4 c7 = tex2D(_SDF7, i.uv);
                float4 c8 = tex2D(_SDF8, i.uv);

                // 2. RGB 또는 Alpha 채널 선택
                float values[9];

                values[0] = lerp(c0.a, c0.r, _UseRGB);
                values[1] = lerp(c1.a, c1.r, _UseRGB);
                values[2] = lerp(c2.a, c2.r, _UseRGB);
                values[3] = lerp(c3.a, c3.r, _UseRGB);
                values[4] = lerp(c4.a, c4.r, _UseRGB);
                values[5] = lerp(c5.a, c5.r, _UseRGB);
                values[6] = lerp(c6.a, c6.r, _UseRGB);
                values[7] = lerp(c7.a, c7.r, _UseRGB);
                values[8] = lerp(c8.a, c8.r, _UseRGB);

                // 3. 256단계 보간 및 평균 계산
                int sampleCount = (int)clamp(
                    _SampleCount, 16.0, 256.0
                );

                float accumulation = 0.0;

                float safeDelta = max(_Delta, 0.00001);

                [loop]
                for (int j = 1; j <= 256; j++)
                {
                    if (j > sampleCount)
                        break;

                    float progress = (float)j / sampleCount;

                    // 9개 텍스처 사이의 8개 구간
                    float scaledProgress = progress * 8.0;

                    int segment = min(
                        (int)floor(scaledProgress), 7
                    );

                    float localT =
                        scaledProgress - segment;

                    // 인접한 SDF 보간
                    float sdfValue = lerp(
                        values[segment],
                        values[segment + 1],
                        localT
                    );

                    // 그림자 경계 부드럽게 처리
                    float shadowMask = smoothstep(
                        0.5 - safeDelta,
                        0.5 + safeDelta,
                        sdfValue
                    );

                    accumulation += shadowMask;
                }

                // 4. 모든 단계의 평균
                float result = accumulation / sampleCount;

                return float4(
                    result,
                    result,
                    result,
                    1.0
                );
            }

            ENDCG
        }
    }

    Fallback Off
}
