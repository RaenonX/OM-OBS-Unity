Shader "Unlit/PlasmaWaver"
{
    Properties
    {
        [PerRendererData] _MainTex("Particle Texture", 2D) = "white" {}
        gridSmoothWidth ("Grid Smooth Width", Float) = 0.015
        axisWidth ("Axis Width", Float) = 0.05
        majorLineWidth ("Major Line Width", Float) = 0.025
        minorLineWidth ("Minor Line Width", Float) = 0.0125
        majorLineFrequency ("Major Line Frequency", Float) = 5.0
        minorLineFrequency ("Minor Line Frequency", Float) = 1.0
        gridColor ("Grid Color", Color) = (0.5, 0.5, 0.5, 0.5)
        scale ("Scale", Float) = 5.0
        lineColor ("Line Color", Color) = (0.25, 0.5, 1.0, 1.0)
        minLineWidth ("Min Line Width", Float) = 0.02
        maxLineWidth ("Max Line Width", Float) = 0.5
        lineSpeed ("Line Speed", Float) = 1.0
        lineAmplitude ("Line Amplitude", Float) = 1.0
        lineFrequency ("Line Frequency", Float) = 0.2
        warpSpeed ("Warp Speed", Float) = 0.2
        warpFrequency ("Warp Frequency", Float) = 0.5
        warpAmplitude ("Warp Amplitude", Float) = 1.0
        offsetFrequency ("Offset Frequency", Float) = 0.5
        offsetSpeed ("Offset Speed", Float) = 1.33
        minOffsetSpread ("Min Offset Spread", Float) = 0.6
        maxOffsetSpread ("Max Offset Spread", Float) = 2.0
        minorBackgroundColor ("Minor Background Color", Color) = (0.05, 0.3, 0.3, 0)
        majorBackgroundColor ("Major Background Color", Color) = (0.125, 0.25, 0.5, 0.5)
        linesPerGroup ("Lines Per Group", Int) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
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

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float overallSpeed;
            float gridSmoothWidth;
            float axisWidth;
            float majorLineWidth;
            float minorLineWidth;
            float majorLineFrequency;
            float minorLineFrequency;
            float4 gridColor;
            float scale;
            float4 lineColor;
            float minLineWidth;
            float maxLineWidth;
            float lineSpeed;
            float lineAmplitude;
            float lineFrequency;
            float warpSpeed;
            float warpFrequency;
            float warpAmplitude;
            float offsetFrequency;
            float offsetSpeed;
            float minOffsetSpread;
            float maxOffsetSpread;
            int linesPerGroup;
            float4 majorBackgroundColor;
            float4 minorBackgroundColor;
            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;

            #define mod(x, y) (x - y * floor(x/y))
            #define drawCircle(pos, radius, coord) smoothstep(radius + gridSmoothWidth, radius, length(coord - (pos)))
            #define drawSmoothLine(pos, halfWidth, t) smoothstep(halfWidth, 0.0, abs(pos - (t)))
            #define drawCrispLine(pos, halfWidth, t) smoothstep(halfWidth + gridSmoothWidth, halfWidth, abs(pos - (t)))
            #define drawPeriodicLine(freq, width, t) drawCrispLine(freq / 2.0, width, abs(mod(t, freq) - (freq) / 2.0))

            float drawGridLines(float axis)
            {
                return drawCrispLine(0.0, axisWidth, axis)
                    + drawPeriodicLine(majorLineFrequency, majorLineWidth, axis)
                    + drawPeriodicLine(minorLineFrequency, minorLineWidth, axis);
            }

            float drawGrid(float2 space)
            {
                return min(1., drawGridLines(space.x)
                    + drawGridLines(space.y));
            }

            // probably can optimize w/ noise, but currently using fourier transform
            float random(float t)
            {
                return (cos(t) + cos(t * 1.3 + 1.3) + cos(t * 1.4 + 1.4)) / 3.0;
            }

            float getPlasmaY(float x, float horizontalFade, float offset)
            {
                return random(x * lineFrequency + _Time.y * lineSpeed) * horizontalFade * lineAmplitude + offset;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 space = (i.uv - 0.5) / scale * 1.5;
                //float2 space = (fragCoord - iResolution.xy / 2.0) / iResolution.x * 2.0 * scale;

                float horizontalFade = 1.0 - (cos(i.uv.x * 6.28) * 0.5 + 0.5);
                float verticalFade = 1.0 - (cos(i.uv.y * 6.28) * 0.5 + 0.5);

                // fun with nonlinear transformations! (wind / turbulence)
                space.y += random(space.x * warpFrequency + _Time.y * warpSpeed) * warpAmplitude * (0.5 + horizontalFade);
                space.x += random(space.y * warpFrequency + _Time.y * warpSpeed + 2.0) * warpAmplitude * horizontalFade;

                float4 lines = 0;

                for (int l = 0; l < linesPerGroup; l++)
                {
                    float normalizedLineIndex = float(l) / float(linesPerGroup);
                    float offsetTime = _Time.y * offsetSpeed;
                    float offsetPosition = float(l) + space.x * offsetFrequency;
                    float rand = random(offsetPosition + offsetTime) * 0.5 + 0.5;
                    float halfWidth = lerp(minLineWidth, maxLineWidth, rand * horizontalFade) / 2.0;
                    float offset = random(offsetPosition + offsetTime * (1.0 + normalizedLineIndex)) * lerp(minOffsetSpread, maxOffsetSpread, horizontalFade);
                    float linePosition = getPlasmaY(space.x, horizontalFade, offset);
                    float _line = drawSmoothLine(linePosition, halfWidth, space.y) / 2.0 + drawCrispLine(linePosition, halfWidth * 0.15, space.y);

                    float circleX = mod(float(l) + _Time.y * lineSpeed, 25.0) - 12.0;
                    float2 circlePosition = float2(circleX, getPlasmaY(circleX, horizontalFade, offset));
                    float circle = drawCircle(circlePosition, 0.01, space) * 4.0;


                    _line = _line + circle;
                    lines += _line * lineColor * rand;
                }

                float4 fragColor = tex2D(_MainTex, i.uv);
                //float4 fragColor = lerp(majorBackgroundColor, minorBackgroundColor, i.uv.x);
                //fragColor *= verticalFade;
                // debug grid:
                //fragColor = lerp(fragColor, gridColor, drawGrid(space));
                fragColor += lines;
                fragColor.a = 1.0;

                return fragColor;
            }
            ENDCG
        }
    }
}
