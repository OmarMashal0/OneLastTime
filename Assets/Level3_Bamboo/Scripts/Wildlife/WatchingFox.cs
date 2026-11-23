using UnityEngine;

namespace Level3Bamboo
{
    // A fox watches Ryu from behind the path, pauses when he comes close, then walks off behind cover.
    // When the whole area is off-screen it quietly returns to its starting spot.
    public class WatchingFox : MonoBehaviour
    {
        public enum State { Watching, Noticed, Leaving, Hidden }

        public BambooPlayerController player;
        public Camera gameplayCamera;
        public SpriteRenderer body;
        public Sprite idle;
        public Sprite[] walk;
        [Min(1f)] public float walkFps = 9f;
        [Min(0.1f)] public float walkSpeed = 1.15f;
        public Transform westCover;
        public Transform eastCover;
        public float watchDistance = 11f;
        public float noticeDistance = 4.2f;
        public Vector2 pauseSeconds = new Vector2(0.8f, 1.2f);

        public State Current => state;

        private State state;
        private Vector3 home;
        private float timer;
        private float frameClock;
        private Transform target;

        // Start is called before the first frame update
        void Start()
        {
            home = body.transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (!IsOnScreen(home.x) && !IsOnScreen(body.transform.position.x))
            {
                ResetWhenHidden();
                return;
            }

            float x = body.transform.position.x;
            float dx = player.transform.position.x - x;
            bool ryuActive = !player.IsDead;
            switch (state)
            {
                case State.Watching:
                    body.sprite = idle;
                    // The art faces right; turn to keep an eye on Ryu.
                    if (ryuActive && Mathf.Abs(dx) < watchDistance)
                        body.flipX = dx < 0f;
                    if (ryuActive && Mathf.Abs(dx) < noticeDistance)
                    {
                        state = State.Noticed;
                        timer = Random.Range(pauseSeconds.x, pauseSeconds.y);
                    }
                    break;

                case State.Noticed:
                    body.flipX = dx < 0f;
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        // Leave away from Ryu, toward the cover on the far side.
                        target = dx < 0f ? eastCover : westCover;
                        body.flipX = target.position.x < x;
                        frameClock = 0f;
                        state = State.Leaving;
                    }
                    break;

                case State.Leaving:
                    frameClock += Time.deltaTime;
                    body.sprite = walk[Mathf.FloorToInt(frameClock * walkFps) % walk.Length];
                    float next = Mathf.MoveTowards(x, target.position.x, walkSpeed * Time.deltaTime);
                    body.transform.position = new Vector3(next, home.y, home.z);
                    // The cover is wider than the fox, so it is fully hidden when it reaches the centre.
                    if (Mathf.Abs(next - target.position.x) < 0.01f)
                    {
                        body.enabled = false;
                        state = State.Hidden;
                    }
                    break;
            }
        }

        private bool IsOnScreen(float x)
        {
            float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
            return Mathf.Abs(x - gameplayCamera.transform.position.x) < halfWidth + 2f;
        }

        private void ResetWhenHidden()
        {
            if (state == State.Watching)
                return;
            body.transform.position = home;
            body.sprite = idle;
            body.enabled = true;
            state = State.Watching;
        }
    }
}
