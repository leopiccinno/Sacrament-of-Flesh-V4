using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActDialogueController : MonoBehaviour
{
    // Die verschiedenen Phasen des Dialogs
    private enum DialogueState
    {
        IntroLines,      // linearer Text bis zur Entscheidung
        ShowingChoices,  // die Antwort-Buttons sind sichtbar
        BranchLines,     // der gewaehlte Antwort-Zweig laeuft
        EndingLines,     // gemeinsame Zeilen nach dem Zweig (optional)
        Finished         // fertig -> naechste Szene
    }

    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Choice UI")]
    public GameObject choicePanel;               // Panel, das die Antwort-Buttons enthaelt
    public Button[] choiceButtons;               // die Antwort-Buttons (z.B. 4 Stueck)
    public TextMeshProUGUI[] choiceButtonLabels; // die Beschriftung auf jedem Button

    [Header("Optional End Button")]
    public GameObject nextSceneButton;           // erscheint am Ende -> laedt naechste Szene

    [Header("Dialogue")]
    public string characterName = "YOU";

    [TextArea(2, 5)]
    public string[] introLines;                  // der lineare Teil bis zur Entscheidung

    public DialogueChoice[] choices;             // die Antwortmoeglichkeiten

    [TextArea(2, 5)]
    public string[] endingLines;                 // gemeinsame Zeilen nach JEDEM Zweig (kann leer bleiben)

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private DialogueState state = DialogueState.IntroLines;
    private string[] currentLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // Eine einzelne Antwortmoeglichkeit (Button-Text + der Text, der danach kommt)
    [System.Serializable]
    public class DialogueChoice
    {
        public string buttonText;        // z.B. "Yes."
        [TextArea(2, 5)]
        public string[] resultLines;     // die Zeilen, die nach dieser Wahl gezeigt werden
    }

    private void Start()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (nextSceneButton != null)
            nextSceneButton.SetActive(false);

        if (textBox != null)
            textBox.SetActive(true);

        if (nameText != null)
            nameText.text = characterName;

        StartLines(introLines, DialogueState.IntroLines);
    }

    private void Update()
    {
        // Weiterschalten mit Enter (wie in deiner Opening Scene)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (state == DialogueState.IntroLines ||
                state == DialogueState.BranchLines ||
                state == DialogueState.EndingLines)
            {
                HandleEnter();
            }
        }
    }

    private void StartLines(string[] lines, DialogueState newState)
    {
        state = newState;
        currentLines = lines;
        currentLineIndex = 0;

        if (textBox != null)
            textBox.SetActive(true);

        if (currentLines == null || currentLines.Length == 0)
        {
            FinishCurrentLines();
            return;
        }

        StartCurrentLine();
    }

    private void HandleEnter()
    {
        // Wenn gerade getippt wird: Zeile sofort komplett anzeigen
        if (isTyping)
        {
            FinishLineInstantly();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentLines.Length)
        {
            FinishCurrentLines();
            return;
        }

        StartCurrentLine();
    }

    private void StartCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(currentLines[currentLineIndex]));
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

    private void FinishLineInstantly()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null)
            dialogueText.text = currentLines[currentLineIndex];

        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishCurrentLines()
    {
        if (state == DialogueState.IntroLines)
        {
            // linearer Teil vorbei -> Entscheidung anzeigen
            ShowChoices();
        }
        else if (state == DialogueState.BranchLines)
        {
            // Zweig vorbei -> gemeinsame Schlusszeilen, falls vorhanden, sonst Ende
            if (endingLines != null && endingLines.Length > 0)
                StartLines(endingLines, DialogueState.EndingLines);
            else
                Finish();
        }
        else if (state == DialogueState.EndingLines)
        {
            Finish();
        }
    }

    private void ShowChoices()
    {
        state = DialogueState.ShowingChoices;

        if (textBox != null)
            textBox.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);

                if (i < choiceButtonLabels.Length && choiceButtonLabels[i] != null)
                    choiceButtonLabels[i].text = choices[i].buttonText;

                int choiceIndex = i; // eigene Variable pro Button (wichtig in der Schleife!)
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            }
            else
            {
                // ueberzaehlige Buttons ausblenden
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // Wird aufgerufen, wenn der Spieler eine Antwort anklickt
    public void OnChoiceSelected(int index)
    {
        if (state != DialogueState.ShowingChoices)
            return;

        if (choicePanel != null)
            choicePanel.SetActive(false);

        StartLines(choices[index].resultLines, DialogueState.BranchLines);
    }

    private void Finish()
    {
        state = DialogueState.Finished;

        if (textBox != null)
            textBox.SetActive(false);

        if (nextSceneButton != null)
            nextSceneButton.SetActive(true);
    }
}
