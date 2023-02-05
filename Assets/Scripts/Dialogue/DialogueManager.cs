using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TarodevController;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text nameText;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    private TMP_Text[] _choicesText;

    private Story _currentStory;
    public bool DialogueIsPlaying { get; private set; }
    
    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    public PlayerInput Input => playerInput;
    
    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Found more than one instance of DialogueManager in the scene!");
        }
        Instance = this;
    }

    private void Start()
    {
        DialogueIsPlaying = false;
        dialoguePanel.SetActive(false);

        _choicesText = new TMP_Text[choices.Length];
        for (var i = 0; i < choices.Length; i++)
        {
            _choicesText[i] = choices[i].GetComponentInChildren<TMP_Text>();
        }
    }

    private void Update()
    {
        if (!DialogueIsPlaying) return;

        if (playerInput.FrameInput.SubmitDown)
            ContinueStory();
    }

    public void EnterDialogueMode(TextAsset inkJson, string characterName)
    {
        if (DialogueIsPlaying) return;
        
        DialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        _currentStory = new Story(inkJson.text);
        
        if (characterName == null)
        {
            nameText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            nameText.text = characterName;
        }

        ContinueStory();
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForEndOfFrame();
        
        DialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
    }
    
    private void ContinueStory()
    {
        if (_currentStory.canContinue)
        {
            dialogueText.text = _currentStory.Continue();
            DisplayChoices();
        }
        else
        {
            StartCoroutine(ExitDialogueMode());
        }
    }

    private void DisplayChoices()
    {
        var currentChoices = _currentStory.currentChoices;
        
        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("Not enough choices UI elements!");
        }

        var lastIndex = -1;
        for (var i = 0; i < currentChoices.Count; i++)
        {
            choices[i].SetActive(true);
            _choicesText[i].text = currentChoices[i].text;

            lastIndex = i;
        }
        
        for (var i = lastIndex+1; i < choices.Length; i++)
        {
            choices[i].SetActive(false);
        }

        StartCoroutine(SelectFirstChoice());
    }
    
    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        
        EventSystem.current.SetSelectedGameObject(choices[0]);
    }

    public void MakeChoice(int choiceIndex) 
    {
        _currentStory.ChooseChoiceIndex(choiceIndex);
    }
}
