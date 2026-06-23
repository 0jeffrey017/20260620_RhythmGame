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

        [Tooltip("Line drawn from the note head back along the lane for hold notes.")]
        [SerializeField] private LineRenderer holdLine;

        // World-space length of the trailing hold line (0 = tap, no line).
        private float _holdLength;

        /// <summary>Set the hold tail length in world units. 0 = plain tap.</summary>
        public void Configure(float holdWorldLength)
        {
            _holdLength = Mathf.Max(0f, holdWorldLength);
            if (holdLine != null)
            {
                holdLine.useWorldSpace = true;
                holdLine.positionCount = 2;
                holdLine.enabled = _holdLength > 0f;
            }
        }


        // Draw the hold line from the head backwards along the travel direction.
        private void UpdateHoldLine(Vector3 head, Vector3 moveDir)
        {
            if (holdLine == null || _holdLength <= 0f) return;
            holdLine.SetPosition(0, head);
            holdLine.SetPosition(1, head - moveDir * _holdLength);
        }

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
            Vector3 dir1 = (endPos - startPos).sqrMagnitude > 1e-8f
                ? (endPos - startPos).normalized : Vector3.zero;
            float timer = 0;
            while (timer < duration && !token.IsCancellationRequested)
            {
                float t = timer / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                UpdateHoldLine(transform.position, dir1);
                timer += Time.deltaTime;
                await UniTask.Yield(token);
            }
            transform.position = endPos;
            UpdateHoldLine(endPos, dir1);

            float firstDistance = Vector3.Distance(startPos, endPos);
            float speed = firstDistance > 0f ? firstDistance / duration : 0f;

            float destroyDistance = Vector3.Distance(endPos, destroyPos);
            float destroyDuration = speed > 0f ? destroyDistance / speed : 0f;
            Vector3 dir2 = (destroyPos - endPos).sqrMagnitude > 1e-8f
                ? (destroyPos - endPos).normalized : dir1;
            timer = 0;
            while (timer < destroyDuration && !token.IsCancellationRequested)
            {
                float t = timer / destroyDuration;
                transform.position = Vector3.Lerp(endPos, destroyPos, t);
                UpdateHoldLine(transform.position, dir2);
                timer += Time.deltaTime;
                await UniTask.Yield(token);
            }
            transform.position = destroyPos;
            ReturnToPool();
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
    }
}