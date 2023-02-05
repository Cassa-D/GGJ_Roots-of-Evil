using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class ComboCharacter : MonoBehaviour
{
    private StateMachine meleeStateMachine;

    [SerializeField] public Collider2D hitbox;
    [SerializeField] private PlayerInput playerInput;
    
    // For JAM only
    [SerializeField] public AudioSource attackSource;

    // Start is called before the first frame update
    void Start()
    {
        meleeStateMachine = GetComponent<StateMachine>();
    }

    // Update is called once per frame
    void Update()
    {
        if (DialogueManager.Instance.DialogueIsPlaying || Time.timeScale == 0) return;
        if (playerInput.FrameInput.AttackDown && meleeStateMachine.CurrentState.GetType() == typeof(IdleCombatState))
        {
            meleeStateMachine.SetNextState(new GroundEntryState());
        }
    }
}
