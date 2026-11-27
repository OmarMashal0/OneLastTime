using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class WellBossEntrance : MonoBehaviour
{
    private static bool bossDefeated;

    [SerializeField] private BoxCollider2D player;
    private BoxCollider2D well;
    private float standingTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => bossDefeated = false;

    public static void LockAfterVictory() => bossDefeated = true;

    private void Awake()
    {
        well = GetComponent<BoxCollider2D>();
        if (bossDefeated) enabled = false;
    }

    private void FixedUpdate()
    {
        Bounds feet = player.bounds;
        Bounds rim = well.bounds;
        bool onRim = feet.max.x > rim.min.x && feet.min.x < rim.max.x
            && Mathf.Abs(feet.min.y - rim.max.y) < 0.075f
            && Mathf.Abs(player.attachedRigidbody.velocity.y) < 0.1f
            && player.IsTouching(well);
        standingTime = onRim ? standingTime + Time.fixedDeltaTime : 0f;
        if (standingTime < 3f || SceneFader.Instance.IsTransitioning) return;

        enabled = false;
        SceneFader.Instance.FadeToScene("Hidden_Rabbit_Boss");
    }
}
