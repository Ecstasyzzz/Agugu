using UnityEngine;

public class TestPresenter : MonoBehaviour
{
    [SerializeField] private GameObject _red;
    [SerializeField] private GameObject _green;

    private GenericView _view;

    private void Start()
    {
        var viewGameObject = GameObject.Find("Canvas");
        _view = viewGameObject.GetComponent<GenericView>();
        _view.OnButtonClick += _OnButtonClick;
    }

    private void _OnButtonClick(string buttonName)
    {
        Debug.Log(buttonName);
    }
}
