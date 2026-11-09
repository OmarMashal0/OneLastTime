using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // What the cam will follow; the player
    public Collider2D bounds; // To keep the cam from seeing out of bounds, pun intended :3
    public float smoothTime = 0.15f; // Duration cam takes to reach target
    public Vector3 offset = new Vector3(0, 1f, -10f); // A small gap between the player and cam

    private Vector3 velocity; // This is where the velocity is kept track of
    Camera cam;

    void Awake()
    {
        // Grabs the camera
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        // The point cam will follow + offset point
        // And 
        Vector3 goal = target.position + offset;
        Vector3 pos = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);

        if (bounds != null) // To avoid crashing or undetermined behavior
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Bounds b = bounds.bounds;

            pos.x = Mathf.Clamp(pos.x, b.min.x + halfW, b.max.x - halfW);
            pos.y = Mathf.Clamp(pos.y, b.min.y + halfH, b.max.y - halfH);
        }

        transform.position = pos;
    }
}