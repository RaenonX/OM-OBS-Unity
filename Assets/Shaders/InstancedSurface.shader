Shader "Instanced/InstancedSurfaceShader" {
    Properties
    {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _EmissionMap("Emission", 2D) = "black" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
    SubShader
        {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;
        sampler2D _EmissionMap;

        struct Input {
            float2 uv_MainTex;
        };

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<float4> PositionBuffer;
        #endif
        float4x4 ObjectToWorld;
        float4 InstancedArgs;
        float4 AxisAngle;

        float4x4 inverse(float4x4 input)
        {
            #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
            float4x4 cofactors = float4x4(
                minor(_22_23_24, _32_33_34, _42_43_44),
                -minor(_21_23_24, _31_33_34, _41_43_44),
                minor(_21_22_24, _31_32_34, _41_42_44),
                -minor(_21_22_23, _31_32_33, _41_42_43),

                -minor(_12_13_14, _32_33_34, _42_43_44),
                minor(_11_13_14, _31_33_34, _41_43_44),
                -minor(_11_12_14, _31_32_34, _41_42_44),
                minor(_11_12_13, _31_32_33, _41_42_43),

                minor(_12_13_14, _22_23_24, _42_43_44),
                -minor(_11_13_14, _21_23_24, _41_43_44),
                minor(_11_12_14, _21_22_24, _41_42_44),
                -minor(_11_12_13, _21_22_23, _41_42_43),

                -minor(_12_13_14, _22_23_24, _32_33_34),
                minor(_11_13_14, _21_23_24, _31_33_34),
                -minor(_11_12_14, _21_22_24, _31_32_34),
                minor(_11_12_13, _21_22_23, _31_32_33)
                );
            #undef minor
            return transpose(cofactors) / determinant(input);
        }

        float4x4 matrixFromData(float4 axisAngle, float4 positionScale)
        {
            float4 q = float4(
                axisAngle.xyz * sin(axisAngle.w * 0.5),
                cos(axisAngle.w * 0.5)
            );

            float x = q.x * 2;
            float y = q.y * 2;
            float z = q.z * 2;
            float xx = q.x * x;
            float yy = q.y * y;
            float zz = q.z * z;
            float xy = q.x * y;
            float xz = q.x * z;
            float yz = q.y * z;
            float wx = q.w * x;
            float wy = q.w * y;
            float wz = q.w * z;

            float4x4 m;
            m._11_21_31_41 = float4((1 - (yy + zz)), xy + wz, xz - wy, 0);
            m._12_22_32_42 = float4(xy - wz, (1 - (xx + zz)), yz + wx, 0);
            m._13_23_33_43 = float4(xz + wy, yz - wx, (1 - (xx + yy)), 0);
            m._14_24_34_44 = float4(positionScale.xyz, 1);
            m._11_22_33 *= positionScale.w;

            return m;
        }

        void setup()
        {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float4 data = PositionBuffer[unity_InstanceID];

            float angleModifier = clamp(AxisAngle.w + unity_InstanceID * InstancedArgs.x, 0, 6.28318530718);
            float scaleModifier = clamp(InstancedArgs.w + unity_InstanceID * InstancedArgs.y, 0, 1);

            data.w *= scaleModifier;
            float4x4 instanceToObject = matrixFromData(float4(AxisAngle.xyz, angleModifier), data);
            unity_ObjectToWorld = mul(ObjectToWorld, instanceToObject);
            unity_WorldToObject = inverse(unity_ObjectToWorld);
        #endif
        }

        half _Glossiness;
        half _Metallic;

        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = tex2D(_EmissionMap, IN.uv_MainTex);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}