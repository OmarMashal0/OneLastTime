using UnityEngine;

namespace Level3Bamboo
{
    // The sound of the river grows louder as Ryu walks toward the river exit at the end of the trail.
    public class RiverAmbienceFade : MonoBehaviour
    {
        public BambooPlayerController player;
        public AudioSource riverSound;
        public float fadeStartX = 146f;
        public float fadeEndX = 164f;
        [Range(0f, 1f)] public float fullVolume = 0.22f;

        // Update is called once per frame
        void Update()
        {
            float x = player.transform.position.x;
            riverSound.volume = fullVolume * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStartX, fadeEndX, x));

            if (Time.timeScale <= 0f)
                riverSound.Pause();
            else
                riverSound.UnPause();
        }
    }
}
