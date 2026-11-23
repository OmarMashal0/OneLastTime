using UnityEngine;

namespace Level3Bamboo
{
    // An owl on a high branch blinks and turns its head to follow Ryu as he passes beneath.
    public class WatchfulOwl : MonoBehaviour
    {
        public BambooPlayerController player;
        public SpriteRenderer body;
        public Sprite front;
        public Sprite blink;
        public Sprite lookRight;
        public Sprite ruffle;
        public float watchRange = 7f;
        public float turnMargin = 0.5f;
        public float minHold = 0.7f;
        public float settleSeconds = 2.2f;
        public Vector2 blinkEvery = new Vector2(3f, 7f);
        public float blinkSeconds = 0.14f;
        public Vector2 ruffleEvery = new Vector2(28f, 45f);
        public float ruffleSeconds = 0.55f;

        public bool LookingRight => lookingRight;

        private bool lookingRight;
        private float hold;
        private float away;
        private float nextBlink;
        private float nextRuffle;
        private float blinking;
        private float ruffling;

        // Start is called before the first frame update
        void Start()
        {
            nextBlink = Random.Range(blinkEvery.x, blinkEvery.y);
            nextRuffle = Random.Range(ruffleEvery.x, ruffleEvery.y);
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            float dt = Time.deltaTime;
            float dx = player.transform.position.x - body.transform.position.x;
            bool watching = !player.IsDead && Mathf.Abs(dx) < watchRange;
            hold -= dt;

            if (watching)
            {
                away = 0f;
                // The only turned pose looks to the right, so turn once Ryu is clearly on that side.
                bool wantRight = lookingRight ? dx > -turnMargin : dx > turnMargin;
                if (wantRight != lookingRight && hold <= 0f)
                {
                    lookingRight = wantRight;
                    hold = minHold;
                }
            }
            else
            {
                // Ryu has gone: after a while, face the front again.
                away += dt;
                if (lookingRight && away > settleSeconds && hold <= 0f)
                {
                    lookingRight = false;
                    hold = minHold;
                }
            }

            nextBlink -= dt;
            nextRuffle -= dt;
            if (blinking > 0f) blinking -= dt;
            if (ruffling > 0f) ruffling -= dt;
            if (!lookingRight && blinking <= 0f && ruffling <= 0f && nextBlink <= 0f)
            {
                blinking = blinkSeconds;
                nextBlink = Random.Range(blinkEvery.x, blinkEvery.y);
            }
            if (!watching && !lookingRight && ruffling <= 0f && nextRuffle <= 0f)
            {
                ruffling = ruffleSeconds;
                nextRuffle = Random.Range(ruffleEvery.x, ruffleEvery.y);
            }

            if (lookingRight)
                body.sprite = lookRight;
            else if (ruffling > 0f)
                body.sprite = ruffle;
            else if (blinking > 0f)
                body.sprite = blink;
            else
                body.sprite = front;
        }
    }
}
