using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroDialogueController : MonoBehaviour
{
    private enum DialogueState
    {
        IntroDialogue,
        WaitingForNoteClick,
        ReadingNote,
        AfterNoteDialogue,
        Finished
    }

    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image blackOverlay;

   [Header("Note UI")]
public bool useNoteInteraction = true;
public GameObject button;
public GameObject bigNotePanel;

    [Header("Optional End Button")]
    public GameObject nextSceneButton;

    [Header("Dialogue")]
    public string characterName = "YOU";

    [TextArea(2, 5)]
    public string[] introDialogueLines;

    [TextArea(2, 5)]
    public string[] afterNoteDialogueLines;

    [Header("Pause Settings")]
    public string pauseMarker = "[PAUSE]";

    [Header("Overlay Settings")]
    public bool useBlackOverlay = true;
    public int revealBackgroundAfterLine = 2;
    public bool fadeBlackOverlay = true;
    public float fadeDuration = 1f;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private DialogueState state = DialogueState.IntroDialogue;

    private string[] currentDialogueLines;
    private int currentLineIndex = 0;

    private bool isTyping = false;
    private bool isPaused = false;
    private bool backgroundRevealed = false;

    private Coroutine typingCoroutine;

   private void Start()
{
    if (button != null)
        button.SetActive(false);

    if (bigNotePanel != null)
        bigNotePanel.SetActive(false);

    if (nextSceneButton != null)
        nextSceneButton.SetActive(false);

    if (textBox != null)
        textBox.SetActive(true);

    if (blackOverlay != null)
    {
        if (useBlackOverlay)
        {
            Color color = blackOverlay.color;
            color.a = 1f;
            blackOverlay.color = color;
            blackOverlay.gameObject.SetActive(true);
        }
        else
        {
            blackOverlay.gameObject.SetActive(false);
        }
    }

    if (nameText != null)
        nameText.text = characterName;

    StartDialogue(introDialogueLines, DialogueState.IntroDialogue);
}


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (state == DialogueState.ReadingNote)
            {
                CloseNoteAndStartAfterDialogue();
                return;
            }

            if (state == DialogueState.IntroDialogue || state == DialogueState.AfterNoteDialogue)
            {
                HandleDialogueEnter();
            }
        }
    }

    private void StartDialogue(string[] lines, DialogueState newState)
    {
        state = newState;
        currentDialogueLines = lines;
        currentLineIndex = 0;
        isPaused = false;

        if (textBox != null)
            textBox.SetActive(true);

        if (nameText != null)
            nameText.text = characterName;

        if (currentDialogueLines == null || currentDialogueLines.Length == 0)
        {
            FinishCurrentDialogueSequence();
            return;
        }

        StartCurrentLine();
    }

    private void HandleDialogueEnter()
    {
        if (isPaused)
        {
            isPaused = false;

            if (textBox != null)
                textBox.SetActive(true);

            currentLineIndex++;

            if (currentLineIndex >= currentDialogueLines.Length)
            {
                FinishCurrentDialogueSequence();
                return;
            }

            StartCurrentLine();
            CheckBackgroundReveal();
            return;
        }

        if (isTyping)
        {
            FinishCurrentLineInstantly();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentDialogueLines.Length)
        {
            FinishCurrentDialogueSequence();
            return;
        }

        StartCurrentLine();
        CheckBackgroundReveal();
    }

    private void StartCurrentLine()
    {
        if (currentDialogueLines[currentLineIndex] == pauseMarker)
        {
            StartPause();
            return;
        }

        if (textBox != null)
            textBox.SetActive(true);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(currentDialogueLines[currentLineIndex]));
    }

    private void StartPause()
    {
        isPaused = true;
        isTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueText != null)
            dialogueText.text = "";

        if (textBox != null)
            textBox.SetActive(false);
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char letter in line)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishCurrentLineInstantly()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null)
            dialogueText.text = currentDialogueLines[currentLineIndex];

        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishCurrentDialogueSequence()
{
    if (textBox != null)
        textBox.SetActive(false);

    if (state == DialogueState.IntroDialogue)
    {
        if (useNoteInteraction)
        {
            state = DialogueState.WaitingForNoteClick;

            if (button != null)
                button.SetActive(true);
        }
        else
        {
            state = DialogueState.Finished;

            if (nextSceneButton != null)
                nextSceneButton.SetActive(true);
            else
                Debug.LogWarning("NextSceneButton is not assigned in the IntroDialogueController.");
        }
    }
    else if (state == DialogueState.AfterNoteDialogue)
    {
        state = DialogueState.Finished;

        if (nextSceneButton != null)
            nextSceneButton.SetActive(true);
        else
            Debug.LogWarning("NextSceneButton is not assigned in the IntroDialogueController.");
    }
}

    public void OnNoteClicked()
    {
        if (state != DialogueState.WaitingForNoteClick)
            return;

        state = DialogueState.ReadingNote;

        if (button != null)
            button.SetActive(false);

        if (textBox != null)
            textBox.SetActive(false);

        if (bigNotePanel != null)
            bigNotePanel.SetActive(true);
    }

    private void CloseNoteAndStartAfterDialogue()
    {
        if (bigNotePanel != null)
            bigNotePanel.SetActive(false);

        StartDialogue(afterNoteDialogueLines, DialogueState.AfterNoteDialogue);
    }

    private void CheckBackgroundReveal()
    {
        if (!useBlackOverlay)
            return;

        if (!backgroundRevealed && currentLineIndex >= revealBackgroundAfterLine)
        {
            backgroundRevealed = true;
            RevealBackground();
        }
    }

    private void RevealBackground()
    {
        if (blackOverlay == null)
            return;

        if (fadeBlackOverlay)
        {
            StartCoroutine(FadeOutBlackOverlay());
        }
        else
        {
            blackOverlay.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOutBlackOverlay()
    {
        float timer = 0f;
        Color startColor = blackOverlay.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            Color color = startColor;
            color.a = alpha;
            blackOverlay.color = color;

            yield return null;
        }

        Color finalColor = blackOverlay.color;
        finalColor.a = 0f;
        blackOverlay.color = finalColor;

        blackOverlay.gameObject.SetActive(false);
    }

    public void StartAfterNoteDialogue()
{
    StartDialogue(afterNoteDialogueLines, DialogueState.AfterNoteDialogue);
}


}