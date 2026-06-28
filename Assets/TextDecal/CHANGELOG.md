# Changelog

## [1.2.6] - `2026-06-12`

### Fixed
- **Projection Depth Clipping**:
  - Fixed an issue where text decals could render beyond the configured Projection Depth.

## [1.2.5] - `2026-03-26`

### Fixed
- **Unity 6.4 Compatibility**:
  - Updated renderer feature implementation to comply with Unity 6.4 API changes.

## [1.2.4] - `2025-08-19`

### Fixed
- **Multiple Decals with Dynamic Batching**:
  - Fixed incorrect rendering when multiple text decals were present in the same scene with Dynamic Batching enabled.
- **SRP Batching Compatibility**:
  - Fixed shader issues preventing correct operation with SRP Batching.
- **Gamma Color Handling**:
  - Fixed incorrect vertex color processing in Gamma color space, ensuring consistent decal colors across Gamma and Linear workflows.

## [1.2.3] - `2025-06-25`

### Fixed
- **Vertex Color Handling**:
  - Fixed incorrect vertex color rendering under Linear color space.

## [1.2.2] - `2025-06-09`

### Fixed
- **Editor Preview Error**:
  - Fixed an error occurring during asset preview generation in the Unity Editor (e.g., in the Project window).

## [1.2.1] - `2025-06-02`

### Added
- **Unity 6.1+ Clustered Lighting Compatibility**:
  - Added `#define _CLUSTER_LIGHT_LOOP` to support Clustered Light Loop in URP 16+ (Unity 6.1 and above).
  - Ensures correct lighting behavior when using Forward+ rendering path with clustered lights.

### Fixed
- **Shader Import Typo**:
  - Fixed a typo in an include directive in the shader that could cause compilation issues on some platforms.

## [1.2.0] - `2025-05-02`

### Added
- **Angle Fade Support**:
  - Added `Angle Fade` option to the shader and `TextDecal` component.
  - Allows decals to smoothly fade out based on surface angle, configurable via Start and End angles (0°–180°).

- **Calculate Rotation Support**:
  - Added `Calculate Rotation` option to the shader for correct projection when individual TextMesh Pro characters are rotated or repositioned manually.
  - Useful for curved, spline-following, or animated text effects.

### Fixed
- **Decal Bleeding Correction**:
  - Improved decal projection to reduce bleeding artifacts beyond intended surfaces for `DBuffer` mode.

## [1.1.1] - `2025-04-09`

### Fixed
- **Component Initialization**: Fixed an issue where the `TextDecal` component was not initializing correctly when added via script at runtime.

## [1.1.0] - `2025-02-24`

### Added
- **Text Decal Renderer Feature**:
  - No longer requires `Depth Texture` setup.
  - Text decals are now always rendered after the `URP Decal Projector`.
- **Emissive Pass**: Added for `DBuffer` mode.
- **Normal Reconstruction**: Added support for better normal handling.
- **Bevel Normal Generation**: Added support for generating bevel normals for more realistic shading.
- **TextMeshPro Font Fallback**: Added support for TMP font fallback and submeshes.

### Fixed
- **Lighting Calculation**: Fixed and improved the lighting calculation, now supporting **smoothness** and **metallic** properties.
- **WebGL Shader Compilation**: Fixed a shader compilation error preventing correct rendering on WebGL.

### Changed
- **Shader Renaming**: All shaders have been renamed for better clarity and consistency.


## [1.0.0] - `2025-02-06`

- Initial release.