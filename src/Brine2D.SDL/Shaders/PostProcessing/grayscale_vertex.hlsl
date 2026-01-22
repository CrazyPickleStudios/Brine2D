// Grayscale post-processing vertex shader
// Generates a full-screen triangle from vertex ID

struct VSOutput
{
    float4 position : SV_Position;
    float2 texCoord : TEXCOORD0;
};

VSOutput main(uint vertexID : SV_VertexID)
{
    VSOutput output;
    
    // Generate full-screen triangle positions and UVs
    // vertexID 0: (-1, 1) -> uv (0, 0) -- top-left
    // vertexID 1: (-1, -3) -> uv (0, 2) -- way below bottom-left
    // vertexID 2: (3, 1) -> uv (2, 0) -- way right of top-right
    
    float x = (vertexID == 2) ? 3.0 : -1.0;
    float y = (vertexID == 1) ? -3.0 : 1.0;
    
    output.position = float4(x, y, 0.0, 1.0);
    
    // UV coordinates (0,0) = top-left, (1,1) = bottom-right
    float u = (vertexID == 2) ? 2.0 : 0.0;
    float v = (vertexID == 1) ? 2.0 : 0.0;
    
    output.texCoord = float2(u, v);
    
    return output;
}