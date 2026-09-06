using UnityEngine;
using System.Collections;

public class ButtonShow : MonoBehaviour
{
    [SerializeField] private GameObject button;
    [SerializeField] private float buttonDelay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        StartCoroutine(ShowButton());
    }

    private IEnumerator ShowButton()
    {
        button.SetActive(false);
        yield return new WaitForSeconds(buttonDelay);
        button.SetActive(true);
    }
}
