using UnityEngine;

namespace Level3Bamboo
{
    // The Badawy boss fight in the clearing. When Ryu walks into the FightStartTrigger child, it calls
    // StartFight(): the title fades in, and every few seconds Badawy summons a pair of Gloom Eyes
    // (Purple and Green Eye prefabs). The trail gate stays closed until Badawy is defeated.
    // If Ryu dies first, the fight resets.
    public class BadawyEncounter : MonoBehaviour
    {
        public BambooPlayerController player;
        public Collider2D playerCollider;
        public ForestEnemy badawy;
        public ForestEnemy purpleEye;
        public ForestEnemy greenEye;
        public GameObject trailBlocker;
        public event System.Action ObjectiveChanged;
        public CanvasGroup encounterTitle;
        public float spawnInterval = 6f;

        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public int LivingEyes { get; private set; }
        private readonly System.Collections.Generic.List<ForestEnemy> eyes = new System.Collections.Generic.List<ForestEnemy>();
        private Vector3 bossHome;
        private float nextPair;
        private float titleTime;
        private int pairs;
        public float summonWarning = 0.45f;
        private bool warned;
        private readonly Collider2D[] spawnObstructions = new Collider2D[1];

        // Start is called before the first frame update
        void Start()
        {
            bossHome = badawy.transform.position;
            badawy.ResetEnemy(bossHome);
            badawy.CanBeHit = false;
            encounterTitle.alpha = 0f;
            player.Respawned += ResetUnfinishedFight;
            badawy.Defeated += OnBadawyDefeated;
        }

        void OnDestroy()
        {
            if (player != null)
                player.Respawned -= ResetUnfinishedFight;
            if (badawy != null)
                badawy.Defeated -= OnBadawyDefeated;
        }

        // Called by FightStartTrigger when Ryu walks into the clearing.
        public void StartFight()
        {
            if (Active || Completed || player.IsDead || Time.timeScale <= 0f)
                return;

            Active = true;
            badawy.CanBeHit = true;
            titleTime = 0f;
            encounterTitle.alpha = 0f;
            nextPair = Time.time + summonWarning;
            warned = false;
            ObjectiveChanged?.Invoke();
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f || Completed || player.IsDead)
                return;

            if (!Active)
                return;

            titleTime += Time.deltaTime;
            encounterTitle.alpha = Mathf.Min(Mathf.Clamp01(titleTime / 0.62f), Mathf.Clamp01((3f - titleTime) / 0.8f));
            if (badawy.Alive && !warned && Time.time >= nextPair - summonWarning)
            {
                warned = true;
                badawy.WarnSummon(summonWarning);
            }
            if (badawy.Alive && Time.time >= nextPair)
            {
                SpawnPair();
                warned = false;
                nextPair += Mathf.Max(0.1f, spawnInterval);
            }
        }

        private void SpawnPair()
        {
            badawy.Summon();
            Spawn(purpleEye, -1f);
            Spawn(greenEye, 1f);
            pairs++;
        }

        private void Spawn(ForestEnemy prefab, float side)
        {
            Vector3 position = bossHome + new Vector3(side * 2.35f, 1.36f + (pairs % 3) * 0.34f, 0f);
            var filter = new ContactFilter2D { useTriggers = false };
            for (int i = 0; i < 8; i++)
            {
                var nearby = Physics2D.OverlapCircle(position, 0.44f, filter, spawnObstructions);
                if (nearby == 0)
                    break;
                position.y += 0.52f;
            }

            ForestEnemy eye = Instantiate(prefab, position, Quaternion.identity, transform);
            eye.player = player;
            eye.playerCollider = playerCollider;
            eye.MinimumFlightY = bossHome.y + 0.65f;
            eye.Defeated += OnEyeDefeated;
            eye.ResetEnemy(position);
            eyes.RemoveAll(item => item == null);
            eyes.Add(eye);
            LivingEyes++;
        }

        private void OnEyeDefeated(ForestEnemy eye)
        {
            eye.Defeated -= OnEyeDefeated;
            LivingEyes--;
            TryComplete();
        }

        private void OnBadawyDefeated(ForestEnemy enemy)
        {
            if (LivingEyes > 0)
                ObjectiveChanged?.Invoke();
            TryComplete();
        }

        private void TryComplete()
        {
            if (!Active || badawy.Alive || LivingEyes != 0)
                return;

            Active = false;
            Completed = true;
            encounterTitle.alpha = 0f;
            trailBlocker.SetActive(false);
            ObjectiveChanged?.Invoke();
        }

        private void ResetUnfinishedFight()
        {
            if (Completed)
                return;

            foreach (ForestEnemy eye in eyes)
            {
                if (eye == null)
                    continue;
                eye.Defeated -= OnEyeDefeated;
                eye.gameObject.SetActive(false);
                Destroy(eye.gameObject);
            }
            eyes.Clear();
            LivingEyes = 0;
            pairs = 0;
            Active = false;
            nextPair = 0f;
            badawy.ResetEnemy(bossHome);
            badawy.CanBeHit = false;
            encounterTitle.alpha = 0f;
            ObjectiveChanged?.Invoke();
        }
    }
}
