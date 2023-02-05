using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Animator _animator;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            _animator.SetBool("Attack", true);
            GetComponentInChildren<EnemyController>().enabled = false;
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _animator.SetBool("Attack", false);
            GetComponentInChildren<EnemyController>().enabled = true;
        }
    }

    public void OnAttack()
    {
        player.GetComponent<PlayerHealthHandler>().TakeDamage(1);
    }
}
