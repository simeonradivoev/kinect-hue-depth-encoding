// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Unlit/Point Cloud Shader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile __ FLIP_Y

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 data : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_DECLARE_TEX2D(_DepthMap);
            float3 _Size;
            bool _UseRaw;
            float2 _CutOff;
            float _PointSize;
            float _PointSizeRandomness;
            float _PointPositionRandomness;
            float4 _Resolution;
            float4x4 _Transform;
            float4 _Color;
            float2 _RayScale;

            const float Max16 = 65535;
            static const int DepthWidth = 640;
static const int DepthHeight = 480;
static const float2 DepthWidthHeight = float2(DepthWidth, DepthHeight);
static const float2 DepthHalfWidthHeight = DepthWidthHeight / 2.0;
static const float2 DepthHalfWidthHeightOffset = DepthHalfWidthHeight - 0.5;

            float rand(float n) {
                return frac(sin(n * 12.9898) * 43758.5453);
            }

            float3 rgb2hsv(float3 c) {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            inline float RGB2Hue(float3 c)
            {
                float minc = min(min(c.r, c.g), c.b);
                float maxc = max(max(c.r, c.g), c.b);
                float div = 1 / (6 * max(maxc - minc, 1e-5));
                float r = (c.g - c.b) * div;
                float g = 1.0 / 3 + (c.b - c.r) * div;
                float b = 2.0 / 3 + (c.r - c.g) * div;
                float d = lerp(r, lerp(g, b, c.g < c.b), c.r < max(c.g, c.b));
                return frac(d + 1);
            }

            float decode(float3 data) 
            {
                float dNormal = RGB2Hue(data) * 1529.0;

                half dMin = _CutOff.x;
                half dMax = _CutOff.y;

                return dMin + ((dMax - dMin) * dNormal) / 1529.0;
            }

            inline half3 LinearToGammaSpaceAccurate(half3 linRGB)
            {
                // Exact version, useful for debugging.
                return half3(LinearToGammaSpaceExact(linRGB.r), LinearToGammaSpaceExact(linRGB.g), LinearToGammaSpaceExact(linRGB.b));
            }

            v2f vert (uint id:SV_VertexID, appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                int x = id % DepthWidth;
                int y = id / DepthWidth;
#if FLIP_Y
                float3 data = _DepthMap.Load(uint3(x, DepthHeight - y, 0)).rgb;
#else
                float3 data = _DepthMap.Load(uint3(x, y, 0)).rgb;
#endif
                float depth = decode(LinearToGammaSpaceAccurate(data));
                //depth *= _CutOff.y - _CutOff.x;
                //depth += _CutOff.x;
                //float depth = (data.g * 255.0 + ((int)(data.b * 255.0) << 8)) * 1.525902189669642e-5;
                //float depth = data.r + data.g + data.b;
                //float depth = data.r;
                float luminence = 1;
                float3 r = float3(rand(id), rand(id + 128), rand(id + 256));
                float2 xyPos = (float2(x,y) - DepthHalfWidthHeightOffset) * _RayScale * depth;
                float zDistance = (saturate(depth) + r.x * _PointPositionRandomness);
                float3 xyz = v.vertex.xyz + float3(xyPos, zDistance) * _Size;
                o.vertex = UnityObjectToClipPos(mul(_Transform, float4(xyz, 1)));
                bool isCulled = depth > _CutOff.x;
                bool shouldRender = r.y < saturate(1.0 / pow(o.vertex.w, 2));
                o.data = float4(luminence, isCulled, r.z,0);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            [maxvertexcount(6)]
            void geom(point v2f input[1], uint id:SV_PrimitiveID, inout TriangleStream<v2f> outStream)
            {
                if (input[0].data.g <= 0) {
                    return;
                }

                float4 origin = input[0].vertex;
                float r = input[0].data.b;
                float d = 1.0 / origin.w;
                float sizeMul = lerp(0, 1, saturate(saturate(d - r) * 20));

                float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize) * lerp(1,r, _PointSizeRandomness) * sizeMul;
                v2f o = input[0];
                float size = extent.y * origin.w * _ScreenParams.y;

                o.vertex = origin + float4(size, size,0,0);
                o.data.zw = float2(0, 0);
                o.data.zw = float2(0, 0);
                outStream.Append(o);
                o.vertex = origin + float4(-size, size, 0, 0);
                o.data.zw = float2(1, 0);
                outStream.Append(o);
                o.vertex = origin + float4(-size, -size, 0, 0);
                o.data.zw = float2(1, 1);
                outStream.Append(o);

                o.vertex = origin + float4(size, size, 0, 0);
                o.data.zw = float2(0, 0);
                outStream.Append(o);
                o.vertex = origin + float4(size, -size, 0, 0);
                o.data.zw = float2(0, 1);
                outStream.Append(o);
                o.vertex = origin + float4(-size, -size, 0, 0);
                o.data.zw = float2(1, 1);
                outStream.Append(o);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                /*float2 uv = abs(i.data.zw - 0.5f);
                float d = length(uv);
                float pwidth = fwidth(d);
                float alpha = saturate((0.5 - d) / pwidth);*/
                return float4(_Color.rgb * saturate(_Color.a * i.data.r * 100),1);
            }
            ENDCG
        }
    }
}
