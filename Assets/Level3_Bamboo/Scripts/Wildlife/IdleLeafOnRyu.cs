using UnityEngine;

namespace Level3Bamboo
{
    // Now and then, when Ryu stands still under bamboo, a leaf drifts down and settles on his head.
    // It falls off as soon as he moves, attacks or gets hurt.
    public class IdleLeafOnRyu : MonoBehaviour
    {
        public enum State { Waiting, Settling, Resting, Dropping }

        public BambooPlayerController player;
        [Tooltip("No leaf falls while one of these fights is going on.")]
        public BadawyEncounter badawyFight;
        public SwampThing swampThing;
        public SpriteRenderer leaf;
        [Tooltip("Idle animation frames where the head position below is valid.")]
        public Sprite[] idleFrames;
        [Tooltip("Top of Ryu's head relative to his pivot when facing right (measured from the idle frames).")]
        public Vector2 headOffset = new Vector2(0.225f, 1.06f);
        [Tooltip("World x ranges under bamboo canopy where leaves can fall.")]
        public Vector2[] canopyZones;
        public float idleSeconds = 6f;
        public Vector2 cooldown = new Vector2(90f, 150f);
        [Range(0f, 1f)] public float chance = 0.7f;
        public float settleSeconds = 1.9f;
        public float dropSeconds = 0.9f;

        public State Current => state;

        private State state;
        private float idleTime;
        private float cooldownLeft = 20f;
        private float timer;
        private Vector3 from;
        private Vector3 dropVelocity;
        private float spin;
        private int lastHealth = -1;
        private float respawnTime = -100f;
        private Color tint;
        private Rigidbody2D playerBody;
        private SpriteRenderer playerSprite;

        private void OnEnable()
        {
            player.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            player.Respawned -= OnRespawned;
        }

        // Start is called before the first frame update
        void Start()
        {
            playerBody = player.GetComponent<Rigidbody2D>();
            playerSprite = player.GetComponent<SpriteRenderer>();
            tint = leaf.color;
            Hide();
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            float dt = Time.deltaTime;
            bool idle = RyuIsStandingStill();
            bool hurt = lastHealth >= 0 && player.Health < lastHealth;
            lastHealth = player.Health;

            switch (state)
            {
                case State.Waiting:
                    cooldownLeft -= dt;
                    idleTime = idle && UnderCanopy(player.transform.position.x) ? idleTime + dt : 0f;
                    if (idleTime >= idleSeconds && cooldownLeft <= 0f)
                        TryStartFalling();
                    break;

                case State.Settling:
                    if (!idle || hurt)
                        Drop();
                    else
                        timer += dt / settleSeconds;
                    break;

                case State.Resting:
                    if (!idle || hurt)
                        Drop();
                    break;

                case State.Dropping:
                    timer += dt / dropSeconds;
                    dropVelocity += Vector3.down * 2.6f * dt;
                    leaf.transform.position += dropVelocity * dt;
                    leaf.transform.Rotate(0f, 0f, spin * dt);
                    Color color = tint;
                    color.a *= 1f - Mathf.Clamp01(timer);
                    leaf.color = color;
                    if (timer >= 1f)
                    {
                        Hide();
                        state = State.Waiting;
                        cooldownLeft = Random.Range(cooldown.x, cooldown.y);
                    }
                    break;
            }
        }

        // The leaf follows Ryu's head after his animation has updated this frame.
        void LateUpdate()
        {
            if (state == State.Settling)
            {
                float t = Mathf.Clamp01(timer);
                float ease = 1f - (1f - t) * (1f - t);
                Vector3 sway = new Vector3(Mathf.Sin(t * 9f) * 0.18f * (1f - t), 0f, 0f);
                leaf.transform.position = Vector3.Lerp(from, Head(), ease) + sway;
                leaf.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 8f) * 35f * (1f - t) + 12f);
                if (t >= 1f)
                    state = State.Resting;
            }
            else if (state == State.Resting)
            {
                leaf.transform.position = Head();
                leaf.transform.rotation = Quaternion.Euler(0f, 0f, player.FacingLeft ? -12f : 12f);
            }
        }

        private void TryStartFalling()
        {
            idleTime = 0f;
            if (Random.value > chance)
            {
                cooldownLeft = Random.Range(cooldown.x, cooldown.y) * 0.5f;
                return;
            }
            from = Head() + new Vector3(Random.Range(-0.45f, 0.45f), 1.5f, 0f);
            leaf.transform.position = from;
            leaf.enabled = true;
            leaf.color = tint;
            timer = 0f;
            state = State.Settling;
        }

        private bool RyuIsStandingStill()
        {
            if (player.IsDead || !player.IsGrounded || player.IsAttacking || InCombat() || Time.time - respawnTime < 2f)
                return false;
            if (Mathf.Abs(playerBody.velocity.x) > 0.05f || Mathf.Abs(playerBody.velocity.y) > 0.2f)
                return false;
            return System.Array.IndexOf(idleFrames, playerSprite.sprite) >= 0;
        }

        private bool InCombat()
        {
            bool swampFighting = swampThing != null && swampThing.CurrentState != SwampThing.State.Dormant
                && swampThing.CurrentState != SwampThing.State.Dead;
            return (badawyFight != null && badawyFight.Active) || swampFighting;
        }

        private bool UnderCanopy(float x)
        {
            foreach (Vector2 zone in canopyZones)
                if (x >= zone.x && x <= zone.y)
                    return true;
            return false;
        }

        private Vector3 Head()
        {
            return player.transform.position + new Vector3(player.FacingLeft ? -headOffset.x : headOffset.x, headOffset.y, 0f);
        }

        private void Drop()
        {
            state = State.Dropping;
            timer = 0f;
            float vx = playerBody.velocity.x;
            float side = vx > 0.05f ? -1f : vx < -0.05f ? 1f : (Random.value < 0.5f ? -1f : 1f);
            dropVelocity = new Vector3(side * Random.Range(0.5f, 0.9f), Random.Range(0.4f, 0.9f), 0f);
            spin = side * Random.Range(160f, 260f);
        }

        private void Hide()
        {
            leaf.enabled = false;
            leaf.transform.rotation = Quaternion.identity;
        }

        private void OnRespawned()
        {
            respawnTime = Time.time;
            Hide();
            state = State.Waiting;
            idleTime = 0f;
            cooldownLeft = Mathf.Max(cooldownLeft, 10f);
        }
    }
}
