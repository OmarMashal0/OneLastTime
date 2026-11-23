using System.Collections;
using OneLastTime.Combat;
using UnityEngine;

namespace OneLastTime.Interaction
{
    public class BreakableBarricade : MonoBehaviour, IDamageable
    {
        private const int HitsToBreak = 3;
        private int hitsTaken;
        private Coroutine shake;
        private Vector3 restingPosition;

        public bool IsDead { get; private set; }

        private void Awake()
        {
            restingPosition = transform.localPosition;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0) return;

            hitsTaken++;
            if (shake != null)
            {
                StopCoroutine(shake);
                shake = null;
                transform.localPosition = restingPosition;
            }

            if (hitsTaken < HitsToBreak)
            {
                shake = StartCoroutine(ShakeInPlace());
                return;
            }

            IsDead = true;
            foreach (var blocker in GetComponentsInChildren<Collider2D>())
            {
                blocker.enabled = false;
            }

            StartCoroutine(ScatterPieces());
        }

        private IEnumerator ShakeInPlace()
        {
            float strength = 0.08f * hitsTaken;
            float duration = 0.16f + 0.04f * hitsTaken;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float offset = Mathf.Sin(progress * Mathf.PI * 6f) * strength * (1f - progress);
                transform.localPosition = restingPosition + Vector3.right * offset;
                yield return null;
                elapsed += Time.deltaTime;
            }

            transform.localPosition = restingPosition;
            shake = null;
        }

        private IEnumerator ScatterPieces()
        {
            const float duration = 0.48f;
            var pieces = GetComponentsInChildren<SpriteRenderer>();
            var positions = new Vector3[pieces.Length];
            var rotations = new Quaternion[pieces.Length];
            var colors = new Color[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                positions[i] = pieces[i].transform.localPosition;
                rotations[i] = pieces[i].transform.localRotation;
                colors[i] = pieces[i].color;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                for (int i = 0; i < pieces.Length; i++)
                {
                    float direction = (i % 2 == 0) ? -1f : 1f;
                    float distance = 0.55f + 0.2f * (i % 3);
                    var piece = pieces[i].transform;
                    piece.localPosition = positions[i] + new Vector3(
                        direction * distance * progress,
                        1.5f * progress - 2.8f * progress * progress,
                        0f);
                    piece.localRotation = rotations[i] * Quaternion.Euler(0f, 0f, direction * 95f * progress);

                    Color color = colors[i];
                    color.a *= 1f - Mathf.InverseLerp(0.3f, 1f, progress);
                    pieces[i].color = color;
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            gameObject.SetActive(false);
        }
    }
}
