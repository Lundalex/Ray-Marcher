static const float PI = 3.1415926;

void ApplyTransformTriVertices(float3 rot, inout float3 a, inout float3 b, inout float3 c)
{
    float cosX = cos(rot.x);
    float sinX = sin(rot.x);
    float cosY = cos(rot.y);
    float sinY = sin(rot.y);
    float cosZ = cos(rot.z);
    float sinZ = sin(rot.z);

    // Combine rotation matrices into a single matrix
    float3x3 rotationMatrix = float3x3(
        cosY * cosZ,                             cosY * sinZ,                           -sinY,
        sinX * sinY * cosZ - cosX * sinZ,   sinX * sinY * sinZ + cosX * cosZ,  sinX * cosY,
        cosX * sinY * cosZ + sinX * sinZ,   cosX * sinY * sinZ - sinX * cosZ,  cosX * cosY
    );

    // Apply the combined rotation matrix to each vertex
    a = mul(rotationMatrix, a);
    b = mul(rotationMatrix, b);
    c = mul(rotationMatrix, c);
}

float sqr(float a)
{
	return a * a;
}

float dot2(float3 a) // float3 version
{
    return dot(a, a);
}
float dot2(float2 a) // float2 version
{
    return dot(a, a);
}

uint NextRandom(inout uint state)
{
    state = state * 747796405 + 2891336453;
    uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
    result = (result >> 22) ^ result;
    return result;
}

float randNormalized(inout uint state)
{
    return NextRandom(state) / 4294967295.0; // 2^32 - 1
}

float randValueNormalDistribution(inout uint state)
{
    float theta = 2 * PI * randNormalized(state);
    float rho = sqrt(-2 * log(randNormalized(state)));
    return rho * cos(theta);
}

// Expensive!
float3 randPointOnUnitSphere(inout uint state)
{
    float x = randValueNormalDistribution(state);
    float y = randValueNormalDistribution(state);
    float z = randValueNormalDistribution(state);
    return normalize(float3(x, y, z));
}

float2 randPointInCircle(inout uint state)
{
    float angle = randNormalized(state) * 2 * PI;
    float2 pointOnCircle = float2(cos(angle), sin(angle));
    return pointOnCircle * sqrt(randNormalized(state));
}