
float2 halfPixel;

uniform const texture depthMap : register(t0);

sampler depthSampler : register(s0) = sampler_state
{
	Texture = (depthMap);

	AddressU = CLAMP;
	AddressV = CLAMP;

	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};

//Vertex Input Structure
struct VSI
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

//Vertex Output Structure
struct VSO
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

//Vertex Shader
VSO VS(VSI input)
{
	//Initialize Output
	VSO output;

	output.Position = input.Position;
	output.TexCoord = input.TexCoord + halfPixel;
	

	//Return
	return output;
}

//Pixel Shader
float4 PS(VSO input) : COLOR0
{ 
	float3 samples[6] = 
	{
		float3(-1,-1,	-1),
		float3(-1, 0,	-2),
		float3(-1, 1,	-1),

		float3( 1,-1,	 1),
		float3( 1, 0,	 2),
		float3( 1, 1,	 1)
	};

	float accX = 0;
	float accY = 0;

	float vX, vY;

	bool hasZero = false;

	for(int i = 0;i<6;i++)
	{
		vX = tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples[i].xy)).r;
		vY = tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples[i].yx)).r;

		hasZero = hasZero || (vX == 0 || vY == 0);

		accX += vX * samples[i].z;
		accY += vY * samples[i].z;
	}

	float v = (tex2D(depthSampler, input.TexCoord).r);

	//return float4((v > 0 && v < 16000) ? 1 : 0, 0, 0, 1);

	float r = sqrt(accX * accX + accY * accY);

	return float4((v > 0.5 ? 1 : 0) * (v / 0.5), 0, 0, 1);// float4((hasZero && (r > 0)) ? 1 : 0, 0, 0, 1);

	//return float4(r > 60 ? 1 : 0,0,0,1);

	//return float4(r / 60, 0, 0, 1); 
}

//Technique
technique Compose
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS();
	}
}
