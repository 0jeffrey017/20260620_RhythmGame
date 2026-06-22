using System.Collections;
using CartoonFX;
using UnityEngine;
using UnityEngine.Pool;

namespace RhythmGame
{
    /// <summary>
    /// Pools P_Effect particle instances and plays one at a given position on
    /// demand (e.g. when the player hits a lane). CFXR's own clear behavior is
    /// disabled so the pool fully owns each instance's lifecycle: an effect is
    /// released back to the pool once its particles finish.
    /// </summary>
    public class EffectPool : MonoBehaviour
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private bool collectionChecks = true;

        private IObjectPool<ParticleSystem> m_Pool;

        public IObjectPool<ParticleSystem> Pool =>
            m_Pool ??= new ObjectPool<ParticleSystem>(
                CreatePooledItem,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionChecks,
                defaultCapacity,
                maxPoolSize);

        /// <summary>Play an effect at the given world position.</summary>
        public void Play(Vector3 position)
        {
            var ps = Pool.Get();
            ps.transform.position = position;
            ps.Play(true);
            StartCoroutine(ReleaseWhenDone(ps));
        }

        private IEnumerator ReleaseWhenDone(ParticleSystem ps)
        {
            // Wait a frame so IsAlive reports the freshly started particles.
            yield return null;
            while (ps != null && ps.IsAlive(true))
                yield return null;
            if (ps != null)
                Pool.Release(ps);
        }

        private ParticleSystem CreatePooledItem()
        {
            var go = Instantiate(effectPrefab, transform);

            // Let the pool own the lifecycle: stop CFXR from disabling/destroying.
            var cfxr = go.GetComponent<CFXR_Effect>();
            if (cfxr != null) cfxr.clearBehavior = CFXR_Effect.ClearBehavior.None;

            return go.GetComponent<ParticleSystem>();
        }

        private void OnTakeFromPool(ParticleSystem ps) => ps.gameObject.SetActive(true);

        private void OnReturnedToPool(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject(ParticleSystem ps) => Destroy(ps.gameObject);
    }
}
