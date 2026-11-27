using UnityEngine;

namespace Level3Bamboo
{
    // Particle burst played when the katana hits an enemy (colours chosen by the enemy).
    public class EnemyHitEffect : MonoBehaviour
    {
        public ParticleSystem impact;
        public ParticleSystem leaves;

        public void Play(Color first, Color second, bool scatterLeaves)
        {
            var main = impact.main;
            main.startColor = new ParticleSystem.MinMaxGradient(first, second);
            impact.Play(false);
            if (scatterLeaves)
                leaves.Play(false);
            Destroy(gameObject, 0.65f);
        }
    }
}
