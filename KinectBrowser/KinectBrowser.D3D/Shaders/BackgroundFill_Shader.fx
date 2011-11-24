uniform const float2 halfPixel;

struct VS_OUTPUT
{
	float4 Pos      : POSITION;
	float2 TexCoord : TEXCOORD0;
};

VS_OUTPUT vs_main(in float4 pos      : POSITION,
					 in float2 texCoord : TEXCOORD)
{
	VS_OUTPUT Out;
   
	Out.Pos      = pos;
	Out.TexCoord = texCoord + halfPixel;
   
	return Out;
}

float4 ps_main(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 baseColor = float4(cos(TexCoord.x),sin(TexCoord.y),TexCoord.x*TexCoord.y*4,1);
	baseColor.xyz *= 0.4 * (1 - TexCoord.y);

	return saturate(baseColor);
}

technique BackgroundFill 
{ 
   pass Pass_0 
   { 
	  VertexShader = compile vs_3_0 vs_main(); 
	  PixelShader = compile ps_3_0 ps_main(); 
   } 
}