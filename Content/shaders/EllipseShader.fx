float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR
{
    return float4(texCoord, .5, 1.);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
