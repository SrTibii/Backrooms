uniform sampler2D	_FaceTex;					// Alpha : Signed Distance
uniform float		_FaceUVSpeedX;
uniform float		_FaceUVSpeedY;
uniform float4		_FaceColor;					// RGBA : Color + Opacity
uniform float		_FaceDilate;				// v[ 0, 1]
uniform float		_OutlineSoftness;			// v[ 0, 1]

uniform float4		_OutlineColor;				// RGBA : Color + Opacity
uniform float		_OutlineWidth;				// v[ 0, 1]

// API Editable properties
uniform float 		_ShaderFlags;
uniform float		_WeightNormal;
uniform float		_WeightBold;

uniform float		_ScaleRatioA;
uniform float		_ScaleRatioB;
uniform float		_ScaleRatioC;

uniform float		_VertexOffsetX;
uniform float		_VertexOffsetY;

// Font Atlas properties
uniform sampler2D	_MainTex;
uniform float		_TextureWidth;
uniform float		_TextureHeight;
uniform float 		_GradientScale;
uniform float		_ScaleX;
uniform float		_ScaleY;
uniform float		_PerspectiveFilter;
uniform float		_Sharpness;

uniform float		_NormalBlend;						// v[ 0, 1]
uniform float		_Bevel;						// v[ 0, 1]
uniform float		_BevelOffset;				// v[-1, 1]
uniform float		_BevelWidth;				// v[-1, 1]
uniform float		_BevelClamp;				// v[ 0, 1]
uniform float		_BevelRoundness;			// v[ 0, 1]

// decal
float _Smoothness;
float _Metallic;
float _DecalLayerMaskFromDecal;
float _DrawOrder;

// uniform int _UIVertexColorAlwaysGammaSpace;