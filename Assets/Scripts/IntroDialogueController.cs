using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroDialogueController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image blackOverlay;

    [Header("Dialogue")]
    public string characterName = "Character";

    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Reveal Settings")]
    public int revealBackgroundAfterLine = 2;
    public bool fadeBlackOverlay = true;
    public float fadeDuration = 1f;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool backgroundRevealed = false;
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (textBox != null)
        {
            textBox.SetActive(true);
        }

        if (blackOverlay != null)
        {
            Color color = blackOverlay.color;
            color.a = 1f;
            blackOverlay.color = color;
            blackOverlay.gameObject.SetActive(true);
        }

        if (nameText != null)
        {
            nameText.text = characterName;
        }

        StartCurrentLine();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            HandleEnterPressed();
        }
    }

    private void HandleEnterPressed()
    {
        if (isTyping)
        {
            FinishCurrentLineInstantly();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        StartCurrentLine();

        if (!backgroundRevealed && currentLineIndex >= revealBackgroundAfterLine)
        {
            backgroundRevealed = true;
            RevealBackground();
        }
    }

    private void StartCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(dialogueLines[currentLineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
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
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = dialogueLines[currentLineIndex];
        isTyping = false;
        typingCoroutine = null;
    }

    private void RevealBackground()
    {
        if (blackOverlay == null)
        {
            return;
        }

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

    private void EndDialogue()
    {
        if (textBox != null)
        {
            textBox.SetActive(false);
        }
    }
}