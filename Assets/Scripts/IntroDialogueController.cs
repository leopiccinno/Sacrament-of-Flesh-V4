using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class IntroDialogueController : MonoBehaviour
{
    private enum DialogueState
    {
        IntroDialogue,
        WaitingForNoteClick,
        ReadingNote,
        AfterNoteDialogue,
        ShowingChoices,
        BranchLines,
        EndingLines,
        Finished
    }

    public enum DialogueSection
    {
        IntroDialogue,
        AfterNoteDialogue,
        BranchLines,
        EndingLines
    }

    [System.Serializable]
    public class SpeakerChange
    {
        public DialogueSection section;
        public int lineIndex;
        public string speakerName;
    }

    [System.Serializable]
    public class SpeakerChangeInChoice
    {
        public int lineIndex;
        public string speakerName;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string buttonText;

        [TextArea(2, 5)]
        public string[] resultLines;

        public SpeakerChangeInChoice[] speakerChanges;
    }

    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image blackOverlay;

    [Header("Note / Card Interaction")]
    public bool useNoteInteraction = true;

    [Tooltip("This can be a note button, card button, or any interactable object.")]
    public GameObject button;

    public GameObject bigNotePanel;

    [Header("Choice UI")]
    public GameObject choicePanel;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceButtonLabels;

    [Header("Optional End Button")]
    public GameObject nextSceneButton;
    public string autoNextSceneName = "";

    [Header("Dialogue")]
    public string characterName = "YOU";

    [TextArea(2, 5)]
    public string[] introDialogueLines;

    [TextArea(2, 5)]
    public string[] afterNoteDialogueLines;

    public DialogueChoice[] choices;

    [TextArea(2, 5)]
    public string[] endingLines;

    [Header("Speaker Name Changes")]
    public string defaultSpeakerName = "YOU";
    public SpeakerChange[] speakerChanges;

    [Header("Pause Settings")]
    public string pauseMarker = "[PAUSE]";

    [Header("Overlay Settings")]
    public bool useBlackOverlay = true;
    public int revealBackgroundAfterLine = 2;
    public bool fadeBlackOverlay = true;
    public float fadeDuration = 1f;

    [Header("Character Appearance")]
    public GameObject characterObject;
    public Image characterImage;
    public Sprite characterSprite;

    [Tooltip("Bei welcher Intro-Zeile der Character erscheinen soll. -1 = nie.")]
    public int characterAppearsAtIntroLine = -1;

    public bool fadeCharacterIn = true;
    public float characterFadeDuration = 1f;

    [Tooltip("Bei welcher Intro-Zeile der Character wieder verschwinden soll. -1 = nie.")]
    public int characterLeavesAtIntroLine = -1;

    [Header("Gathering Characters")]
    public GameObject gatheringCharactersObject;

    [Tooltip("Bei welcher Intro-Zeile die Gruppe erscheinen soll. -1 = nie.")]
    public int gatheringAppearsAtIntroLine = -1;

    [Header("Background Change")]
    public SpriteRenderer backgroundRenderer;

    public Sprite secondBackground;
    public int changeBackgroundAtIntroLine = -1;

    public Sprite thirdBackground;
    public int changeBackgroundAtIntroLine2 = -1;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private DialogueState state = DialogueState.IntroDialogue;

    private string[] currentLines;
    private int currentLineIndex = 0;

    private bool isTyping = false;
    private bool isPaused = false;
    private bool backgroundRevealed = false;

    private bool characterHasAppeared = false;
    private bool characterHasLeft = false;
    private bool gatheringHasAppeared = false;
    private bool backgroundHasChanged = false;
    private bool backgroundHasChanged2 = false;

    private Coroutine typingCoroutine;
    private Coroutine characterFadeCoroutine;

    private DialogueChoice currentChoice;

    private void Start()
    {
        if (button != null)
            button.SetActive(false);

        if (bigNotePanel != null)
            bigNotePanel.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (nextSceneButton != null)
            nextSceneButton.SetActive(false);

        if (gatheringCharactersObject != null)
            gatheringCharactersObject.SetActive(false);

        if (textBox != null)
            textBox.SetActive(true);

        SetupBlackOverlay();
        SetupCharacterAtStart();

        if (nameText != null)
            nameText.text = defaultSpeakerName;

        StartLines(introDialogueLines, DialogueState.IntroDialogue);
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

            if (state == DialogueState.IntroDialogue ||
                state == DialogueState.AfterNoteDialogue ||
                state == DialogueState.BranchLines ||
                state == DialogueState.EndingLines)
            {
                HandleEnter();
            }
        }
    }

    private void SetupBlackOverlay()
    {
        if (blackOverlay == null)
            return;

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

    private void SetupCharacterAtStart()
    {
        if (characterImage != null && characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.preserveAspect = true;
        }

        if (characterObject != null)
            characterObject.SetActive(false);

        if (characterImage != null)
        {
            Color color = characterImage.color;
            color.a = 0f;
            characterImage.color = color;
        }

        characterHasAppeared = false;
        characterHasLeft = false;
    }

    private void StartLines(string[] lines, DialogueState newState)
    {
        state = newState;
        currentLines = lines;
        currentLineIndex = 0;
        isPaused = false;

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
        if (isPaused)
        {
            isPaused = false;

            if (textBox != null)
                textBox.SetActive(true);

            currentLineIndex++;

            if (currentLineIndex >= currentLines.Length)
            {
                FinishCurrentLines();
                return;
            }

            StartCurrentLine();
            return;
        }

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
        if (currentLines[currentLineIndex] == pauseMarker)
        {
            StartPause();
            return;
        }

        if (textBox != null)
            textBox.SetActive(true);

        CheckBlackOverlayReveal();
        CheckCharacterAppearance();
        CheckCharacterLeaves();
        CheckGatheringAppearance();
        CheckBackgroundChange();
        ApplySpeakerNameForCurrentLine();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(currentLines[currentLineIndex]));
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

    private void FinishLineInstantly()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null)
            dialogueText.text = currentLines[currentLineIndex];

        isTyping = false;
        typingCoroutine = null;
    }

    private void ApplySpeakerNameForCurrentLine()
    {
        if (nameText == null)
            return;

        DialogueSection currentSection = GetCurrentDialogueSection();

        string speakerToUse = defaultSpeakerName;

        if (speakerChanges != null)
        {
            foreach (SpeakerChange speakerChange in speakerChanges)
            {
                if (speakerChange.section == currentSection &&
                    speakerChange.lineIndex == currentLineIndex)
                {
                    speakerToUse = speakerChange.speakerName;
                }
            }
        }

        if (state == DialogueState.BranchLines &&
            currentChoice != null &&
            currentChoice.speakerChanges != null)
        {
            foreach (SpeakerChangeInChoice speakerChange in currentChoice.speakerChanges)
            {
                if (speakerChange.lineIndex == currentLineIndex)
                {
                    speakerToUse = speakerChange.speakerName;
                }
            }
        }

        nameText.text = speakerToUse;
    }

    private DialogueSection GetCurrentDialogueSection()
    {
        if (state == DialogueState.AfterNoteDialogue)
            return DialogueSection.AfterNoteDialogue;

        if (state == DialogueState.BranchLines)
            return DialogueSection.BranchLines;

        if (state == DialogueState.EndingLines)
            return DialogueSection.EndingLines;

        return DialogueSection.IntroDialogue;
    }

    private void CheckCharacterAppearance()
    {
        if (characterHasAppeared)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        if (characterAppearsAtIntroLine < 0)
            return;

        if (currentLineIndex < characterAppearsAtIntroLine)
            return;

        ShowCharacter();
    }

    private void ShowCharacter()
    {
        characterHasAppeared = true;

        if (characterObject != null)
            characterObject.SetActive(true);

        if (characterImage != null && characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.preserveAspect = true;
        }

        if (characterImage != null)
        {
            if (characterFadeCoroutine != null)
                StopCoroutine(characterFadeCoroutine);

            if (fadeCharacterIn)
                characterFadeCoroutine = StartCoroutine(FadeInCharacter());
            else
            {
                Color color = characterImage.color;
                color.a = 1f;
                characterImage.color = color;
            }
        }
    }

    private IEnumerator FadeInCharacter()
    {
        float timer = 0f;

        Color color = characterImage.color;
        color.a = 0f;
        characterImage.color = color;

        while (timer < characterFadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(0f, 1f, timer / characterFadeDuration);

            color = characterImage.color;
            color.a = alpha;
            characterImage.color = color;

            yield return null;
        }

        color = characterImage.color;
        color.a = 1f;
        characterImage.color = color;

        characterFadeCoroutine = null;
    }

    private void CheckCharacterLeaves()
    {
        if (characterHasLeft)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        if (characterLeavesAtIntroLine < 0)
            return;

        if (currentLineIndex < characterLeavesAtIntroLine)
            return;

        HideCharacter();
    }

    private void HideCharacter()
    {
        characterHasLeft = true;

        if (characterImage != null && fadeCharacterIn)
        {
            if (characterFadeCoroutine != null)
                StopCoroutine(characterFadeCoroutine);

            characterFadeCoroutine = StartCoroutine(FadeOutCharacter());
        }
        else
        {
            if (characterImage != null)
            {
                Color color = characterImage.color;
                color.a = 0f;
                characterImage.color = color;
            }

            if (characterObject != null)
                characterObject.SetActive(false);
        }
    }

    private IEnumerator FadeOutCharacter()
    {
        float timer = 0f;

        while (timer < characterFadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, timer / characterFadeDuration);

            Color color = characterImage.color;
            color.a = alpha;
            characterImage.color = color;

            yield return null;
        }

        Color finalColor = characterImage.color;
        finalColor.a = 0f;
        characterImage.color = finalColor;

        if (characterObject != null)
            characterObject.SetActive(false);

        characterFadeCoroutine = null;
    }

    private void CheckGatheringAppearance()
    {
        if (gatheringHasAppeared)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        if (gatheringAppearsAtIntroLine < 0)
            return;

        if (currentLineIndex < gatheringAppearsAtIntroLine)
            return;

        if (gatheringCharactersObject != null)
            gatheringCharactersObject.SetActive(true);

        gatheringHasAppeared = true;
    }

    private void CheckBackgroundChange()
    {
        if (backgroundRenderer == null)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        if (!backgroundHasChanged &&
            secondBackground != null &&
            changeBackgroundAtIntroLine >= 0 &&
            currentLineIndex >= changeBackgroundAtIntroLine)
        {
            backgroundRenderer.sprite = secondBackground;
            backgroundHasChanged = true;
        }

        if (!backgroundHasChanged2 &&
            thirdBackground != null &&
            changeBackgroundAtIntroLine2 >= 0 &&
            currentLineIndex >= changeBackgroundAtIntroLine2)
        {
            backgroundRenderer.sprite = thirdBackground;
            backgroundHasChanged2 = true;
        }
    }

    private void CheckBlackOverlayReveal()
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

    private void FinishCurrentLines()
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
                else
                    Debug.LogWarning("Interaction button is not assigned in IntroDialogueController.");
            }
            else
            {
                if (choices != null && choices.Length > 0)
                    ShowChoices();
                else if (endingLines != null && endingLines.Length > 0)
                    StartLines(endingLines, DialogueState.EndingLines);
                else
                    Finish();
            }
        }
        else if (state == DialogueState.AfterNoteDialogue)
        {
            if (choices != null && choices.Length > 0)
                ShowChoices();
            else if (endingLines != null && endingLines.Length > 0)
                StartLines(endingLines, DialogueState.EndingLines);
            else
                Finish();
        }
        else if (state == DialogueState.BranchLines)
        {
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

        StartAfterNoteDialogue();
    }

    public void StartAfterNoteDialogue()
    {
        if (button != null)
            button.SetActive(false);

        if (bigNotePanel != null)
            bigNotePanel.SetActive(false);

        StartLines(afterNoteDialogueLines, DialogueState.AfterNoteDialogue);
    }

    private void ShowChoices()
    {
        state = DialogueState.ShowingChoices;

        if (textBox != null)
            textBox.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        if (choiceButtons == null)
            return;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
                continue;

            if (choices != null && i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);

                if (choiceButtonLabels != null &&
                    i < choiceButtonLabels.Length &&
                    choiceButtonLabels[i] != null)
                {
                    choiceButtonLabels[i].text = choices[i].buttonText;
                }

                int choiceIndex = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnChoiceSelected(int index)
    {
        if (state != DialogueState.ShowingChoices)
            return;

        if (choices == null || index < 0 || index >= choices.Length)
            return;

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (choiceButtons != null)
        {
            foreach (Button choiceButton in choiceButtons)
            {
                if (choiceButton != null)
                    choiceButton.gameObject.SetActive(false);
            }
        }

        currentChoice = choices[index];

        StartLines(currentChoice.resultLines, DialogueState.BranchLines);
    }

    private void Finish()
    {
        state = DialogueState.Finished;

        if (textBox != null)
            textBox.SetActive(false);

        if (!string.IsNullOrEmpty(autoNextSceneName))
        {
            SceneManager.LoadScene(autoNextSceneName);
        }
        else if (nextSceneButton != null)
        {
            nextSceneButton.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Dialogue finished, but no nextSceneButton or autoNextSceneName is assigned.");
        }
    }
}