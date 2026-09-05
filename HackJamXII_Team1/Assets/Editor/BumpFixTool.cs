using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Herramienta de un solo uso: sube los checkpoints (familia "Checkpoint 1...")
/// que quedan hundidos bajo los bultos de CIRCUIT_V1_low hasta la altura real
/// de la superficie en ese punto (X,Y), interpolando la propia malla.
/// No toca la escena actualmente abierta en disco hasta que el usuario guarde.
/// </summary>
public static class BumpFixTool
{
    private static readonly Regex CheckpointNameRegex = new Regex(@"^Checkpoint(?: \d+)?( ?\(\d+\))?$");

    // Pequeño margen por encima de la superficie interpolada, para que el
    // checkpoint quede apoyado ENCIMA del bulto y no exactamente coplanar
    // (evita z-fighting visual con la propia malla).
    private const float ClearanceLocal = 0.005f;

    [MenuItem("Tools/Bump Audit/Fix Sunken Checkpoints")]
    public static void Run()
    {
        var sb = new StringBuilder();
        string outPath = Path.Combine(Application.dataPath, "..", "bump_fix_output.txt");

        GameObject circuitRoot = GameObject.Find("CIRCUIT_V1_low");
        if (circuitRoot == null)
        {
            Debug.LogError("BumpFixTool: no se encontró 'CIRCUIT_V1_low' en la escena abierta.");
            return;
        }

        // Contenedor real de los checkpoints en uso (hijo directo, nombre EXACTO
        // "Checkpoints"; deliberadamente no toca "Checkpoints_OLD" ni nada del Canvas UI).
        Transform checkpointsContainer = circuitRoot.transform.Cast<Transform>()
            .FirstOrDefault(t => t.name == "Checkpoints");

        if (checkpointsContainer == null)
        {
            Debug.LogError("BumpFixTool: no se encontró el contenedor 'Checkpoints' bajo CIRCUIT_V1_low.");
            return;
        }

        // Recolectar triángulos de toda la malla visible bajo circuitRoot, en su espacio local.
        var meshFilters = new List<MeshFilter>();
        void Walk(Transform t)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) meshFilters.Add(mf);
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i));
        }
        Walk(circuitRoot.transform);

        var triangles = new List<Vector3[]>();
        foreach (var mf in meshFilters)
        {
            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            Matrix4x4 toCircuitLocal = circuitRoot.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            var localVerts = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
                localVerts[i] = toCircuitLocal.MultiplyPoint3x4(verts[i]);

            for (int i = 0; i + 2 < tris.Length; i += 3)
                triangles.Add(new[] { localVerts[tris[i]], localVerts[tris[i + 1]], localVerts[tris[i + 2]] });
        }

        sb.AppendLine($"Triángulos recolectados: {triangles.Count}");

        double SignedArea(Vector2 a, Vector2 b, Vector2 c) =>
            (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

        bool TryGetHeightAt(float x, float y, out float bestZ)
        {
            bestZ = float.NegativeInfinity;
            bool found = false;
            var p = new Vector2(x, y);
            foreach (var tri in triangles)
            {
                var a = new Vector2(tri[0].x, tri[0].y);
                var b = new Vector2(tri[1].x, tri[1].y);
                var c = new Vector2(tri[2].x, tri[2].y);

                double areaTotal = SignedArea(a, b, c);
                if (Math.Abs(areaTotal) < 1e-9) continue;

                double a1 = SignedArea(p, b, c);
                double a2 = SignedArea(a, p, c);
                double a3 = SignedArea(a, b, p);

                bool sameSign = (a1 >= -1e-6 && a2 >= -1e-6 && a3 >= -1e-6) ||
                                 (a1 <= 1e-6 && a2 <= 1e-6 && a3 <= 1e-6);
                if (!sameSign) continue;

                double u = a1 / areaTotal, v = a2 / areaTotal, w = a3 / areaTotal;
                float z = (float)(u * tri[0].z + v * tri[1].z + w * tri[2].z);

                found = true;
                if (z > bestZ) bestZ = z;
            }
            return found;
        }

        var checkpoints = checkpointsContainer.Cast<Transform>()
            .Where(t => CheckpointNameRegex.IsMatch(t.name))
            .ToList();

        sb.AppendLine($"Checkpoints en el contenedor 'Checkpoints': {checkpoints.Count}");

        int fixedCount = 0;
        foreach (var t in checkpoints)
        {
            Vector3 local = t.localPosition; // ya están en el espacio local de "Checkpoints" == espacio local de circuitRoot (transform identidad)
            bool found = TryGetHeightAt(local.x, local.y, out float surfaceZ);
            if (!found)
            {
                sb.AppendLine($"{t.name}\tSIN IMPACTO EN LA MALLA (no se toca)");
                continue;
            }

            if (surfaceZ > local.z + 0.001f)
            {
                float newZ = surfaceZ + ClearanceLocal;
                Undo.RecordObject(t, "Fix sunken checkpoint");
                t.localPosition = new Vector3(local.x, local.y, newZ);
                EditorUtility.SetDirty(t);
                sb.AppendLine($"{t.name}\tz: {local.z:F4} -> {newZ:F4}  (superficie={surfaceZ:F4})");
                fixedCount++;
            }
            else
            {
                sb.AppendLine($"{t.name}\tsin cambios (z={local.z:F4}, superficie={surfaceZ:F4})");
            }
        }

        sb.AppendLine($"=== Corregidos: {fixedCount} de {checkpoints.Count} ===");

        EditorSceneManager.MarkSceneDirty(circuitRoot.scene);

        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"BumpFixTool: {fixedCount} checkpoints corregidos. Detalle en {outPath}. La escena quedó marcada como modificada: recuerda guardarla (Ctrl+S) para persistir el cambio.");
    }
}
