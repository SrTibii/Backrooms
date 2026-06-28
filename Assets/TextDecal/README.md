# **Text Decal Documentation**
_`space.chikalin.textdecal`_

## **Description**
Text Decal is a tool for rendering text decals in Unity using the Universal Render Pipeline (URP). This asset allows you to leverage the advantages of both TextMeshPro and Decal Projector, making it an efficient solution for dynamic text rendering on any surface.

---

## **Quick Setup**
1. Add `Decal Renderer Feature` to your URP Renderer and set `DBuffer` or `Screen Space` mode.
2. Add `Text Decal Renderer Feature`.
3. Ensure the `TextMeshPro` package is installed and import `TextMeshPro Essentials`.
4. Create a custom `TMP_Font Asset` from a chosen font and apply the shader `Text Decal/Distance Field SSD [Unlit | URP Lit]` to it.
5. Add the `Rendering → Text Decal` component to a 3D TextMeshPro object and assign the custom font asset.
6. Configure the `Text Decal` component and text material, ensuring you set the `Projection Depth` properly.

---

## **Detailed Setup**

### **1. Configure Universal Render Pipeline (URP)**

- Open your `Universal Renderer Data` asset
  - _you can find the location through_ `Universal Renderer Pipeline Asset → Renderer List`.
- In the `Renderer Features` section: 
  - click `Add Renderer Feature → Decal`. 
  - click `Add Renderer Feature → Text Decal`.
- Set `Decal Technique` to either `DBuffer` or `Screen Space`.
    > ⚠️ **Note:** To use the `DBuffer` mode with `Text Decal Renderer`, enable it in the `Decal Renderer` and configure the settings accordingly. `Automatic` mode is not supported.

### **2. Set Up TextMesh Pro with Decal Shader**

1. **Create TMP_Font Asset:**
    - Select the font you want to use in the Project window.
    - Right-click the font and choose `Create → TextMeshPro → Font Asset → SDF`.
    - This will generate a `TMP_Font Asset`.

2. **Assign Decal Shader:**
    - Select the created `TMP_Font Asset`.
    - In the **Inspector**, locate the `Atlas & Material`, double click to the `Font Material` field value.
    - Click the shader dropdown and choose one of the following:
        - `Text Decal/Distance Field SSD Unlit` – Use for unlit rendering.
        - `Text Decal/Distance Field SSD URP Lit` – Use for lit rendering.

### **4. Using the Decal in the Scene**

1. Add a 3D TextMeshPro object to your scene
   - `Game Object → 3D Object → Text - TextMeshPro`.
2. Attach the `Text Decal` script to the object by clicking `Add Component` in the **Inspector**:
    - You can find the script at: `Packages/Text Decal/Runtime/TextDecal.cs`.
3. Assign the `TMP_Font Asset` you created in _Step 2_ with the shader applied 
   - by selecting it in the `Font Asset` property of the `TextMeshPro - Text` component.
4. Adjust `Projection Depth` and `Rendering Layers` in the **Inspector**:
    - `Projection Depth` controls how far the decal projection extends.
    - `Rendering Layers` allows you to control where the decal appears.

---

### **Setting Up Rendering Layers**

Rendering Layers determine which objects the decal will project onto. Follow these steps to configure them:

1. **Enable Rendering Layers in URP Settings:**
    - Go to `Universal Renderer Data` asset.
    - Ensure that `Use Rendering Layers` are enabled in the `Renderer Features → Decal` settings.

2. **Set Rendering Layers on Decal Object:**
    - Select the 3D TextMeshPro object with the `Text Decal` script attached.
    - In the **Inspector**, locate the material settings, in the `Rendering Layers` dropdown assign the desired rendering layer(s) for the decal.

3. **Assign Rendering Layers to Target Surfaces:**
    - Select the objects in your scene that you want the decal to project onto.
    - In the **Inspector**, locate the `Mesh Renderer → Additional Settings → Rendering Layer Mask` dropdown.
    - Ensure these objects are assigned to the same rendering layer(s) as the decal.

> **Tip:** Use unique layers to isolate specific decals to certain surfaces, making it easier to manage complex scenes.

---

## Angle Fade Support

**Angle Fade** controls the visibility of text decals based on the angle between the decal projection direction and the surface normal, helping decals naturally blend with the environment.

### How to Enable and Configure Angle Fade:

1. **Shader Setup**:
   - Ensure that the **Angle Fade** feature is enabled by checking the `Angle Fade` checkbox in the material settings.

2. **Component Configuration** (`TextDecal` script):
   - Adjust the **Angle Fade Start** and **Angle Fade End** values (both are in degrees, 0°–180°).
   - These values define a range of angles:
      - **Angle Fade Start**: The angle (in degrees) where the decal begins to fade out.
      - **Angle Fade End**: The angle (in degrees) where the decal becomes fully invisible.

### Example:
- **Start = 60°, End = 80°**:
   - The decal remains fully visible for surfaces facing within 0°–60° relative to the projection.
   - Between 60° and 80°, the decal gradually fades out.
   - Beyond 80°, the decal becomes completely transparent.

### Important Notes:
- Setting **Start** and **End** too close together will create a sharp cutoff.
- Setting **Start** much lower than **End** results in a smoother and more gradual fade.

> 🔥 Tip: Use Angle Fade to prevent text decals from unnaturally "climbing" onto sharp edges or being visible from unrealistic viewing angles.

---

## Calculate Rotation Support

**Calculate Rotation** ensures that text decals correctly align and project when individual characters in a TextMesh Pro object are manually moved or rotated — for example, to create curved, spline-following, or animated text effects.

### How to Use Calculate Rotation:

1. **Shader Setup**:
   - In the material settings, enable the `Calculate Rotation` checkbox.

2. **When to Enable**:
   - You should enable `Calculate Rotation` if you manually modify the vertices of TextMesh Pro at runtime or in editor scripts, especially when:
      - Text is bent or curved along a spline.
      - Characters are individually rotated for effects like waving or spiraling.
      - Custom animation scripts deform the text mesh.

Without this setting, decals may project incorrectly because the system will assume default upright character orientations.

### Important Execution Order Requirements:

- The `TextDecal` script has `[DefaultExecutionOrder(-80)]`, which means it updates at a specific point in Unity’s frame execution order.
- **Any modifications to the TextMesh Pro mesh (vertices, rotations, etc.) must be applied before execution order -80.**
   - This ensures that `TextDecal` captures the final, modified state of the mesh when calculating decal projection.

If you modify text after execution order -80 (e.g., in `LateUpdate` or later), the decal projection will not reflect those changes correctly.

> ⚡ Tip: If you're writing your own scripts that animate or modify TextMesh Pro geometry, you can set `[DefaultExecutionOrder(-90)]` on your scripts to guarantee the correct update timing relative to `TextDecal`.

---

## **Known Limitations:**

- `GBuffer` Decal technique are not supported yet.
- Some **TextMesh Pro** features are not available: italic text, underline, strikethrough, glow

---

## **Support and Feedback:**  
  If you encounter any issues or have feature requests, please contact the developer.