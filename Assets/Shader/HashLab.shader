// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hash/HashLab"
{
    Properties
    {
        // Color property for material inspector, default to white
        _Color("Main Color", Color) = (1,1,1,1)
    }
        SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #pragma multi_compile_instancing

            // vertex shader
            // this time instead of using "appdata" struct, just spell inputs manually,
            // and instead of returning v2f struct, also just return a single output
            // float4 clip position
        float _Hashes[1024];
    //#pragma exclude_renderers gles
              float3 GetHashColor() {
    #if defined(UNITY_INSTANCING_ENABLED)
              uint hash = _Hashes[unity_InstanceID];
              return (1.0 / 255.0) * float3(
                  hash & 255,
                  (hash >> 8) & 255,
                  (hash >> 16) & 255
                  );
    #else
              return float3(0.5,0.2,0.1);
    #endif

          }

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

        // color from the material
        fixed4 _Color;

        // pixel shader, no inputs needed
        fixed4 frag() : SV_Target
        {
            return _Color; // just return it
        }
        ENDCG
    }
    }
}