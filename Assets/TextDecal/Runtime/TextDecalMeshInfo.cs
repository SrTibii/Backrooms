using System;
using JetBrains.Annotations;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using static space.chikalin.textdecal.TextDecal.TextDecalSettings;

namespace space.chikalin.textdecal
{
public class TextDecalMeshInfo
{

    private const int VolumetricVertexCount = 8;
    private const int VolumetricTrianglesCount = 36;

    private readonly int[] _volumetricTriangles =
    {
        0, 1, 2, //face front
        2, 3, 0,
        1, 5, 6, //face top
        6, 2, 1,
        3, 2, 6, //face right
        6, 7, 3,
        4, 5, 1, //face left
        1, 0, 4,
        4, 0, 3, //face back
        3, 7, 4,
        7, 6, 5, //face bottom
        5, 4, 7
    };


    private Vector3 _depthVector;
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector3[] _normals;
    private Vector4[] _tangents;
    private Vector4[] _extra;
    private Color[] _colors;
    private Vector4[] _uv;
    private Vector4[] _meshData;
    private Vector4[] _uvData;
    private Vector2[] _meshAngle;

    private Mesh _volumetricMesh;

    public Mesh VolumetricMesh
    {
        get
        {
            if (_volumetricMesh != null) return _volumetricMesh;
            _volumetricMesh = new Mesh
            {
                hideFlags = HideFlags.HideAndDontSave
            };
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _volumetricMesh.name = "Text Decal Mesh";
#endif
            _volumetricMesh.MarkDynamic();
            return _volumetricMesh;
        }
    }

    private MeshFilter _meshFilter;

    public Mesh Mesh
    {
        set => _meshFilter.mesh = value;
    }

    public void PrepareMeshData(int vertexCount, MeshFilter meshFilter)
    {
        _meshFilter = meshFilter;
        var size = vertexCount / 4;
        var vertexSize = size * VolumetricVertexCount;
        UpdateArray(ref _vertices, vertexSize);
        UpdateArray(ref _triangles, size * VolumetricTrianglesCount);
        UpdateArray(ref _colors, vertexSize);
        UpdateArray(ref _extra, vertexSize);
        UpdateArray(ref _normals, vertexSize);
        UpdateArray(ref _tangents, vertexSize);
        UpdateArray(ref _uv, vertexSize);
        UpdateArray(ref _meshData, vertexSize);
        UpdateArray(ref _meshAngle, vertexSize);
        UpdateArray(ref _uvData, vertexSize);
    }

