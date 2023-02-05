using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float speed;
    
    private Rigidbody2D _rb;
    private Animator _animator;
    
    private bool _playerInRange;
    
    [SerializeField] private int maxHealth = 3;
    private int _currentHealth;
    
    private void Start()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _animator = GetComponentInParent<Animator>();
        _currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (!_playerInRange) return;

        _animator.SetBool("IsWalking", true);
        Vector2 direction = player.position - transform.position;
        direction.y = 0;
        direction.Normalize();
        _rb.velocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            _rb.velocity = Vector2.zero;
            _animator.SetBool("IsWalking", false);
        }
    }
    
    public void ReceiveDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            Destroy(gameObject.transform.parent.gameObject);
        }
    }
}
