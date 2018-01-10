using System;

using UnityEngine;
using UnityEngine.UI;

public class GenericView : MonoBehaviour
{
    public event Action<string> OnButtonClick;

    private void Start()
    {
        Array.ForEach(gameObject.GetComponentsInChildren<Button>(),
                      button => button.onClick.AddListener(()=>_OnButtonClick(button.name)));
    }

    private void _OnButtonClick(string buttonName)
    {
        if (OnButtonClick != null)
        {
            OnButtonClick(buttonName);
        }
    }
}
