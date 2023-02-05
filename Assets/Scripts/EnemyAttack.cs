using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Animator _animator;
    private AudioSource _attackSource;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _attackSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            _animator.SetTrigger("Attack");
            _animator.SetBool("IsWalking", false);
            GetComponentInChildren<EnemyController>().enabled = false;
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            GetComponentInChildren<EnemyController>().enabled = true;
        }
    }

    public void OnAttack()
    {
        _attackSource.Play();
        player.GetComponent<PlayerHealthHandler>().TakeDamage(1);
    }
}
