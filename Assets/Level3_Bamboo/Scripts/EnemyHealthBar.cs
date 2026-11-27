using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    // Small bar above an enemy. The enemy calls SetHealth; the bar hides itself at 0.
    public class EnemyHealthBar : MonoBehaviour
    {
        public SpriteRenderer fill;
        public TMP_Text enemyName;

        public void SetHealth(int health, int maximum)
        {
            gameObject.SetActive(health > 0);
            Vector3 scale = fill.transform.localScale;
            scale.x = Mathf.Clamp01(health / (float)Mathf.Max(1, maximum));
            fill.transform.localScale = scale;
        }
    }
}
