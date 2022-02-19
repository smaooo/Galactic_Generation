//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
//

#if defined(UNITY_INSTANCING_ENABLED)
StructuredBuffer<uint> _Hashes;
#endif

//#pragma surface surf Standard addshadow fullforwardshadows
#pragma multi_compile_instancing
//#pragma instancing_options procedural:setup
#pragma target 3.0

float3 GetHashColor() {
#if defined(UNITY_INSTANCING_ENABLED)
	uint hash = _Hashes[unity_InstanceID];
	return (1.0 / 255.0) * float3(
		hash & 255,
		(hash >> 8) & 255,
		(hash >> 16) & 255
		);
#else
	return 0.0;
#endif

}

void ShaderGraphFunction_float(out float3 Color) {
	Color = GetHashColor();
}

void ShaderGraphFunction_half(out half3 Color) {
	Color = GetHashColor();
}


void Hash_float(out float hash) {
#if defined(UNITY_INSTANCING_ENABLED)
	hash = _Hashes[unity_InstanceID];
	
#else
	hash = 1;
#endif
}