using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hearthhold.UnityClient
{
    // One lit, vertex-coloured mesh keeps the playable ground flat while breaking up the old solid-green slab.
    // Fine grass blades read as black chevrons at the game's camera scale, so the surface stays deliberately quiet.
    internal sealed class MeadowSurface : MonoBehaviour
    {
        private Mesh generatedMesh;
        private Material generatedMaterial;

        public static void Create(Transform parent)
        {
            GameObject surface = new GameObject("Painted meadow surface");
            surface.transform.SetParent(parent, false);
            MeadowSurface owner = surface.AddComponent<MeadowSurface>();
            owner.Build();
        }

        private void Build()
        {
            const int cells = 80;
            const float spacing = 0.5f;
            var vertices = new List<Vector3>((cells + 1) * (cells + 1));
            var colors = new List<Color32>(vertices.Capacity);
            var triangles = new List<int>(cells * cells * 6);
            for (int z = 0; z <= cells; z++)
            for (int x = 0; x <= cells; x++)
            {
                float px = x * spacing, pz = z * spacing;
                float broad = Mathf.PerlinNoise(px * 0.105f + 13.1f, pz * 0.105f + 7.3f);
                float detail = Mathf.PerlinNoise(px * 0.42f + 44.7f, pz * 0.42f + 29.2f);
                float variation = (broad - 0.5f) * 0.30f + (detail - 0.5f) * 0.10f;
                vertices.Add(new Vector3(px, -0.004f, pz));
                colors.Add(Color.Lerp(new Color32(96, 128, 77, 255), new Color32(116, 149, 87, 255),
                    Mathf.Clamp01(0.46f + variation * 1.6f)));
            }
            for (int z = 0; z < cells; z++)
            for (int x = 0; x < cells; x++)
            {
                int a = z * (cells + 1) + x, b = a + 1, c = a + cells + 1, d = c + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }

            generatedMesh = new Mesh { name = "Painted meadow surface", indexFormat = IndexFormat.UInt32 };
            generatedMesh.SetVertices(vertices);
            generatedMesh.SetColors(colors);
            generatedMesh.SetTriangles(triangles, 0);
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = generatedMesh;
            Shader shader = Shader.Find("Hearthhold/VertexLit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            generatedMaterial = new Material(shader) { name = "Meadow vertex colour" };
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = generatedMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void OnDestroy()
        {
            if (generatedMesh != null) Destroy(generatedMesh);
            if (generatedMaterial != null) Destroy(generatedMaterial);
        }
    }
}
