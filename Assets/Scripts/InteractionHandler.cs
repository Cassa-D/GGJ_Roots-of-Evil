using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;
using UnityEngine.Serialization;

public class InteractionHandler : MonoBehaviour
{
    private Collectable _currentCollectable;
    private PlayerInput _playerInput;

    [SerializeField] private GameObject interactIcon;
    [SerializeField] private AudioSource pUSource;

    public void SetCollectable (Collectable collectable)
    {
        _currentCollectable = collectable;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (!_currentCollectable)
        {
            interactIcon.SetActive(false);
            return;
        }
        
        interactIcon.SetActive(true);
        
        if (_playerInput.FrameInput.InteractDown) Collect();
    }

    private void Collect()
    {
        switch (_currentCollectable.Type)
        {
            case CollectableType.Potion:
                var potionsHandler = GetComponent<CurePotionsHandler>();
                potionsHandler.AddCurePotions(_currentCollectable.Quantity);
                break;
            case CollectableType.PowerUp:
                pUSource.Play();
                GetComponent<PlayerController>().AddPowerUp(_currentCollectable.PowerUpType);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
            
        Destroy(_currentCollectable.gameObject);
    }
}
