#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0

float4x4 view_projection;

struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

PixelInput UntexturedVertexShader(VertexInput input)
{
    PixelInput output;
    
    output.Position = mul(input.Position, view_projection);
    output.Color = input.Color;
    
    return output;
}

float4 UntexturedPixelShader(PixelInput input) : SV_Target
{
    return input.Color;
}

technique BasicTech
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL UntexturedVertexShader();
        PixelShader = compile PS_SHADERMODEL UntexturedPixelShader();
    }
}
