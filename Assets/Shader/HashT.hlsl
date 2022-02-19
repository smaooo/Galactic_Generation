//#pragma target 4.5

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


		  void ShaderGraphFunction_float(out float3 Color) {
			  Color = GetHashColor();
		  }

		  void ShaderGraphFunction_half(out half3 Color) {
			  Color = GetHashColor();
		  }

