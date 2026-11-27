using UnityEngine;

public class Cam_Follow : MonoBehaviour
{
    public float followSpeed = 2f;
    public float yOffSet = 1f;
    public Transform target;

    private const float PanDuration = 0.85f;
    private Transform focus;
    private Vector3 panStart;
    private float panElapsed = PanDuration;

    public void SetFocus(Transform destination)
    {
        focus = destination;
        panStart = transform.position;
        panElapsed = 0f;
    }

    // Runs after the character has moved for this frame.
    void LateUpdate()
    {
        Vector3 targetPosition = focus != null
            ? new Vector3(focus.position.x, focus.position.y, -10f)
            : new Vector3(target.position.x, target.position.y + yOffSet, -10f);

        if (panElapsed < PanDuration)
        {
            panElapsed += Time.deltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, panElapsed / PanDuration);
            transform.position = Vector3.Lerp(panStart, targetPosition, blend);
            return;
        }

        if (focus != null)
        {
            transform.position = targetPosition;
            return;
        }

        transform.position = Vector3.Slerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
}
