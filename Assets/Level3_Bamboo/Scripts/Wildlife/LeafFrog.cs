using UnityEngine;

namespace Level3Bamboo
{
    // A small frog blinks and puffs its throat, then crouches and hops behind cover when Ryu comes close.
    // When its area is off-screen it quietly returns to its starting spot.
    public class LeafFrog : MonoBehaviour
    {
        public enum State { Idle, Crouch, Hop, Hidden }

        public BambooPlayerController player;
        public Camera gameplayCamera;
        public SpriteRenderer body;
        public Sprite idle;
        public Sprite blink;
        public Sprite puff;
        public Sprite crouch;
        public Transform leftCover;
        public Transform rightCover;
        public float noticeDistance = 2.3f;
        public float crouchSeconds = 0.22f;
        public float hopSeconds = 0.42f;
        public float hopHeight = 0.24f;
        public Vector2 blinkEvery = new Vector2(2.5f, 6f);
        public float blinkSeconds = 0.14f;
        public Vector2 puffEvery = new Vector2(7f, 13f);
        public float puffSeconds = 0.75f;

        public State Current => state;

        private State state;
        private Vector3 home;
        private bool homeFlip;
        private float timer;
        private float nextBlink;
        private float nextPuff;
        private float poseTimer;
        private Sprite pose;
        private Vector3 hopFrom;
        private Vector3 hopTo;

        // Start is called before the first frame update
        void Start()
        {
            home = body.transform.position;
            homeFlip = body.flipX;
            nextBlink = Random.Range(blinkEvery.x, blinkEvery.y);
            nextPuff = Random.Range(puffEvery.x, puffEvery.y);
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (!IsOnScreen(home.x))
            {
                ResetWhenHidden();
                return;
            }

            float dt = Time.deltaTime;
            switch (state)
            {
                case State.Idle:
                    UpdateIdlePoses(dt);
                    Vector3 ryu = player.transform.position;
                    float dx = ryu.x - home.x;
                    if (!player.IsDead && Mathf.Abs(dx) < noticeDistance && Mathf.Abs(ryu.y - home.y) < 1.6f)
                    {
                        // Hop to the cover on the side away from Ryu.
                        Transform cover = dx < 0f ? rightCover : leftCover;
                        hopFrom = body.transform.position;
                        hopTo = new Vector3(cover.position.x, home.y, home.z);
                        body.flipX = hopTo.x < hopFrom.x;
                        body.sprite = crouch;
                        timer = crouchSeconds;
                        state = State.Crouch;
                    }
                    break;

                case State.Crouch:
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        timer = 0f;
                        state = State.Hop;
                    }
                    break;

                case State.Hop:
                    timer += dt / hopSeconds;
                    float t = Mathf.Clamp01(timer);
                    body.transform.position = Vector3.Lerp(hopFrom, hopTo, t) + Vector3.up * (4f * hopHeight * t * (1f - t));
                    body.sprite = t < 0.18f || t > 0.86f ? crouch : idle;
                    if (t >= 1f)
                    {
                        // Landed behind the rock or fern: stay tucked away.
                        body.enabled = false;
                        state = State.Hidden;
                    }
                    break;
            }
        }

        // Every few seconds the frog blinks, and now and then it puffs its throat.
        private void UpdateIdlePoses(float dt)
        {
            nextBlink -= dt;
            nextPuff -= dt;
            if (poseTimer > 0f)
                poseTimer -= dt;
            else if (nextPuff <= 0f)
            {
                pose = puff;
                poseTimer = puffSeconds;
                nextPuff = Random.Range(puffEvery.x, puffEvery.y);
                nextBlink = Mathf.Max(nextBlink, 1f);
            }
            else if (nextBlink <= 0f)
            {
                pose = blink;
                poseTimer = blinkSeconds;
                nextBlink = Random.Range(blinkEvery.x, blinkEvery.y);
            }
            body.sprite = poseTimer > 0f ? pose : idle;
        }

        private bool IsOnScreen(float x)
        {
            float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
            return Mathf.Abs(x - gameplayCamera.transform.position.x) < halfWidth + 2f;
        }

        private void ResetWhenHidden()
        {
            if (state == State.Idle)
                return;
            body.transform.position = home;
            body.flipX = homeFlip;
            body.sprite = idle;
            body.enabled = true;
            poseTimer = 0f;
            state = State.Idle;
        }
    }
}
