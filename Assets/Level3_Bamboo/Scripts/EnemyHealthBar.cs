using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    public class EnemyHealthBar : MonoBehaviour
    {
        public SpriteRenderer fill;
        public TMP_Text enemyName;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void SetHealth(int health, int maximum)
        {
            gameObject.SetActive(health > 0);
            Vector3 scale = fill.transform.localScale;
            scale.x = Mathf.Clamp01(health / (float)Mathf.Max(1, maximum));
            fill.transform.localScale = scale;
        }
    }
}
