
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

#define MAXIMUM_CONSIDERED_DEPTH 2000

#define MINIMUM_KINECT_LITERAL_DEPTH 800

/* it's somewhat larger, but we'll take 4000 has the maximum */
#define MAXIMUM_KINECT_LITERAL_DEPTH 4000


float visibleRange(float v, float r)
{
	return ((v > 0 && v < MAXIMUM_CONSIDERED_DEPTH) && (r > 0)) ? 1 : 0;
}

//Pixel Shader
float4 PS_Detect(VSO input) : COLOR0
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
	float nonZeroCount = 0;

	float med = 0;

	for(int i = 0;i<6;i++)
	{
		vX = tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples[i].xy)).r;
		vY = tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples[i].yx)).r;

		med += vX + vY;

		hasZero = hasZero || (vX == 0 || vY == 0);

		nonZeroCount += (vX == 0 ? 0 : 1) + (vY == 0 ? 0 : 1);

		accX += vX * samples[i].z;
		accY += vY * samples[i].z;
	}

	med = nonZeroCount > 1 ? (med / nonZeroCount) : MAXIMUM_KINECT_LITERAL_DEPTH;
	
	med = min(MAXIMUM_KINECT_LITERAL_DEPTH, med);
	
	med = med == 0 ? MAXIMUM_KINECT_LITERAL_DEPTH : med;

	med = med - MINIMUM_KINECT_LITERAL_DEPTH;

	med /= MAXIMUM_CONSIDERED_DEPTH;

	med = min(med, 1);

	med = 1 - med;

	//return float4(med, 0, 0, 1);

	//return float4(((accY + accX) / 2), 0, 0, 1);

	float v = (tex2D(depthSampler, input.TexCoord).r);

	//return float4(v, 0, 0, 1);

	//return float4((v > 0 && v < 16000) ? 1 : 0, 0, 0, 1);

	float r = sqrt(accX * accX + accY * accY);

	r = 200;

	//return float4((v > 0.5 ? 1 : 0) * (v / 0.5), 0, 0, 1);// float4((hasZero && (r > 0)) ? 1 : 0, 0, 0, 1);

	return float4(visibleRange(v, r) * med * ((hasZero || r > 150) ? 1 : 0),0,0,1);
	//return float4((v > 0.5 ? 1 : 0) * ((r > 60 && (hasZero && r > 0)) ? 1 : 0),0,0,1);

	//return float4(r / 60, 0, 0, 1); 
}

//Technique
technique Detect
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS_Detect();
	}
}

/* To increase the size of borders and downsize the texture */

static const float g_vOffsets[5] = {-2, -1, 0, 1, 2};

float sum3(float3 s)
{
	return dot(s, float3(1,1,1));
}

float minimumDepthOffset = 0;
float maximumDepth = 1;

int categorizeDepthBased(float depth)
{
	depth = (1 - depth) - minimumDepthOffset;
	float localMaximumDepth = min(1, maximumDepth);
	depth /= localMaximumDepth;

	if(depth <= 0.1)
		return 0;
	else if(depth <= 0.3)
		return 1;
	else if(depth <= 0.6)
		return 2;
	else
		return 3;
}

float4 PS_Down(VSO input) : COLOR0
{ 
	float vMed = 0;

	[unroll]
	for (int x = 0; x < 5; x++)
	{
		[unroll]
		for (int y = 0; y < 5; y++)
		{
			float2 vOffset;
			vOffset = float2(g_vOffsets[x], g_vOffsets[y]) * 2 * halfPixel;

			float vSample = tex2D(depthSampler, input.TexCoord + vOffset).r;
			
			vMed = max (vMed, vSample);
		}
	}

	/* reduced to LEVEL_NUMBERS levels */
	vMed = categorizeDepthBased(vMed);

	/* Only taking the closer sections */
	return vMed <= 0 ? float4(1,1,1,1) : float4(0,0,0,1);

	/*
	if(vMed == 0)
		return float4(0,0,1,1);
	else if(vMed == 1)
		return float4(1,0,0,1);
	else if(vMed == 2)
		return float4(0,1,0,1);
	else if(vMed == 3)
		return float4(0,0,0,1);
	else
		return float4(1,1,1,1);
	*/

	//return float4(vMed, 0, 0, 1); // (sum3(vColor / 16.0f) > 1) ? float4(1,1,1,1) : float4(0,0,0,1);
}

technique Down
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS_Down();
	}
}

float3 SolidFillColor;

float4 PS_SolidFill() : COLOR0
{
	return float4(SolidFillColor, 1);
}

technique SolidFill
{
	pass P0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS_SolidFill();
	}
}

//Pixel Shader
float4 PS_Grad(VSO input) : COLOR0
{ 
	float3 samples_vert[6] = 
	{
		float3(-1,-1,	-1),
		float3(-1, 0,	-2),
		float3(-1, 1,	-1),

		float3( 1,-1,	 1),
		float3( 1, 0,	 2),
		float3( 1, 1,	 1)
	};

	float3 samples_diag[6] = 
	{
		float3(-1, 0,	-1),
		float3(-1,-1,	-2),
		float3( 0,-1,	-1),

		float3( 0, 1,	 1),
		float3( 1, 1,	 2),
		float3( 1, 0,	 1)
	};

	float dUp, dHorz, dDiagUpLeft, dDiagUpRight;

	dUp = dHorz = dDiagUpLeft = dDiagUpRight = 0;

	for(int i = 0;i<6;i++)
	{
		dUp += tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples_vert[i].xy)).r * samples_vert[i].z;
		dHorz += tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples_vert[i].yx)).r * samples_vert[i].z;

		dDiagUpLeft += tex2D(depthSampler, input.TexCoord + (2 * halfPixel * samples_diag[i].xy)).r * samples_diag[i].z;
		dDiagUpRight += tex2D(depthSampler, input.TexCoord + (2 * halfPixel * float2(samples_diag[i].y, -samples_diag[i].x))).r * samples_diag[i].z;
	}

	dUp = abs(dUp);
	dHorz = abs(dHorz);
	dDiagUpLeft = abs(dDiagUpLeft);
	dDiagUpRight = abs(dDiagUpRight);

	float m = max(dUp, max(dHorz, max(dDiagUpLeft, dDiagUpRight)));

	if(m == 0 || (dUp == dHorz && dUp == dDiagUpLeft && dUp == dDiagUpRight))
		return float4(0,0,0,1);
	else if(m == dUp)
		return float4(1,0,0,1);
	else if(m == dHorz)
		return float4(0,1,0,1);
	else if(m == dDiagUpLeft)
		return float4(0,0,1,1);
	else
		return float4(1,0,1,1);
}

technique Grad
{
	pass P0
	{
		VertexShader = compile vs_3_0 VS();
		PixelShader = compile ps_3_0 PS_Grad();
	}
}
