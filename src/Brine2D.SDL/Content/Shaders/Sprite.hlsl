struct VSIn {
    float2 pos   : TEXCOORD0; // loc 0
    float2 uv    : TEXCOORD1; // loc 1
    float4 color : TEXCOORD2; // loc 2
};

struct VSOut {
    float4 pos   : SV_Position;
    float2 uv    : TEXCOORD0;
    float4 color : TEXCOORD1;
};

Texture2D    Tex0  : register(t0, space2);
SamplerState Samp0 : register(s0, space2);

VSOut VSMain(VSIn i)
{
    VSOut o;
    o.pos   = float4(i.pos, 0.0, 1.0);
    o.uv    = i.uv;
    o.color = i.color;
    return o;
}

float4 PSMain(VSOut i) : SV_Target
{
    return Tex0.Sample(Samp0, i.uv) * float4(1,1,1,1);
}