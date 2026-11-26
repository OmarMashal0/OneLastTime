using UnityEngine;

namespace Level3Bamboo
{
    // A Level 3 enemy (Badawy or a Gloom Eye). Simple state machine: Idle -> Warning (flash) -> Charge
    // (dash at Ryu, contact damage) -> Recover -> Idle. A katana hit puts it in Hurt; at 0 health it plays
    // Dead and switches itself off. Badawy's boss fight is run by BadawyEncounter.
    public class ForestEnemy : MonoBehaviour
    {
        // Badawy walks on the ground and summons Gloom Eyes; an Eye flies toward Ryu and dives at him.
        public enum Kind { Badawy, Eye }
        public Kind kind;
        public BambooPlayerController player;
        public Collider2D playerCollider;
        public BoxCollider2D damageCollider;
        public SpriteRenderer artwork;
        public Animator animator;
        public EnemyHealthBar healthBar;
        public int maxHealth = 20;
        public int contactDamage = 15;

        // Sounds (Lab 7 style: an AudioSource plays clips with PlayOneShot).
        [Header("Sounds")]
        public AudioSource audioSource;
        public AudioClip attackSound;   // when an eye warns, or Badawy summons
        public EnemyHitEffect hitEffect;
        public Color hitColor = Color.white;
        public Color hitAccent = Color.white;
        public AudioClip[] hitSounds = new AudioClip[2];
        private int nextHitSound;
        public AudioClip hurtSound;     // when the katana hits
        public AudioClip deathSound;    // when health reaches 0
        public int katanaDamage = 10;
        public float moveSpeed = 2.25f;
        public float attackSpeed = 8f;
        public float detectionRange = 9f;
        public float warningSeconds = 0.45f;
        public float recoverySeconds = 0.65f;
        public float attackDistance = 3f;
        public float pursuitLimit = 16f;
        public float hurtSeconds = 0.42f;
        public float deathSeconds = 0.7f;

        public bool CanBeHit { get; set; } = true;
        public float MinimumFlightY { get; set; } = float.NegativeInfinity;
        public int Health { get; private set; }
        public bool Alive => Health > 0 && gameObject.activeInHierarchy;
        public event System.Action<ForestEnemy> Defeated;

        private enum Phase { Idle, Warning, Charge, Recover, Hurt, Dead }
        private Phase phase;
        private Vector3 home;
        private Vector2 chargeDirection;
        private float chargeRemaining;
        private float timer;
        private float spawnFlash;
        private bool chargeHit;
        private float crystalFlash;
        private float crystalDuration;
        private MaterialPropertyBlock crystalProperties;
        private static readonly int CrystalFlashId = Shader.PropertyToID("_CrystalFlash");
        private bool initialized;
        private readonly System.Collections.Generic.List<RaycastHit2D> hits = new System.Collections.Generic.List<RaycastHit2D>();

        // Start is called before the first frame update
        void Start()
        {
            if (!initialized)
                ResetEnemy(transform.position);
        }

        public void ResetEnemy(Vector3 position)
        {
            if (player != null)
                player.KatanaSwung -= OnSwing;
            initialized = true;
            home = position;
            transform.position = position;
            Health = maxHealth;
            phase = Phase.Idle;
            timer = 0f;
            crystalFlash = 0f;
            if (kind == Kind.Badawy)
                SetCrystalGlow(0f);
            chargeHit = false;
            spawnFlash = kind == Kind.Eye ? 0.31f : 0f;
            damageCollider.enabled = true;
            gameObject.SetActive(true);
            player.KatanaSwung -= OnSwing;
            player.KatanaSwung += OnSwing;
            artwork.color = Color.white;
            Play("Idle");
            healthBar.SetHealth(Health, maxHealth);
        }

        void OnDisable()
        {
            if (player != null)
                player.KatanaSwung -= OnSwing;
        }

        public void WarnSummon(float duration)
        {
            crystalDuration = Mathf.Max(0.01f, duration);
            crystalFlash = crystalDuration;
        }

        public void Summon()
        {
            if (!Alive)
                return;
            Play("Action");
            PlaySound(attackSound);
            phase = Phase.Recover;
            timer = 0.65f;
        }

        // Update is called once per frame
        void Update()
        {
            timer -= Time.deltaTime;
            if (phase == Phase.Dead)
            {
                if (timer <= 0f)
                {
                    if (kind == Kind.Eye)
                        Destroy(gameObject);
                    else
                        gameObject.SetActive(false);
                }
                return;
            }

            if (spawnFlash > 0f)
            {
                spawnFlash -= Time.deltaTime;
                float fade = Mathf.Clamp01(spawnFlash / 0.31f);
                artwork.color = Color.Lerp(Color.white, new Color(0.66f, 0.44f, 0.93f, 0.38f), fade);
            }

            if (crystalFlash > 0f)
            {
                crystalFlash -= Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(crystalFlash / crystalDuration);
                SetCrystalGlow(Mathf.Sin(progress * Mathf.PI));
            }

            if (player.IsDead || Time.deltaTime <= 0f)
                return;

            if ((phase == Phase.Hurt || phase == Phase.Recover) && timer <= 0f)
                Rest();

            Vector2 target = playerCollider.bounds.center;
            Vector2 center = damageCollider.bounds.center;
            if (kind != Kind.Badawy)
                MoveEnemy(target, center);


        }

        // Badawy hurts Ryu if Ryu walks into him during the fight (like the Lab 6 enemy trigger).
        // Ryu's short invulnerability after a hit stops this from draining his health at once.
        void OnTriggerStay2D(Collider2D other)
        {
            if (kind == Kind.Badawy && Alive && CanBeHit && other.tag == "Player")
                player.TakeDamage(contactDamage);
        }

