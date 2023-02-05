using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cue")]
    [SerializeField] private GameObject visualCue;
    
    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;
    
    [SerializeField] private string characterName;
    
    [SerializeField] private bool repeatable;
    private bool _alreadyTalked;

    private bool _playerInRange;

    private void Awake()
    {
        visualCue.SetActive(false);
        _playerInRange = false;
    }

    private void Update()
    {
        if (!repeatable && _alreadyTalked) return;
        
        if (_playerInRange && !DialogueManager.Instance.DialogueIsPlaying)
        {
            visualCue.SetActive(true);
            
            if (DialogueManager.Instance.Input.FrameInput.InteractDown)
            {
                _alreadyTalked = true;
                visualCue.SetActive(false);
                DialogueManager.Instance.EnterDialogueMode(inkJSON, characterName);
            }
        }
        else
        {
            visualCue.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            _playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
        }
    }
}
