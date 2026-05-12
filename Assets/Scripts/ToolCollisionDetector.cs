using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Detects collisions between tools and specific material tags.
/// Calculates hit strength and manages cooldowns per object.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ToolCollisionDetector : MonoBehaviour
{
    public enum ToolType { Hammer, Chisel, File }

    [Header("Configuration")]
    [SerializeField] private ToolType toolType;
    [SerializeField] private float hitCooldown = 0.2f;
    [SerializeField] private float minVelocityThreshold = 0.1f;

    [Header("Events")]
    // Custom UnityEvent to pass tool, material, and strength to the inspector
    public ToolHitEvent onValidHit;

    // Track cooldowns per specific object hit
    private Dictionary<int, float> lastHitTimes = new Dictionary<int, float>();

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Filter by specific material tags
        string materialTag = collision.gameObject.tag;
        if (!IsValidMaterial(materialTag)) return;

        // 2. Check cooldown for this specific object
        int instanceId = collision.gameObject.GetInstanceID();
        if (IsOnCooldown(instanceId)) return;

        // 3. Calculate collision strength (velocity magnitude)
        float hitStrength = collision.relativeVelocity.magnitude;

        // Ignore very soft touches
        if (hitStrength < minVelocityThreshold) return;

        // 4. Update cooldown tracking
        lastHitTimes[instanceId] = Time.time;

        // 5. Fire event and log
        string toolName = toolType.ToString();
        onValidHit?.Invoke(toolName, materialTag, hitStrength);

        Debug.Log($"[ToolHit] {toolName} struck {materialTag} with strength: {hitStrength:F2}");
    }

    private bool IsValidMaterial(string tag)
    {
        return tag == "Wood" || tag == "Metal" || tag == "Clay" || tag == "Marble";
    }

    private bool IsOnCooldown(int id)
    {
        if (lastHitTimes.TryGetValue(id, out float lastTime))
        {
            return (Time.time - lastTime) < hitCooldown;
        }
        return false;
    }

    // Clean up dictionary periodically or when objects are destroyed if necessary.
    // For a simple VR scene, the dictionary size is usually negligible.
}

[System.Serializable]
public class ToolHitEvent : UnityEvent<string, string, float> { }
