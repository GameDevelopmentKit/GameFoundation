struct minimalVertexInput
{
    half4 position  : POSITION;
};

struct minimalVertexOutput
{
    half4 position  : POSITION;
    half2 texcoord : TEXCOORD0;
};

half2 VertexToUV(half2 vertex)
{    
    half2 texcoord = (vertex + 1.0) * 0.5; // triangle vert to uv
#if UNITY_UV_STARTS_AT_TOP
    texcoord = texcoord * half2(1.0, -1.0) + half2(0.0, 1.0);
#endif
    return texcoord;
}