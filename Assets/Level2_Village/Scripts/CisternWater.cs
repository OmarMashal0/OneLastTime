using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class CisternWater : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0f)] private float framesPerSecond = 7f;
    private SpriteRenderer water;
    private float frame;

    private void Awake() => water = GetComponent<SpriteRenderer>();

    private void Update()
    {
        if (frames.Length == 0) return;
        frame = Mathf.Repeat(frame + framesPerSecond * Time.deltaTime, frames.Length);
        water.sprite = frames[(int)frame];
    }
}
