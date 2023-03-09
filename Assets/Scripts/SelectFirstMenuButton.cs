using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectFirstMenuButton : MonoBehaviour
{
    [SerializeField] private GameObject firstButton;

    private void Start()
    {
        StartCoroutine(SelectFirstItem());
    }
    
    private IEnumerator SelectFirstItem()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        
        EventSystem.current.SetSelectedGameObject(firstButton);
    }
}
