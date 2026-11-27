using UnityEngine;

public class VillageClockTower : MonoBehaviour
{
    [SerializeField] private Transform hourHand;
    [SerializeField] private Transform minuteHand;
    [SerializeField, Min(0.1f)] private float secondsPerClockHour = 60f;
    [SerializeField, Range(0, 11)] private int startingHour = 10;
    [SerializeField, Range(0, 59)] private int startingMinute = 10;
    private float clockMinutes;

    private void Awake()
    {
        clockMinutes = startingHour * 60 + startingMinute;
        PositionHands();
    }

    private void Update()
    {
        clockMinutes = Mathf.Repeat(clockMinutes + Time.deltaTime * 60f /
            Mathf.Max(0.1f, secondsPerClockHour), 720f);
        PositionHands();
    }

    private void PositionHands()
    {
        hourHand.localRotation = Quaternion.Euler(0f, 0f, -clockMinutes * 0.5f);
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, -clockMinutes * 6f);
    }
}
