using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam_Follow : MonoBehaviour
{
    // Start is called before the first frame update
    public float followSpeed = 2f;
    public float yOffSet = 1f;
    public Transform target;
    void Start()
    {
        
    }

    // Runs after the character has moved for this frame.
    void LateUpdate()
    {
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y+yOffSet,-10f);
        transform.position = Vector3.Slerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
}
