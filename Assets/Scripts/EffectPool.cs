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
        public ParticleSystem Play(Transform tran)
        {
            var ps = Pool.Get();
            ps.transform.position = tran.position;
            ps.transform.rotation = tran.rotation;
            
            var main = ps.main;
            main.loop = true;
            var cps = ps.GetComponentsInChildren<ParticleSystem>();
            foreach (var cp in cps)
            {
                var m =  cp.main;
                m.loop = true;
            }
            ps.Play(true);
            return ps;
        }

        public void StopPlay(ParticleSystem ps)
        {
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
            var main = ps.main;
            main.loop = false;
            var cps = ps.GetComponentsInChildren<ParticleSystem>();
            foreach (var cp in cps)
            {
                var m =  cp.main;
                m.loop = false;
            }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject(ParticleSystem ps) => Destroy(ps.gameObject);
    }
}