    private static void UpdateArray<T>(ref T[] array, int size)
    {
        if (array == null)
        {
            array = new T[size];
            return;
        }

        if (array.Length != size)
        {
            Array.Resize(ref array, size);
        }

        Array.Clear(array, 0, array.Length);
    }
    
    
    /// <summary>
    /// Add volumetric mesh for a character
    /// </summary>
    /// <param name="charInfo">TextMesh character info</param>
    /// <param name="meshInfo">TextMesh mesh info</param>
    /// <param name="characterIndex">Character index</param>
    public void AddCharacter(TMP_CharacterInfo charInfo, TMP_MeshInfo meshInfo, 
        TextDecal.TextDecalSettings settings)
    {
        var vertexIndex = charInfo.vertexIndex;
        var volumetricVertexIndex = vertexIndex * 2;
        var triangleIndex = vertexIndex * 9;

        /*
         * copy TextMesh vertices
         */
        var bl = meshInfo.vertices[vertexIndex + 0];
        var tl = meshInfo.vertices[vertexIndex + 1];
        var tr = meshInfo.vertices[vertexIndex + 2];
        var br = meshInfo.vertices[vertexIndex + 3];
        _vertices[volumetricVertexIndex + 0] = bl;
        _vertices[volumetricVertexIndex + 1] = tl;
        _vertices[volumetricVertexIndex + 2] = tr;
        _vertices[volumetricVertexIndex + 3] = br;
        /*
         * Extrude face to make a cube
         */
        var normal = (Vector3)math.normalize(math.cross(bl - tl, bl - br));
        _vertices[volumetricVertexIndex + 4] = bl - normal * settings.projectionDepth;
        _vertices[volumetricVertexIndex + 5] = tl - normal * settings.projectionDepth;
        _vertices[volumetricVertexIndex + 6] = tr - normal * settings.projectionDepth;
        _vertices[volumetricVertexIndex + 7] = br - normal * settings.projectionDepth;
        var tangent = (Vector3)math.normalize(br - bl);
        /*
         * Fill volumetric triangles
         */
        for (var i = 0; i < _volumetricTriangles.Length; i++)
        {
            _triangles[triangleIndex + i] = volumetricVertexIndex + _volumetricTriangles[i];
        }

        /*
         * Copy TextMesh UVs
         */
        _uv[volumetricVertexIndex + 0] = charInfo.vertex_BL.uv;
        _uv[volumetricVertexIndex + 1] = charInfo.vertex_TL.uv;
        _uv[volumetricVertexIndex + 2] = charInfo.vertex_TR.uv;
        _uv[volumetricVertexIndex + 3] = charInfo.vertex_BR.uv;
        _uv[volumetricVertexIndex + 4] = charInfo.vertex_BL.uv;
        _uv[volumetricVertexIndex + 5] = charInfo.vertex_TL.uv;
        _uv[volumetricVertexIndex + 6] = charInfo.vertex_TR.uv;
        _uv[volumetricVertexIndex + 7] = charInfo.vertex_BR.uv;

        /*
         * Additional mesh data for decal shader
         */
        // character bottom left UV
        var lbUV = charInfo.vertex_BL.uv;

        // character vertex center, width
        var meshWidth = Vector3.Distance(br, bl);
        var center = bl + (tr - bl) / 2;
        var meshData = new Vector4(center.x, center.y, center.z, meshWidth);

        // character UV size
        var uvWidth = Vector2.Distance(charInfo.vertex_BR.uv, charInfo.vertex_BL.uv);
        var uvHeight = Vector2.Distance(charInfo.vertex_TR.uv, charInfo.vertex_BR.uv);
        var uvData = new Vector4(lbUV.x, lbUV.y, uvWidth, uvHeight / uvWidth);

        // In the shader to remap from cosine -1 to 1 to new range 0..1  (with 0 - 0 degree and 1 - 180 degree)
        // we do 1.0 - (dot() * 0.5 + 0.5) => 0.5 * (1 - dot())
        // we actually square that to get smoother result => x = (0.5 - 0.5 * dot())^2
        // Do a remap in the shader. 1.0 - saturate((x - start) / (end - start))
        // After simplification => saturate(a + b * dot() * (dot() - 2.0))
        // a = 1.0 - (0.25 - start) / (end - start), y = - 0.25 / (end - start)
        var extra = Vector4.zero;
        if (settings.startAngleFade < 180.0f) // angle fade is disabled
        {
            var angleStart = settings.startAngleFade / 180.0f;
            var angleEnd = settings.endAngleFade / 180.0f;
            var range = Mathf.Max(0.0001f, angleEnd - angleStart);
            extra = new Vector4(1.0f - (0.25f - angleStart) / range, -0.25f / range, settings.projectionDepth, 0f);
        }
        
        for (var i = 0; i < VolumetricVertexCount; i++)
        {
            _meshData[volumetricVertexIndex + i] = meshData;
            _uvData[volumetricVertexIndex + i] = uvData;
            _colors[volumetricVertexIndex + i] = charInfo.color;
            _normals[volumetricVertexIndex + i] = normal;
            _tangents[volumetricVertexIndex + i] = tangent;
            _extra[volumetricVertexIndex + i] = extra;
        }
    }

    public void UpdateMesh(TextDecal.TextDecalSettings settings)
    {
        var mesh = VolumetricMesh;
        mesh.Clear();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.normals = _normals;
        mesh.tangents = _tangents;
        mesh.colors = _colors;
        mesh.SetUVs(0, _uv);
        mesh.SetUVs((int) (settings.useDefaultUV ? vertexDataDefault : settings.vertexData), _meshData);
        mesh.SetUVs((int) (settings.useDefaultUV ? UVDataDefault : settings.UVData), _uvData);
        mesh.SetUVs((int) (settings.useDefaultUV ? extraDataDefault : settings.extraData), _extra);
        if (_meshFilter.sharedMesh != mesh)
        {
            _meshFilter.sharedMesh = mesh;
        }

        mesh.RecalculateBounds();
        mesh.MarkModified();
    }


    public void Dispose()
    {
        _volumetricMesh = null;
    }
}
}