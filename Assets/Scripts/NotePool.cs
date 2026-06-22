using RhythmGame;
using UnityEngine;
using UnityEngine.Pool;

public class NotePool : MonoBehaviour
{   
    [SerializeField] private GameObject notePrefab;
    public bool collectionChecks = true;
    public int maxPoolSize = 1000;

    IObjectPool<NodeObject> m_Pool;

    public IObjectPool<NodeObject> Pool
    {
        get
        {
            m_Pool ??= new ObjectPool<NodeObject>(CreatePooledItem,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionChecks,
                100,
                maxPoolSize);
            return m_Pool;
        }
    }

    NodeObject CreatePooledItem()
    {
        var go = Instantiate(notePrefab, transform);
        var node = go.GetComponent<NodeObject>();
        node.pool = m_Pool;
        return node;
    }

    // Called when an item is returned to the pool using Release
    void OnReturnedToPool(NodeObject system)
    {
        system.gameObject.SetActive(false);
    }

    // Called when an item is taken from the pool using Get
    void OnTakeFromPool(NodeObject system)
    {
        system.MarkTaken();
        system.gameObject.SetActive(true);
    }

    // If the pool capacity is reached then any items returned will be destroyed.
    // We can control what the destroy behavior does, here we destroy the GameObject.
    void OnDestroyPoolObject(NodeObject system)
    {
        Destroy(system.gameObject);
    }
}