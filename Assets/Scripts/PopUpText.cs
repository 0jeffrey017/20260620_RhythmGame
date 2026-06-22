using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class PopUpText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textBox;
    public IObjectPool<PopUpText> pool;

    public void Initialization(string text, Vector3 position)
    {
        textBox.text = text;
        transform.position = position;
        LSequence.Create()
            .Join(
                LMotion.Create(this.transform.position,this.transform.position + Vector3.up * 5,0.5f)
                .BindToLocalPosition(this.transform))
            .Join(LMotion.Create(1f, 0f, 0.5f)
                .WithOnComplete(() =>
                    {
                        pool.Release(this);
                    })
                .BindToColorA(textBox))
            .Run();
    }
    
    
}