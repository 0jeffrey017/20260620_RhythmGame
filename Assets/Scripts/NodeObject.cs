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

        public void SetMovement(Vector3 startPos, Vector3 endPos,Vector3 destroyPos, float duration)
        {
            transform.position = startPos;
            HandleMovement(startPos,
                endPos,
                destroyPos,
                duration,
                this.GetCancellationTokenOnDestroy()).Forget();
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
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("NoteReturnLine"))
            {
                pool.Release(this);
            }
        }
    }
}