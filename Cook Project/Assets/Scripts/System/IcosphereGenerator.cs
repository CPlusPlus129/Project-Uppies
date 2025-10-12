using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates procedural icosphere meshes for 3D light radius visualization.
/// Based on icosahedron subdivision algorithm for even vertex distribution.
/// </summary>
public static class IcosphereGenerator
{
    private struct TriangleIndices
    {
        public int v1;
        public int v2;
        public int v3;

        public TriangleIndices(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    /// <summary>
    /// Creates an icosphere mesh with specified radius and subdivision level.
    /// </summary>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="subdivisions">Number of subdivisions (0-3 recommended, higher = more detail but more vertices)</param>
    /// <returns>Generated mesh</returns>
    public static Mesh Create(float radius, int subdivisions = 1)
    {
        // Clamp subdivisions to prevent excessive vertex counts
        subdivisions = Mathf.Clamp(subdivisions, 0, 3);
        
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();
        
        // Create initial icosahedron
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
        
        vertices.Add(new Vector3(-1, t, 0).normalized * radius);
        vertices.Add(new Vector3(1, t, 0).normalized * radius);
        vertices.Add(new Vector3(-1, -t, 0).normalized * radius);
        vertices.Add(new Vector3(1, -t, 0).normalized * radius);
        
        vertices.Add(new Vector3(0, -1, t).normalized * radius);
        vertices.Add(new Vector3(0, 1, t).normalized * radius);
        vertices.Add(new Vector3(0, -1, -t).normalized * radius);
        vertices.Add(new Vector3(0, 1, -t).normalized * radius);
        
        vertices.Add(new Vector3(t, 0, -1).normalized * radius);
        vertices.Add(new Vector3(t, 0, 1).normalized * radius);
        vertices.Add(new Vector3(-t, 0, -1).normalized * radius);
        vertices.Add(new Vector3(-t, 0, 1).normalized * radius);
        
        // Create 20 triangles of the icosahedron
        List<TriangleIndices> faces = new List<TriangleIndices>();
        
        // 5 faces around point 0
        faces.Add(new TriangleIndices(0, 11, 5));
        faces.Add(new TriangleIndices(0, 5, 1));
        faces.Add(new TriangleIndices(0, 1, 7));
        faces.Add(new TriangleIndices(0, 7, 10));
        faces.Add(new TriangleIndices(0, 10, 11));
        
        // 5 adjacent faces
        faces.Add(new TriangleIndices(1, 5, 9));
        faces.Add(new TriangleIndices(5, 11, 4));
        faces.Add(new TriangleIndices(11, 10, 2));
        faces.Add(new TriangleIndices(10, 7, 6));
        faces.Add(new TriangleIndices(7, 1, 8));
        
        // 5 faces around point 3
        faces.Add(new TriangleIndices(3, 9, 4));
        faces.Add(new TriangleIndices(3, 4, 2));
        faces.Add(new TriangleIndices(3, 2, 6));
        faces.Add(new TriangleIndices(3, 6, 8));
        faces.Add(new TriangleIndices(3, 8, 9));
        
        // 5 adjacent faces
        faces.Add(new TriangleIndices(4, 9, 5));
        faces.Add(new TriangleIndices(2, 4, 11));
        faces.Add(new TriangleIndices(6, 2, 10));
        faces.Add(new TriangleIndices(8, 6, 7));
        faces.Add(new TriangleIndices(9, 8, 1));
        
        // Refine triangles
        for (int i = 0; i < subdivisions; i++)
        {
            List<TriangleIndices> faces2 = new List<TriangleIndices>();
            foreach (var tri in faces)
            {
                // Replace triangle by 4 triangles
                int a = GetMiddlePoint(tri.v1, tri.v2, ref vertices, ref middlePointIndexCache, radius);
                int b = GetMiddlePoint(tri.v2, tri.v3, ref vertices, ref middlePointIndexCache, radius);
                int c = GetMiddlePoint(tri.v3, tri.v1, ref vertices, ref middlePointIndexCache, radius);
                
                faces2.Add(new TriangleIndices(tri.v1, a, c));
                faces2.Add(new TriangleIndices(tri.v2, b, a));
                faces2.Add(new TriangleIndices(tri.v3, c, b));
                faces2.Add(new TriangleIndices(a, b, c));
            }
            faces = faces2;
        }
        
        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = $"Icosphere_R{radius}_Sub{subdivisions}";
        
        mesh.vertices = vertices.ToArray();
        
        // Build triangle array
        int[] triangles = new int[faces.Count * 3];
        for (int i = 0; i < faces.Count; i++)
        {
            triangles[i * 3] = faces[i].v1;
            triangles[i * 3 + 1] = faces[i].v2;
            triangles[i * 3 + 2] = faces[i].v3;
        }
        mesh.triangles = triangles;
        
        // Calculate normals
        Vector3[] normals = new Vector3[vertices.Count];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = vertices[i].normalized;
        }
        mesh.normals = normals;
        
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    /// <summary>
    /// Gets or creates a middle point between two vertices, normalized to sphere radius.
    /// Uses caching to avoid duplicate vertices.
    /// </summary>
    private static int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius)
    {
        // Check if we have it cached
        bool firstIsSmaller = p1 < p2;
        long smallerIndex = firstIsSmaller ? p1 : p2;
        long greaterIndex = firstIsSmaller ? p2 : p1;
        long key = (smallerIndex << 32) + greaterIndex;
        
        if (cache.TryGetValue(key, out int ret))
        {
            return ret;
        }
        
        // Not in cache, calculate it
        Vector3 point1 = vertices[p1];
        Vector3 point2 = vertices[p2];
        Vector3 middle = new Vector3(
            (point1.x + point2.x) / 2f,
            (point1.y + point2.y) / 2f,
            (point1.z + point2.z) / 2f
        );
        
        // Add vertex makes sure point is on unit sphere
        int i = vertices.Count;
        vertices.Add(middle.normalized * radius);
        
        // Store it, return index
        cache.Add(key, i);
        return i;
    }
}
