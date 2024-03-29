static const int TN_RM = 8; // RayMarcher
static const int TN_PC = 512; // PreCalc
static const int TN_SS = 512; // SpatialSort
static const int TN_NG = 8; // NoiseGenerator
static const int TN_TC = 8; // TextureComposition

static const float FLT_MAX = 99999999999.0; // supplement for maximum float val

// Gaussian blur lookup
// Weight distribution is symmetric through the center
// [y=0][x=0] (upper right) contains the center weight
// 2x2x2
static const float GBLookup1_totWeight = 28.0;
static const float2x2 GBLookup1_z1 = float2x2(
    4.0,  2.0,
    2.0,  1.0
);
static const float2x2 GBLookup1_z2 = float2x2(
    2.0,  1.0,
    1.0,  0.0
);

// 3x3x3
static const float GBLookup2_totWeight = 2150.0;
static const float3x3 GBLookup2_z1 = float3x3(
    97.0, 64.0, 41.0,
    64.0, 17.0, 11.0,
    41.0, 11.0,  7.0
);
static const float3x3 GBLookup2_z2 = float3x3(
    64.0, 41.0, 26.0,
    41.0, 11.0,  7.0,
    26.0,  7.0,  4.0
);
static const float3x3 GBLookup2_z3 = float3x3(
    41.0, 26.0, 16.0,
    26.0,  7.0,  4.0,
    16.0,  4.0,  1.0
);

// 4x4x4
static const float GBLookup3_totWeight = 1590.0;
static const float4x4 GBLookup3_z1 = float4x4(
    159.0, 97.0, 22.0, 2.0,
    97.0,  59.0, 13.0, 1.0,
    22.0,  13.0, 3.0,  0.0,
    2.0,   1.0,  0.0,  0.0
);
static const float4x4 GBLookup3_z2 = float4x4(
    97.0, 22.0, 2.0, 0.0,
    22.0, 13.0, 1.0, 0.0,
    2.0,   1.0, 0.0, 0.0,
    0.0,   0.0, 0.0, 0.0
);
static const float4x4 GBLookup3_z3 = float4x4(
    22.0, 2.0, 0.0, 0.0,
    2.0,  1.0, 0.0, 0.0,
    0.0,  0.0, 0.0, 0.0,
    0.0,  0.0, 0.0, 0.0
);
static const float4x4 GBLookup3_z4 = float4x4(
    2.0, 0.0, 0.0, 0.0,
    0.0, 0.0, 0.0, 0.0,
    0.0, 0.0, 0.0, 0.0,
    0.0, 0.0, 0.0, 0.0
);

float WeightFromGBLookup(uint x, uint y, uint z, uint radius)
{
    if (radius == 1)
    {
        if (z == 0)
        {
            return GBLookup1_z1[y][x];
        }
        if (z == 1)
        {
            return GBLookup1_z2[y][x];
        }
    }
    else if (radius == 2)
    {
        if (z == 0)
        {
            return GBLookup2_z1[y][x];
        }
        if (z == 1)
        {
            return GBLookup2_z2[y][x];
        }
        if (z == 2)
        {
            return GBLookup2_z3[y][x];
        }
    }
    else if (radius == 3)
    {
        if (z == 0)
        {
            return GBLookup3_z1[y][x];
        }
        if (z == 1)
        {
            return GBLookup3_z2[y][x];
        }
        if (z == 2)
        {
            return GBLookup3_z3[y][x];
        }
        if (z == 3)
        {
            return GBLookup3_z4[y][x];
        }
    }
    return -999.0; // Error indication
}

float TotWeightFromGBLookup(uint radius)
{
    if (radius == 1) { return GBLookup1_totWeight; }
    if (radius == 2) { return GBLookup2_totWeight; }
    return GBLookup3_totWeight; // radius == 3
}