        // Plays a sound only if both the speaker and the clip were set in the Inspector.
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void SetCrystalGlow(float intensity)
        {
            if (crystalProperties == null)
                crystalProperties = new MaterialPropertyBlock();
            artwork.GetPropertyBlock(crystalProperties);
            crystalProperties.SetFloat(CrystalFlashId, Mathf.Max(0f, intensity));
            artwork.SetPropertyBlock(crystalProperties);
        }

        private void MoveEnemy(Vector2 target, Vector2 center)
        {
            Vector2 offset = target - center;
            bool canDetect = Mathf.Abs(target.x - home.x) <= pursuitLimit && offset.magnitude <= detectionRange
                && !Blocked(center, target);

            if (phase == Phase.Warning)
            {
                if (timer <= 0f)
                {
                    chargeDirection = offset.normalized;
                    if (chargeDirection == Vector2.zero)
                        chargeDirection = Vector2.right;
                    chargeHit = false;
                    chargeRemaining = attackDistance;
                    phase = Phase.Charge;
                    Face(chargeDirection.x);
                    Play("Action");
                }
                return;
            }

            if (phase == Phase.Charge)
            {
                float distance = Mathf.Min(chargeRemaining, attackSpeed * Time.deltaTime);
                float allowed = Sweep(center, chargeDirection, distance);
                MoveBy(chargeDirection * allowed);
                chargeRemaining -= allowed;
                // This collider also receives katana hits, so only the charge evaluates player damage.
                Bounds swept = new Bounds(center, damageCollider.bounds.size);
                swept.Encapsulate(new Bounds(center + chargeDirection * allowed, swept.size));
                if (!chargeHit && swept.Intersects(playerCollider.bounds) && !Blocked(center, target))
                {
                    chargeHit = true;
                    player.TakeDamage(contactDamage);
                }
                if (allowed < distance || chargeRemaining <= 0.001f)
                    Recover();
                return;
            }

            if (phase != Phase.Idle)
                return;

            if (canDetect && offset.magnitude <= 2.1f)
            {
                Face(offset.x);
                Warn();
                return;
            }

            Vector2 desired = canDetect ? target + Vector2.up * 0.45f : (Vector2)home + Vector2.up * Mathf.Sin(Time.time * 2.3f + home.x) * 0.22f;
            Vector2 delta = desired - center;
            float amount = Mathf.Min(delta.magnitude, moveSpeed * Time.deltaTime);
            if (amount > 0.002f)
            {
                float allowed = Sweep(center, delta.normalized, amount);
                MoveBy(delta.normalized * allowed);
                Face(delta.x);
            }
        }

        private void MoveBy(Vector2 delta)
        {
            Vector3 next = transform.position + (Vector3)delta;
            if (kind == Kind.Eye)
                next.y = Mathf.Max(next.y, MinimumFlightY);
            transform.position = next;
        }

        // How far the enemy can move before it would hit something solid.
        private float Sweep(Vector2 center, Vector2 direction, float distance)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            Vector2 size = damageCollider.bounds.size * 0.88f;
            Physics2D.BoxCast(center, size, 0f, direction, filter, hits, distance);
            float limit = distance;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == damageCollider || hit.collider == playerCollider || hit.distance > limit)
                    continue;
                if (hit.collider.isTrigger)
                    continue;
                limit = Mathf.Max(0f, hit.distance - 0.015f);
            }
            return limit;
        }

        private bool Blocked(Vector2 start, Vector2 end)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            Physics2D.Linecast(start, end, filter, hits);
            foreach (RaycastHit2D hit in hits)
                if (hit.collider != playerCollider)
                    return true;
            return false;
        }

        private void OnSwing(Vector2 origin, float facing)
        {
            if (!Alive || !CanBeHit)
                return;

            Vector2 target = damageCollider.bounds.ClosestPoint(origin + Vector2.right * facing * 0.65f);
            Vector2 offset = target - origin;
            if (offset.x * facing < -0.12f || Mathf.Abs(offset.x) > 1.22f || Mathf.Abs(offset.y) > 0.83f || Blocked(origin, target))
                return;

            Health = Mathf.Max(0, Health - katanaDamage);
            healthBar.SetHealth(Health, maxHealth);
            if (hitEffect != null)
                Instantiate(hitEffect, target, Quaternion.identity).Play(hitColor, hitAccent, false);
            if (hitSounds.Length == 2 && hitSounds[0] != null && hitSounds[1] != null)
            {
                audioSource.PlayOneShot(hitSounds[nextHitSound], 0.45f);
                nextHitSound = 1 - nextHitSound;
            }
            else if (Health > 0)
                PlaySound(hurtSound);
            if (Health == 0)
            {
                phase = Phase.Dead;
                damageCollider.enabled = false;
                timer = deathSeconds;
                Play("Death");
                PlaySound(deathSound);
                Defeated?.Invoke(this);
            }
            else
            {
                phase = Phase.Hurt;
                timer = hurtSeconds;
                Play("Hurt");
            }
        }

        private void Warn()
        {
            phase = Phase.Warning;
            timer = warningSeconds;
            Play("Action");
            PlaySound(attackSound);
        }

        private void Recover()
        {
            phase = Phase.Recover;
            timer = recoverySeconds;
            Play("Idle");
        }

        private void Rest()
        {
            phase = Phase.Idle;
            Play("Idle");
        }

        private void Face(float direction)
        {
            if (Mathf.Abs(direction) > 0.04f)
                artwork.flipX = direction < 0f;
        }

        private void Play(string state)
        {
            animator.Play(state, 0, 0f);
        }
    }
}
