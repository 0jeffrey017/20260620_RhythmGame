using UnityEngine;
using UnityEngine.Pool;

public class TextBoxPool : MonoBehaviour
{   
    [SerializeField] private GameObject textBox;
    public bool collectionChecks = true;
    public int maxPoolSize = 1000;

    IObjectPool<PopUpText> m_Pool;

    public IObjectPool<PopUpText> Pool
    {
        get
        {
            m_Pool ??= new ObjectPool<PopUpText>(CreatePooledItem,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionChecks,
                100,
                maxPoolSize);
            return m_Pool;
        }
    }

    PopUpText CreatePooledItem()
    {
        var go = Instantiate(textBox, transform);
        var text = go.GetComponent<PopUpText>();
        text.pool = m_Pool;
        return text;
    }

    // Called when an item is returned to the pool using Release
    void OnReturnedToPool(PopUpText system)
    {
        system.gameObject.SetActive(false);
    }

    // Called when an item is taken from the pool using Get
    void OnTakeFromPool(PopUpText system)
    {
        system.gameObject.SetActive(true);
    }

    // If the pool capacity is reached then any items returned will be destroyed.
    // We can control what the destroy behavior does, here we destroy the GameObject.
    void OnDestroyPoolObject(PopUpText system)
    {
        Destroy(system.gameObject);
    }
}