using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ActDialogueController : MonoBehaviour
{
    private enum DialogueState
    {
        IntroDialogue,
        WaitingForNoteClick,
        ReadingNote,
        AfterNoteDialogue,
        SelectingCharacter,
        CharacterIntroLines,
        ShowingChoices,
        BranchLines,
        EndingLines,
        Finished
    }

    public enum DialogueSection
    {
        IntroDialogue,
        AfterNoteDialogue,
        CharacterIntroLines,
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
    public class BackgroundChange
    {
        public DialogueSection section;
        public int lineIndex;
        public Sprite backgroundSprite;
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

    [System.Serializable]
    public class GatheringCharacter
    {
        [Header("Character Object")]
        public string characterName;
        public GameObject characterObject;
        public Button characterButton;
        public RectTransform characterRectTransform;

        [Header("Positions")]
        public Vector2 normalPosition;
        public Vector2 focusedPosition;

        [Header("Dialogue Before Choices")]
        [TextArea(2, 5)]
        public string[] introLinesBeforeChoices;

        public SpeakerChangeInChoice[] introSpeakerChanges;

        [Header("Dialogue Choices")]
        public DialogueChoice[] choices;

        [Header("Runtime")]
        public bool hasTalkedToCharacter = false;
    }

    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image blackOverlay;

    [Header("Note / Card Interaction")]
    public bool useNoteInteraction = false;

    [Tooltip("This can be a note button, card button, or any interactable object.")]
    public GameObject button;

    public GameObject bigNotePanel;

    [Header("Gathering Characters")]
    public bool useGatheringCharacters = false;
    public GatheringCharacter[] gatheringCharacters;

    [Tooltip("Wenn true, verschwindet ein Character dauerhaft, nachdem man mit ihm gesprochen hat.")]
    public bool hideTalkedCharacters = true;

    [Header("Choice UI")]
    public GameObject choicePanel;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceButtonLabels;

    [Header("Optional End Button")]
    public GameObject nextSceneButton;
    public string autoNextSceneName = "";

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string[] introDialogueLines;

    [TextArea(2, 5)]
    public string[] afterNoteDialogueLines;

    [Tooltip("Allgemeine Choices, falls NICHT das Gathering-Character-System genutzt wird.")]
    public DialogueChoice[] choices;

    [TextArea(2, 5)]
    public string[] endingLines;

    [Header("Speaker Name Changes")]
    public string defaultSpeakerName = "YOU";
    public SpeakerChange[] speakerChanges;

    [Header("Pause Settings")]
    public string pauseMarker = "[PAUSE]";

    [Header("Overlay Settings")]
    public bool useBlackOverlay = false;
    public int revealBackgroundAfterLine = 2;
    public bool fadeBlackOverlay = true;
    public float fadeDuration = 1f;

    [Header("Intro Fading Character")]
    public GameObject characterObject;
    public Image characterImage;
    public Sprite characterSprite;

    [Tooltip("Bei welcher Intro-Zeile der Character erscheinen soll. -1 = nie.")]
    public int characterAppearsAtIntroLine = -1;

    [Tooltip("Bei welcher Intro-Zeile der Character verschwinden soll. -1 = nie.")]
    public int characterLeavesAtIntroLine = -1;

    public bool fadeCharacterIn = true;
    public float characterFadeDuration = 1f;

    [Header("Background Change")]
    public SpriteRenderer backgroundRenderer;
    public BackgroundChange[] backgroundChanges;

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

    private bool[] backgroundChangeTriggered;

    private Coroutine typingCoroutine;
    private Coroutine characterFadeCoroutine;

    private DialogueChoice currentChoice;
    private GatheringCharacter currentCharacter;

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

        if (textBox != null)
            textBox.SetActive(true);

        if (nameText != null)
            nameText.text = defaultSpeakerName;

        SetupBlackOverlay();
        SetupIntroCharacterAtStart();
        SetupGatheringCharactersAtStart();
        SetupBackgroundChanges();

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
                state == DialogueState.CharacterIntroLines ||
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

    private void SetupIntroCharacterAtStart()
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

    private void SetupGatheringCharactersAtStart()
    {
        if (gatheringCharacters == null)
            return;

        for (int i = 0; i < gatheringCharacters.Length; i++)
        {
            GatheringCharacter character = gatheringCharacters[i];

            if (character == null)
                continue;

            character.hasTalkedToCharacter = false;

            if (character.characterObject != null)
                character.characterObject.SetActive(false);

            if (character.characterRectTransform != null)
                character.characterRectTransform.anchoredPosition = character.normalPosition;

            if (character.characterButton != null)
            {
                int characterIndex = i;
                character.characterButton.onClick.RemoveAllListeners();
                character.characterButton.onClick.AddListener(() => OnGatheringCharacterClicked(characterIndex));
            }
        }
    }

    private void SetupBackgroundChanges()
    {
        if (backgroundChanges != null)
            backgroundChangeTriggered = new bool[backgroundChanges.Length];
        else
            backgroundChangeTriggered = new bool[0];
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
        CheckIntroCharacterAppearance();
        CheckIntroCharacterLeaves();
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
            StartCoroutine(FadeOutBlackOverlay());
        else
            blackOverlay.gameObject.SetActive(false);
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

    private void CheckIntroCharacterAppearance()
    {
        if (characterHasAppeared)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        if (characterAppearsAtIntroLine < 0)
            return;

        if (currentLineIndex < characterAppearsAtIntroLine)
            return;

        ShowIntroCharacter();
    }

    private void ShowIntroCharacter()
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

    private void CheckIntroCharacterLeaves()
    {
        if (characterHasLeft)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        if (characterLeavesAtIntroLine < 0)
            return;

        if (currentLineIndex < characterLeavesAtIntroLine)
            return;

        HideIntroCharacter();
    }

    private void HideIntroCharacter()
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

    private void CheckBackgroundChange()
    {
        if (backgroundRenderer == null)
            return;

        if (backgroundChanges == null || backgroundChanges.Length == 0)
            return;

        DialogueSection currentSection = GetCurrentDialogueSection();

        for (int i = 0; i < backgroundChanges.Length; i++)
        {
            BackgroundChange backgroundChange = backgroundChanges[i];

            if (backgroundChange == null)
                continue;

            if (backgroundChangeTriggered != null &&
                i < backgroundChangeTriggered.Length &&
                backgroundChangeTriggered[i])
                continue;

            if (backgroundChange.backgroundSprite == null)
                continue;

            if (backgroundChange.section != currentSection)
                continue;

            if (currentLineIndex < backgroundChange.lineIndex)
                continue;

            backgroundRenderer.sprite = backgroundChange.backgroundSprite;

            if (backgroundChangeTriggered != null &&
                i < backgroundChangeTriggered.Length)
            {
                backgroundChangeTriggered[i] = true;
            }
        }
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

        if (state == DialogueState.CharacterIntroLines &&
            currentCharacter != null &&
            currentCharacter.introSpeakerChanges != null)
        {
            foreach (SpeakerChangeInChoice speakerChange in currentCharacter.introSpeakerChanges)
            {
                if (speakerChange.lineIndex == currentLineIndex)
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

        if (state == DialogueState.CharacterIntroLines)
            return DialogueSection.CharacterIntroLines;

        if (state == DialogueState.BranchLines)
            return DialogueSection.BranchLines;

        if (state == DialogueState.EndingLines)
            return DialogueSection.EndingLines;

        return DialogueSection.IntroDialogue;
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
                    Debug.LogWarning("Interaction button is not assigned.");
            }
            else
            {
                ContinueAfterMainDialogue();
            }
        }
        else if (state == DialogueState.AfterNoteDialogue)
        {
            ContinueAfterMainDialogue();
        }
        else if (state == DialogueState.CharacterIntroLines)
        {
            if (currentCharacter != null)
                ShowChoicesForCharacter(currentCharacter);
            else
                ShowGatheringCharacters();
        }
        else if (state == DialogueState.BranchLines)
        {
            if (currentCharacter != null)
                FinishCurrentCharacterConversation();
            else
                ContinueAfterBranchDialogue();
        }
        else if (state == DialogueState.EndingLines)
        {
            Finish();
        }
    }

    private void ContinueAfterMainDialogue()
    {
        if (useGatheringCharacters)
        {
            ShowGatheringCharacters();
        }
        else if (choices != null && choices.Length > 0)
        {
            ShowChoices();
        }
        else if (endingLines != null && endingLines.Length > 0)
        {
            StartLines(endingLines, DialogueState.EndingLines);
        }
        else
        {
            Finish();
        }
    }

    private void ContinueAfterBranchDialogue()
    {
        if (endingLines != null && endingLines.Length > 0)
            StartLines(endingLines, DialogueState.EndingLines);
        else
            Finish();
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

        HideChoiceButtons();

        currentChoice = choices[index];
        currentCharacter = null;

        StartLines(currentChoice.resultLines, DialogueState.BranchLines);
    }

    private void ShowGatheringCharacters()
    {
        state = DialogueState.SelectingCharacter;

        if (textBox != null)
            textBox.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (AllCharactersTalkedTo())
        {
            StartEndingDialogue();
            return;
        }

        for (int i = 0; i < gatheringCharacters.Length; i++)
        {
            GatheringCharacter character = gatheringCharacters[i];

            if (character == null || character.characterObject == null)
                continue;

            bool shouldShowCharacter = !character.hasTalkedToCharacter || !hideTalkedCharacters;

            character.characterObject.SetActive(shouldShowCharacter);

            if (character.characterRectTransform != null)
                character.characterRectTransform.anchoredPosition = character.normalPosition;

            if (character.characterButton != null)
                character.characterButton.interactable = !character.hasTalkedToCharacter;
        }
    }

    public void OnGatheringCharacterClicked(int index)
    {
        if (state != DialogueState.SelectingCharacter)
            return;

        if (gatheringCharacters == null)
            return;

        if (index < 0 || index >= gatheringCharacters.Length)
            return;

        GatheringCharacter selectedCharacter = gatheringCharacters[index];

        if (selectedCharacter == null)
            return;

        if (selectedCharacter.hasTalkedToCharacter)
            return;

        currentCharacter = selectedCharacter;

        FocusSelectedCharacter(selectedCharacter);

        if (selectedCharacter.introLinesBeforeChoices != null &&
            selectedCharacter.introLinesBeforeChoices.Length > 0)
        {
            StartLines(selectedCharacter.introLinesBeforeChoices, DialogueState.CharacterIntroLines);
        }
        else
        {
            ShowChoicesForCharacter(selectedCharacter);
        }
    }

    private void FocusSelectedCharacter(GatheringCharacter selectedCharacter)
    {
        for (int i = 0; i < gatheringCharacters.Length; i++)
        {
            GatheringCharacter character = gatheringCharacters[i];

            if (character == null || character.characterObject == null)
                continue;

            if (character == selectedCharacter)
            {
                character.characterObject.SetActive(true);

                if (character.characterRectTransform != null)
                    character.characterRectTransform.anchoredPosition = character.focusedPosition;

                if (character.characterButton != null)
                    character.characterButton.interactable = false;
            }
            else
            {
                character.characterObject.SetActive(false);
            }
        }
    }

    private void ShowChoicesForCharacter(GatheringCharacter character)
    {
        state = DialogueState.ShowingChoices;

        if (textBox != null)
            textBox.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        DialogueChoice[] characterChoices = character.choices;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
                continue;

            if (characterChoices != null && i < characterChoices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);

                if (choiceButtonLabels != null &&
                    i < choiceButtonLabels.Length &&
                    choiceButtonLabels[i] != null)
                {
                    choiceButtonLabels[i].text = characterChoices[i].buttonText;
                }

                int choiceIndex = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnCharacterChoiceSelected(choiceIndex));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnCharacterChoiceSelected(int choiceIndex)
    {
        if (state != DialogueState.ShowingChoices)
            return;

        if (currentCharacter == null)
            return;

        if (currentCharacter.choices == null)
            return;

        if (choiceIndex < 0 || choiceIndex >= currentCharacter.choices.Length)
            return;

        HideChoiceButtons();

        currentChoice = currentCharacter.choices[choiceIndex];

        StartLines(currentChoice.resultLines, DialogueState.BranchLines);
    }

    private void HideChoiceButtons()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (choiceButtons == null)
            return;

        foreach (Button choiceButton in choiceButtons)
        {
            if (choiceButton != null)
                choiceButton.gameObject.SetActive(false);
        }
    }

    private void FinishCurrentCharacterConversation()
    {
        if (currentCharacter != null)
        {
            currentCharacter.hasTalkedToCharacter = true;

            if (hideTalkedCharacters && currentCharacter.characterObject != null)
                currentCharacter.characterObject.SetActive(false);

            if (currentCharacter.characterButton != null)
                currentCharacter.characterButton.interactable = false;
        }

        currentChoice = null;
        currentCharacter = null;

        if (AllCharactersTalkedTo())
            StartEndingDialogue();
        else
            ShowGatheringCharacters();
    }

    private bool AllCharactersTalkedTo()
    {
        if (gatheringCharacters == null || gatheringCharacters.Length == 0)
            return true;

        foreach (GatheringCharacter character in gatheringCharacters)
        {
            if (character == null)
                continue;

            if (!character.hasTalkedToCharacter)
                return false;
        }

        return true;
    }

    private void StartEndingDialogue()
    {
        HideTalkedCharactersBeforeEnding();

        if (endingLines != null && endingLines.Length > 0)
            StartLines(endingLines, DialogueState.EndingLines);
        else
            Finish();
    }

    private void HideTalkedCharactersBeforeEnding()
    {
        if (!hideTalkedCharacters)
            return;

        if (gatheringCharacters == null)
            return;

        foreach (GatheringCharacter character in gatheringCharacters)
        {
            if (character == null || character.characterObject == null)
                continue;

            if (character.hasTalkedToCharacter)
                character.characterObject.SetActive(false);
        }
    }

    private void Finish()
    {
        state = DialogueState.Finished;

        if (textBox != null)
            textBox.SetActive(false);

        HideAllGatheringCharacters();

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

    private void HideAllGatheringCharacters()
    {
        if (gatheringCharacters == null)
            return;

        foreach (GatheringCharacter character in gatheringCharacters)
        {
            if (character != null && character.characterObject != null)
                character.characterObject.SetActive(false);
        }
    }
}