using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsAutoFitter : MonoBehaviour
{
    // Parent object containing level assets, used to collect
    // renderer sizes to form a collective collider for the bounds
    public Transform levelRoot;
    public float padding = 0f;  // Extra space around the bounds
    public float minHeight = 12f;   // In case a level has small y axis bound
    Renderer[] renderers;
    BoxCollider2D box;

    [ContextMenu("Auto Stretch bounds to level")]
    void Fit()
    {
        renderers = levelRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);

        box = GetComponent<BoxCollider2D>();

        float width  = b.size.x + padding * 2f;
        float height = Mathf.Max(b.size.y + padding * 2f, minHeight);

        // Anchor the bottom edge so any extra height grows upward
        float bottom = b.min.y - padding;

        transform.position = new Vector3(b.center.x, bottom + height / 2f, 0f);
        box.offset = Vector2.zero;
        box.size = new Vector2(width, height);
    }
}