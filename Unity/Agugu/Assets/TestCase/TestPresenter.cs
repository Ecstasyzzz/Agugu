using UnityEngine;

public class TestPresenter : MonoBehaviour
{
    [SerializeField] private GameObject _red;
    [SerializeField] private GameObject _green;

    private GenericView _view;

    private void Start()
    {
        var viewGameObject = GameObject.Find("01");
        _view = viewGameObject.GetComponent<GenericView>();
        _view.OnButtonClick += _OnButtonClick;
    }

    private void _OnButtonClick(string buttonName)
    {
        if (buttonName == "Rectangle 3")
        {
            _green.SetActive(true);
            _red.SetActive(false);
        }
        else
        {
            _green.SetActive(false);
            _red.SetActive(true);
        }
    }
}
