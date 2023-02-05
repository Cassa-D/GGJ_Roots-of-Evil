using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerHealthHandler : MonoBehaviour
{
    [SerializeField] private int maxHealth;
    public int MaxHealth => maxHealth;
    public int Health { get; private set; }
    [SerializeField] private int pMaxHealth;
    public int PurpleHealth { get; private set; }
    
    public event Action DamageTaken;

    private const float InvincibilityTime = 1f;
    private float _invincibilityTimer;
    
    [SerializeField] private Image healthBar;
    [SerializeField] private Image purpleHealthBar;

    [SerializeField] private AudioSource damageSource;

    private void Start()
    {
        Health = maxHealth;
        PurpleHealth = pMaxHealth;
        UpdateHealthBar();
        DamageTaken += SetInvincibilityTimer;
        DamageTaken += UpdateHealthBar;
    }
    
    public void TakeDamage(int damage)
    {
        if (_invincibilityTimer > 0) return;

        var damageSurplus = 0;
        if (PurpleHealth > 0)
        {
            PurpleHealth -= damage;

            if (PurpleHealth > pMaxHealth)
            {
                damageSurplus = PurpleHealth - pMaxHealth;
            }
        }
        else
        {
            damageSurplus = damage;
        }
        
        Health -= damageSurplus;
        DamageTaken?.Invoke();
        
        if (Health <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int healAmount)
    {
        Health += healAmount;
        if (Health > maxHealth)
        {
            Health = maxHealth;
        }
        UpdateHealthBar();
    }

    public void InstantDeath()
    {
        Health = 0;
        DamageTaken?.Invoke();
        
        Die();
    }
    
    private void SetInvincibilityTimer()
    {
        // For JAM only
        damageSource.Play();
        
        _invincibilityTimer = InvincibilityTime;
        StartCoroutine(InvincibilityTimer());
    }

    private IEnumerator InvincibilityTimer()
    {
        while (_invincibilityTimer > 0)
        {
            _invincibilityTimer -= Time.deltaTime;
            yield return null;
        }
    }
    
    private void UpdateHealthBar()
    {
        var healthBarSize = healthBar.rectTransform;
        healthBarSize.sizeDelta = new Vector2(Health * 50, healthBarSize.sizeDelta.y);
        healthBar.rectTransform.sizeDelta = healthBarSize.sizeDelta;
        
        var pHealthBarSize = purpleHealthBar.rectTransform;
        pHealthBarSize.sizeDelta = new Vector2(PurpleHealth * 50, pHealthBarSize.sizeDelta.y);
        purpleHealthBar.rectTransform.sizeDelta = pHealthBarSize.sizeDelta;
    }
    
    private void Die()
    {
        GameStateManager.Instance.SetGameState(GameStateManager.GameState.Lose);
    }
}
