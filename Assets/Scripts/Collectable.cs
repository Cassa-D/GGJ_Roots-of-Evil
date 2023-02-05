using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using TarodevController;
using UnityEngine;

public enum CollectableType
{
    Potion,
    PowerUp,
}

public enum PowerUpType
{
    Jump,
    Dash
}

[RequireComponent(typeof(Collider2D))]
public class Collectable : MonoBehaviour
{
    [SerializeField] private CollectableType type;
    public CollectableType Type => type;
    
    [SerializeField]
    [ShowIf("type", CollectableType.Potion)]
    private int quantity;
    public int Quantity => quantity;
    
    [SerializeField]
    [ShowIf("type", CollectableType.PowerUp)]
    private  PowerUpType powerUpType;
    public PowerUpType PowerUpType => powerUpType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        other.GetComponent<InteractionHandler>().SetCollectable(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        other.GetComponent<InteractionHandler>().SetCollectable(null);
    }
}
