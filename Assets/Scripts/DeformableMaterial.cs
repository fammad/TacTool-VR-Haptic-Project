using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class DeformableMaterial : MonoBehaviour
{
    public enum MaterialType { Wood, Metal, Marble, Clay }

    [Header("General Settings")]
    [SerializeField] private MaterialType materialType = MaterialType.Wood;
    [SerializeField] private float resistance = 1.0f; 
    [SerializeField] private float maxDeformation = 0.2f;
    [SerializeField] private float deformationRadius = 0.15f;
    
    [Header("Mesh Settings")]
    [SerializeField] private bool autoSubdivide = true;
    [SerializeField] private int subdivisionResolution = 20;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    
    private Dictionary<int, List<int>> vertexGroups = new Dictionary<int, List<int>>();
    private Vector3 accumulatedBendOffset = Vector3.zero;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        
        if (autoSubdivide && (mf.sharedMesh == null || mf.sharedMesh.vertexCount <= 24))
        {
            float size = mf.sharedMesh != null ? mf.sharedMesh.bounds.size.x : 1f;
            mf.mesh = SubdividedCubeGenerator.Generate(subdivisionResolution, size);
            Debug.Log($"[Deformable] Procedural subdivision generated for {gameObject.name} ({mf.mesh.vertexCount} vertices)");
        }

        mesh = mf.mesh;
        originalVertices = mesh.vertices;
        modifiedVertices = (Vector3[])originalVertices.Clone();

        BuildVertexGroups();
    }

    private void BuildVertexGroups()
    {
        vertexGroups.Clear();
        for (int i = 0; i < originalVertices.Length; i++)
        {
            bool found = false;
            foreach (var key in vertexGroups.Keys)
            {
                if (Vector3.Distance(originalVertices[i], originalVertices[key]) < 0.001f)
                {
                    vertexGroups[key].Add(i);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                vertexGroups[i] = new List<int> { i };
            }
        }
    }

    public void ApplyDeformation(Vector3 worldPoint, Vector3 worldForce, bool isFiling = false)
    {
        Vector3 impactDirection = -worldForce; 
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 localForce = transform.InverseTransformDirection(impactDirection);

        switch (materialType)
        {
            case MaterialType.Metal:
                ApplyMetalBending(localForce, localPoint);
                break;
            case MaterialType.Marble:
                if (isFiling) ApplyMarbleSmoothing(localPoint, localForce);
                else ApplyMarbleCracking(localPoint, localForce);
                break;
            case MaterialType.Clay:
                ApplyOrganicClayMolding(localPoint, localForce, isFiling);
                break;
            case MaterialType.Wood:
                ApplyWoodIndentation(localPoint, localForce);
                break;
        }

        UpdateMesh();
    }

    private void MoveVertexGroup(int representativeIndex, Vector3 localDelta, float limitMultiplier = 1.0f)
    {
        foreach (int index in vertexGroups[representativeIndex])
        {
            modifiedVertices[index] += localDelta;
            
            Vector3 diff = modifiedVertices[index] - originalVertices[index];
            float limit = maxDeformation * limitMultiplier;
            if (diff.magnitude > limit)
                modifiedVertices[index] = originalVertices[index] + diff.normalized * limit;
        }
    }

    private void ApplyWoodIndentation(Vector3 localPoint, Vector3 localForce)
    {
        float forceScale = 0.015f / Mathf.Max(0.1f, resistance);
        Vector3 force = localForce * forceScale;

        foreach (var entry in vertexGroups)
        {
            int i = entry.Key;
            float dist = Vector3.Distance(localPoint, modifiedVertices[i]);
            if (dist < deformationRadius)
            {
                float falloff = Mathf.Pow((deformationRadius - dist) / deformationRadius, 2);
                MoveVertexGroup(i, force * falloff);
            }
        }
    }

    private void ApplyMetalBending(Vector3 localForce, Vector3 localImpactPoint)
    {
        Vector3 bendDir = new Vector3(localForce.x, 0, localForce.z);
        float leverage = Mathf.Clamp01(localImpactPoint.y); 
        accumulatedBendOffset += bendDir * (0.08f / resistance) * leverage;

        if (accumulatedBendOffset.magnitude > maxDeformation * 5.0f)
            accumulatedBendOffset = accumulatedBendOffset.normalized * (maxDeformation * 5.0f);

        foreach (var entry in vertexGroups)
        {
            int i = entry.Key;
            float h = originalVertices[i].y;
            float weight = Mathf.Pow(Mathf.Clamp01(h), 3.0f); 
            
            Vector3 targetPos = originalVertices[i] + accumulatedBendOffset * weight;
            Vector3 delta = targetPos - modifiedVertices[i];
            MoveVertexGroup(i, delta, 5.0f);
        }
    }

    private void ApplyMarbleCracking(Vector3 localPoint, Vector3 localForce)
    {
        float forceScale = 0.12f / Mathf.Max(0.1f, resistance);
        Vector3 force = localForce.normalized * forceScale;

        foreach (var entry in vertexGroups)
        {
            int i = entry.Key;
            float dist = Vector3.Distance(localPoint, modifiedVertices[i]);
            if (dist < deformationRadius * 0.6f)
            {
                float noise = Random.value > 0.5f ? Random.Range(1.0f, 2.5f) : Random.Range(-0.5f, 0.5f);
                Vector3 jitter = Random.onUnitSphere * 0.015f;
                MoveVertexGroup(i, (force + jitter) * noise);
            }
        }
    }

    private void ApplyMarbleSmoothing(Vector3 localPoint, Vector3 localForce)
    {
        foreach (var entry in vertexGroups)
        {
            int i = entry.Key;
            float dist = Vector3.Distance(localPoint, modifiedVertices[i]);
            if (dist < deformationRadius)
            {
                foreach (int index in entry.Value)
                    modifiedVertices[index] = Vector3.Lerp(modifiedVertices[index], originalVertices[index], 0.25f);
            }
        }
    }

    private void ApplyOrganicClayMolding(Vector3 localPoint, Vector3 localForce, bool isFiling)
    {
        float forceScale = (isFiling ? 0.25f : 0.15f) / Mathf.Max(0.1f, resistance);
        Vector3 force = localForce * forceScale;

        foreach (var entry in vertexGroups)
        {
            int i = entry.Key;
            float dist = Vector3.Distance(localPoint, modifiedVertices[i]);
            if (dist < deformationRadius)
            {
                float t = dist / deformationRadius;
                float bulge = Mathf.Sin(t * Mathf.PI); 
                float push = Mathf.Cos(t * Mathf.PI * 0.5f);
                
                Vector3 outward = (modifiedVertices[i] - localPoint).normalized * bulge * force.magnitude * 0.4f;
                MoveVertexGroup(i, (force * push) + outward, 4.0f);
            }
        }
    }

    private void UpdateMesh()
    {
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null)
        {
            // Optimization for Quest 3: Only update the collider if the deformation is significant 
            // or use a separate simplified physical mesh. For now, we force update but reset sharedMesh.
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
            
            // On Android/Quest, MeshCollider must be Convex to collide with other Rigidbodies (Tools)
            if (!mc.convex) mc.convex = true;
        }
        else
        {
            // Add if missing
            mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
    }
}