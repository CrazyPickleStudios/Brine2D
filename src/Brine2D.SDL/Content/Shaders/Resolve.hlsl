// Fullscreen resolve: linear scene -> sRGB backbuffer. Blending OFF.

struct VSOut { float4 pos : SV_Position; float2 uv : TEXCOORD0; };

VSOut VSMain(uint id : SV_VertexID)
{
    const float2 POS[3] = { float2(-1,-1), float2(-1,3), float2(3,-1) };
    const float2 UV [3] = { float2(0,1),   float2(0,-1), float2(2,1)   };
    VSOut o; o.pos = float4(POS[id], 0, 1); o.uv = UV[id]; return o;
}

Texture2D    SceneTex  : register(t0, space2);
SamplerState SceneSamp : register(s0, space2);

static float3 LinearToSRGB(float3 x)
{
    x = saturate(x);
    const float a = 0.055, k0 = 0.0031308, phi = 12.92;
    float3 below = x * phi;
    float3 above = (1.0 + a) * pow(x, 1.0 / 2.4) - a;
    float3 t = step(k0.xxx, x); // per-component select
    return lerp(below, above, t);
}

float4 PSMain(VSOut i) : SV_Target
{
    float4 c = SceneTex.Sample(SceneSamp, i.uv); // linear
    c.rgb = LinearToSRGB(c.rgb);                 // encode once
    return c;                                    // alpha stays linear
}