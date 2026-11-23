using UnityEngine;

namespace Level3Bamboo
{
    public class KatanaLogObstacle : MonoBehaviour
    {
        public BambooPlayerController player;
        public CollectibleLog log;
        public Transform strikePoint;
        public SpriteRenderer obstacle;
        public Sprite clearedSprite;
        public GameObject rope;
        public Transform landingPoint;
        public float horizontalReach = 1.15f;
        public float verticalReach = 0.85f;

        private bool cut;
        private Vector3 dropStart;
        private float dropProgress;

        private void OnEnable()
        {
            if (player != null)
                player.KatanaSwung += OnSwing;
        }

        private void OnDisable()
        {
            if (player != null)
                player.KatanaSwung -= OnSwing;
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (!cut || landingPoint == null || !log.locked)
                return;

            dropProgress = Mathf.MoveTowards(dropProgress, 1f, Time.deltaTime / 0.48f);
            log.transform.position = Vector3.Lerp(dropStart, landingPoint.position, dropProgress * dropProgress);
            if (dropProgress >= 1f)
                log.locked = false;
        }

        private void OnSwing(Vector2 origin, float facing)
        {
            Vector2 offset = (Vector2)strikePoint.position - origin;
            if (cut || offset.x * facing < -0.08f || Mathf.Abs(offset.x) > horizontalReach
                || Mathf.Abs(offset.y) > verticalReach)
                return;

            cut = true;
            if (landingPoint == null)
            {
                obstacle.sprite = clearedSprite;
                log.GetComponent<SpriteRenderer>().enabled = true;
                log.locked = false;
                return;
            }

            dropStart = log.transform.position;
            rope.SetActive(false);
        }
    }
}
