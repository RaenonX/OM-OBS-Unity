Shader "Unlit/PlasmaWaver"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0

        overallSpeed ("Overall Speed", Float) = 0.2
        gridSmoothWidth ("Grid Smooth Width", Float) = 0.015
        //axisWidth ("Axis Width", Float) = 0.05
        majorLineWidth ("Major Line Width", Float) = 0.025
        minorLineWidth ("Minor Line Width", Float) = 0.0125
        //majorLineFrequency ("Major Line Frequency", Float) = 5.0
        //minorLineFrequency ("Minor Line Frequency", Float) = 1.0
        scale ("Scale", Float) = 5.0
        lineColor ("Line Color", Color) = (0.25, 0.5, 1.0, 1.0)
        minLineWidth ("Min Line Width", Float) = 0.02
        maxLineWidth ("Max Line Width", Float) = 0.5
        lineSpeed ("Line Speed", Float) = 1.0
        lineAmplitude ("Line Amplitude", Float) = 1.0
        lineFrequency ("Line Frequency", Float) = 0.2
        warpFrequency ("Warp Frequency", Float) = 0.5
        warpAmplitude ("Warp Amplitude", Float) = 1.0
        warpSpeed ("Warp Speed", Float) = 0.2
        offsetFrequency ("Offset Frequency", Float) = 0.5
        offsetSpeed ("Offset Speed", Float) = 1.33
        minOffsetSpread ("Min Offset Spread", Float) = 0.6
        maxOffsetSpread ("Max Offset Spread", Float) = 2.0
        linesPerGroup ("Lines Per Group", Int) = 16
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.uv = TRANSFORM_TEX(v.uv, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            float overallSpeed;
            float gridSmoothWidth;
            //float axisWidth;
            float majorLineWidth;
            float minorLineWidth;
            //float majorLineFrequency;
            //float minorLineFrequency;
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

            #define mod(x, y) (x - y * floor(x/y))
            #define drawCircle(pos, radius, coord) smoothstep(radius + gridSmoothWidth, radius, length(coord - (pos)))
            #define drawSmoothLine(pos, halfWidth, t) smoothstep(halfWidth, 0.0, abs(pos - (t)))
            #define drawCrispLine(pos, halfWidth, t) smoothstep(halfWidth + gridSmoothWidth, halfWidth, abs(pos - (t)))

            // probably can optimize w/ noise, but currently using fourier transform
            float random(float t)
            {
                return (cos(t) + cos(t * 1.3 + 1.3) + cos(t * 1.4 + 1.4)) / 3.0;
            }

            float getPlasmaY(float x, float horizontalFade, float offset, float speed)
            {
                return random(x * lineFrequency + _Time.y * speed) * horizontalFade * lineAmplitude + offset;
            }

            float4 frag(v2f IN) : SV_Target
            {
                float4 fragColor = (tex2D(_MainTex, IN.uv) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                fragColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(fragColor.a - 0.001);
                #endif

                warpSpeed *= overallSpeed;
                offsetSpeed *= overallSpeed;
                lineSpeed *= overallSpeed;

                float2 space = (IN.uv - 0.5) / scale * 1.5;
                //float2 space = (fragCoord - iResolution.xy / 2.0) / iResolution.x * 2.0 * scale;

                float horizontalFade = 1.0 - (cos(IN.uv.x * 6.28) * 0.5 + 0.5);
                float verticalFade = 1.0 - (cos(IN.uv.y * 6.28) * 0.5 + 0.5);

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
                    float linePosition = getPlasmaY(space.x, horizontalFade, offset, lineSpeed);
                    float _line = drawSmoothLine(linePosition, halfWidth, space.y) / 2.0 + drawCrispLine(linePosition, halfWidth * 0.15, space.y);

                    float circleX = mod(float(l) + _Time.y * lineSpeed, 25.0) - 12.0;
                    float2 circlePosition = float2(circleX, getPlasmaY(circleX, horizontalFade, offset, lineSpeed));
                    float circle = drawCircle(circlePosition, 0.01, space) * 4.0;


                    _line = _line + circle;
                    lines += _line * lineColor * rand;
                }

                fragColor += lines;
                return fragColor;
            }
        ENDCG
        }
    }
}
