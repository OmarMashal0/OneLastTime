using UnityEngine;

namespace Level3Bamboo
{
    // A snail creeps back and forth along the top of a stone, pausing now and then.
    // The path is a few empty child objects (Path Start, Path Middle, Path End) placed on the stone.
    public class StoneSnail : MonoBehaviour
    {
        public SpriteRenderer body;
        public Sprite[] crawl;
        [Tooltip("Points along the top of the stone, left to right.")]
        public Transform[] path;
        public float speed = 0.03f;
        public float crawlFps = 3f;
        public Vector2 crawlFor = new Vector2(4f, 9f);
        public Vector2 restFor = new Vector2(2f, 5f);

        private float distance;
        private float length;
        private float direction = 1f;
        private float timer;
        private bool resting;
        private float frameClock;

        // Start is called before the first frame update
        void Start()
        {
            for (int i = 1; i < path.Length; i++)
                length += Vector3.Distance(path[i - 1].position, path[i].position);
            distance = length * 0.35f;
            timer = Random.Range(crawlFor.x, crawlFor.y);
            PlaceOnPath();
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            float dt = Time.deltaTime;
            timer -= dt;
            if (timer <= 0f)
            {
                resting = !resting;
                timer = resting ? Random.Range(restFor.x, restFor.y) : Random.Range(crawlFor.x, crawlFor.y);
            }

            if (!resting)
            {
                distance += direction * speed * dt;
                if (distance >= length || distance <= 0f)
                {
                    // Reached an end of the stone: turn around and rest.
                    distance = Mathf.Clamp(distance, 0f, length);
                    direction = -direction;
                    resting = true;
                    timer = Random.Range(restFor.x, restFor.y);
                }
                frameClock += dt;
                body.sprite = crawl[Mathf.FloorToInt(frameClock * crawlFps) % crawl.Length];
            }

            PlaceOnPath();
        }

        private void PlaceOnPath()
        {
            float left = distance;
            for (int i = 1; i < path.Length; i++)
            {
                Vector3 a = path[i - 1].position;
                Vector3 b = path[i].position;
                float span = Vector3.Distance(a, b);
                if (left <= span || i == path.Length - 1)
                {
                    float t = span > 0f ? Mathf.Clamp01(left / span) : 0f;
                    body.transform.position = Vector3.Lerp(a, b, t);
                    // Keep the foot flat on the surface; the art faces right.
                    float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    body.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    body.flipX = direction < 0f;
                    return;
                }
                left -= span;
            }
        }
    }
}
