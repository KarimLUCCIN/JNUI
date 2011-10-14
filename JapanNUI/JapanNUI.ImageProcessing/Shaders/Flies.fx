/*

Les particules sont stoquées sous la forme de textures afin de permettre l'échange et la mise à jour avec le GPU

On doit pouvoir avoir des fenêtres de pixels se déplaçant dans une fenêtre de 320x240, donc, avec un format Rgba64 (4 x 16bits):

- [RG] Pour le XY on utilise deux floats sur 16bits (HalfSingle)
- [B ] Pour le score actuel, on utilise un float sur 16bits entre 0 et 1
- [A ] Vide

Tous les rendus sont faits avec un full screen quad

*/

/* ---------------- COMMON ------------- */

float2 fliesHalfPixel;
float2 bordersHalfPixel;

uniform const texture previousPopulationMap : register(t0);
uniform const texture noiseMap : register(t1);
uniform const texture bordersMap : register(t2);

uniform const float randomNoiseOffset;

sampler previousPopulationMapSampler : register(s0) = sampler_state
{
	Texture = (previousPopulationMap);

	AddressU = CLAMP;
	AddressV = CLAMP;

	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};

sampler noiseMapSampler : register(s1) = sampler_state
{
	Texture = (noiseMap);

	AddressU = WRAP;
	AddressV = WRAP;

	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};

sampler bordersMapSampler : register(s2) = sampler_state
{
	Texture = (bordersMap);

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
	output.TexCoord = input.TexCoord;

	//Return
	return output;
}

/* ---------------- RESET ------------- */

void PS_Reset(VSO input, out float4 color : COLOR0)
{
	color.rg = input.TexCoord;
	color.b = 0;
	color.a = 1;
}

//Technique
technique Reset
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS_Reset();
	}
}

/* ---------------- GROW ------------- */

bool computeScore(inout float4 oldFly, inout float4 newFly)
{
	float oldScore = tex2Dlod(bordersMapSampler, float4(oldFly.rg + bordersHalfPixel, 0, 0)).r;
	float newScore = tex2Dlod(bordersMapSampler, float4(newFly.rg + bordersHalfPixel, 0, 0)).r;

	oldFly.b = oldScore;
	newFly.b = newScore;

	return newScore >= oldScore;
}

float2 scanToBestDirection(float4 fly)
{
	float2 directions[8] =
	{
		float2(-1, -1),
		float2( 0, -1),
		float2( 1, -1),
		float2( 1,  0),
		float2( 1,  1),
		float2( 0,  1),
		float2(-1,  1),
		float2(-1,  0)
	};

	float2 maxDirection = (float2)0;

	float bestScore = 0;
	float currentScore;

	for(int i = 0;i<8;i++)
	{
		currentScore = tex2D(bordersMapSampler, fly.rg + directions[i] * bordersHalfPixel * 2).r;

		maxDirection = (currentScore > 0 && currentScore > bestScore) ? directions[i] : maxDirection;
		bestScore = max(bestScore, currentScore);
	}

	return maxDirection;
}

void PS_Grow(VSO input, out float4 color : COLOR0)
{
	float4 oldFly = tex2D(previousPopulationMapSampler, input.TexCoord + fliesHalfPixel);

	float3 noiseData = tex2D(noiseMapSampler, input.TexCoord + oldFly.xy + float2(randomNoiseOffset, randomNoiseOffset));

	float2 bestDirection = scanToBestDirection(oldFly);

	if(length(bestDirection) > 0)
	{
		oldFly.rg += bordersHalfPixel * 2 * bestDirection;
		oldFly.b = tex2Dlod(bordersMapSampler, float4(oldFly.rg + bordersHalfPixel, 0, 0)).r;

		color = oldFly;
	}
	else
	{
		float commonMultiply = 2;

		float2 noiseValue = (noiseData.xy - float2(0.7, 0.7));

		if(oldFly.b <= 0)
			commonMultiply *= (int)(6 * noiseValue.x);

		float2 noisePixelDirection = /* float2(fliesHalfPixel.x * 2, 0); */ noiseValue * 2 * commonMultiply * fliesHalfPixel * (noiseData.z > 0.5 ? 1 : -1); //((noiseData.xy - float2(0.5, 0.5)) * 2) * 2 * halfPixel;

		float dir = noisePixelDirection > 0 ? 0.1 : 0.9;

		color.rg = oldFly.xy + noisePixelDirection;

		color.r = ((color.r % 1) + 1) % 1;
		color.g = ((color.g % 1) + 1) % 1;

		if(!computeScore(oldFly, color))
			color = oldFly;
	}

	color.a = 1;
}

//Technique
technique Grow
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS_Grow();
	}
}


/* ---------------- DEBUG PLOT ------------- */

//Vertex Input Structure
struct VSI_PLOT
{
	float4 Position : POSITION0;
};

//Vertex Shader
VSO VS_PLOT(VSI_PLOT input)
{
	//Initialize Output
	VSO output;

	float4 fly = tex2Dlod(previousPopulationMapSampler, float4(input.Position.xy, 0, 1));

	output.Position = float4(2 * (float3(fly.r, 1 - fly.g, 0) - float3(0.5,0.5,0)), 1) + float4(2 * fliesHalfPixel * input.Position.z,0,0);
	output.TexCoord = fly.b > 0 ? float2(1,0) : float2(0,1);

	//Return
	return output;
}

float4 PS_PLOT(VSO input) : COLOR0
{
	return float4(1,1,1,1) * input.TexCoord.x + float4(0,0,0.5,1) * input.TexCoord.y;
}

technique Plot
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS_PLOT();
		PixelShader = compile ps_3_0 PS_PLOT();
	}
}