using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CurePotionsHandler : MonoBehaviour
{
    public int CurePotions { get; private set; }
    
    private PlayerHealthHandler _playerHealthHandler;
    private PlayerInput _input;

    public event Action ChangePotionCount;
    
    [SerializeField] private TMP_Text curePotionsText;
    [SerializeField] private AudioSource potionSource;

    private void Start()
    {
        _input = GetComponent<PlayerInput>();
        _playerHealthHandler = GetComponent<PlayerHealthHandler>();

        ChangePotionCount += OnChangePotionCount;
        ChangePotionCount?.Invoke();
    }
    
    private void Update()
    {
        if (_input.FrameInput.PotionDown && CurePotions > 0)
        {
            UseCurePotions(1);
        }
    }

    public void AddCurePotions(int amount)
    {
        CurePotions += amount;
        ChangePotionCount?.Invoke();
    }
    
    private void UseCurePotions(int amount)
    {
        CurePotions -= amount;
        _playerHealthHandler.Heal(10);
        potionSource.Play();
        ChangePotionCount?.Invoke();
    }
    
    private void OnChangePotionCount()
    {
        curePotionsText.text = CurePotions.ToString();
    }
}
