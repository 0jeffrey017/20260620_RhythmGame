using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace RhythmGame
{
    public class NodeObject : MonoBehaviour
    {
        public IObjectPool<NodeObject> pool;

        // Incremented every time this instance is taken from the pool. A queued
        // reference in JudgementManager captures the value at spawn time, so a
        // stale reference (whose instance has since been reused for another
        // note) can be detected and ignored instead of releasing the wrong note.
        public int UseId { get; private set; }

        // True once this instance has been handed back to the pool for the
        // current use. Makes release idempotent: the judgement path and the
        // return-line trigger can both ask to release, but only the first wins,
        // so the same instance is never released (and re-pooled) twice.
        private bool _released;

        /// <summary>Called by the pool when this instance is taken for a new note.</summary>
        public void MarkTaken()
        {
            UseId++;
            _released = false;
        }

        // Token for the current movement only. Cancelled (and replaced) every
        // time the note is reused, so an old movement can never run alongside
        // a new one on the same pooled instance.
        private CancellationTokenSource _moveCts;

        public void SetMovement(Vector3 startPos, Vector3 endPos,Vector3 destroyPos, float duration)
        {
            // Stop any movement left over from a previous use of this instance.
            CancelMovement();
            _moveCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());

            transform.position = startPos;
            HandleMovement(startPos,
                endPos,
                destroyPos,
                duration,
                _moveCts.Token).Forget();
        }

        private void CancelMovement()
        {
            if (_moveCts == null) return;
            _moveCts.Cancel();
            _moveCts.Dispose();
            _moveCts = null;
        }

        private async UniTaskVoid HandleMovement(Vector3 startPos,
            Vector3 endPos, 
            Vector3 destroyPos,
            float duration,
            CancellationToken token)
        {
            float timer = 0;
            while (timer < duration && !token.IsCancellationRequested)
            {
                float t = timer / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                timer += Time.deltaTime;
                await UniTask.Yield(token);
            }
            transform.position = endPos;
            
            float firstDistance = Vector3.Distance(startPos, endPos);
            float speed = firstDistance > 0f ? firstDistance / duration : 0f;
            
            float destroyDistance = Vector3.Distance(endPos, destroyPos);
            float destroyDuration = speed > 0f ? destroyDistance / speed : 0f;
            timer = 0;
            while (timer < destroyDuration && !token.IsCancellationRequested)
            {
                float t = timer / destroyDuration;
                transform.position = Vector3.Lerp(endPos, destroyPos, t);
                timer += Time.deltaTime;
                await UniTask.Yield(token);
            }
            transform.position = destroyPos;
        }
        /// <summary>Return this note to the pool once it has been judged.
        /// Idempotent: a second call (e.g. from the return-line trigger) is a
        /// no-op, so the instance is never released twice.</summary>
        public void ReturnToPool()
        {
            if (_released) return;
            _released = true;
            CancelMovement();
            pool.Release(this);
        }

        private void OnDisable()
        {
            // Safety net: never leave a movement running on a pooled instance.
            CancelMovement();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("NoteReturnLine"))
            {
                // Same idempotent path as judgement so the two can't double-release.
                ReturnToPool();
            }
        }
    }
}