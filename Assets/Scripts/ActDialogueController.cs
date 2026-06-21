using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActDialogueController : MonoBehaviour
{
    private enum DialogueState
    {
        IntroLines,
        ShowingChoices,
        BranchLines,
        EndingLines,
        Finished
    }

    public enum DialogueSection
    {
        IntroLines,
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

    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Character Appearance")]
    public GameObject characterObject;
    public Image characterImage;
    public Sprite characterSprite;

    [Tooltip("Bei welcher Intro-Zeile der Character erscheinen soll. 0 = erste Zeile, 1 = zweite Zeile usw.")]
    public int characterAppearsAtIntroLine = 3;

    public bool fadeCharacterIn = true;
    public float characterFadeDuration = 1f;
    [Header("Background Change")]
    public SpriteRenderer backgroundRenderer;
    public Sprite secondBackground;
    public int changeBackgroundAtIntroLine = 8;
    public Sprite thirdBackground;
    public int changeBackgroundAtIntroLine2 = -1;
    public int characterLeavesAtIntroLine = -1;
    [Header("Gathering Characters")]
    public GameObject gatheringCharactersObject;
    public int gatheringAppearsAtIntroLine = -1;

    [Header("Speaker Name Changes")]
    public string defaultSpeakerName = "YOU";
    public SpeakerChange[] speakerChanges;

    [Header("Choice UI")]
    public GameObject choicePanel;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceButtonLabels;

    [Header("Optional End Button")]
    public GameObject nextSceneButton;
    public string autoNextSceneName = "";

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string[] introLines;

    public DialogueChoice[] choices;

    [TextArea(2, 5)]
    public string[] endingLines;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private DialogueState state = DialogueState.IntroLines;
    private string[] currentLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool characterHasAppeared = false;
    private bool backgroundHasChanged = false;
    private bool backgroundHasChanged2 = false;
    private bool characterHasLeft = false;
    private bool gatheringHasAppeared = false;
    private Coroutine typingCoroutine;
    private Coroutine characterFadeCoroutine;
    private DialogueChoice currentChoice;

    [System.Serializable]
public class DialogueChoice
{
    public string buttonText;

    [TextArea(2, 5)]
    public string[] resultLines;

    public SpeakerChangeInChoice[] speakerChanges;
}

[System.Serializable]
public class SpeakerChangeInChoice
{
    public int lineIndex;
    public string speakerName;
}


    private void Start()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (nextSceneButton != null)
            nextSceneButton.SetActive(false);

        if (gatheringCharactersObject != null)
        gatheringCharactersObject.SetActive(false);

        if (textBox != null)
            textBox.SetActive(true);

        if (nameText != null)
            nameText.text = defaultSpeakerName;

        SetupCharacterAtStart();

        StartLines(introLines, DialogueState.IntroLines);
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
    }

    private void Update()
    {
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
        CheckCharacterAppearance();
        CheckBackgroundChange();
        CheckCharacterLeaves();
        CheckGatheringAppearance();
        ApplySpeakerNameForCurrentLine();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(currentLines[currentLineIndex]));
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

    if (state == DialogueState.BranchLines && currentChoice != null && currentChoice.speakerChanges != null)
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
        if (state == DialogueState.BranchLines)
            return DialogueSection.BranchLines;

        if (state == DialogueState.EndingLines)
            return DialogueSection.EndingLines;

        return DialogueSection.IntroLines;
    }

    private void CheckCharacterAppearance()
    {
        if (characterHasAppeared)
            return;

        if (state != DialogueState.IntroLines)
            return;

        if (currentLineIndex < characterAppearsAtIntroLine)
            return;

        ShowCharacter();
    }
    private void CheckBackgroundChange()
    {
        if (backgroundRenderer == null) return;
        if (state != DialogueState.IntroLines) return;

        if (!backgroundHasChanged && secondBackground != null
            && currentLineIndex >= changeBackgroundAtIntroLine)
        {
            backgroundRenderer.sprite = secondBackground;
            backgroundHasChanged = true;
        }

        if (!backgroundHasChanged2 && thirdBackground != null
            && changeBackgroundAtIntroLine2 >= 0
            && currentLineIndex >= changeBackgroundAtIntroLine2)
        {
            backgroundRenderer.sprite = thirdBackground;
            backgroundHasChanged2 = true;
        }
    }

        private void CheckCharacterLeaves()
    {
        if (characterHasLeft) return;
        if (state != DialogueState.IntroLines) return;
        if (characterLeavesAtIntroLine < 0) return;
        if (currentLineIndex < characterLeavesAtIntroLine) return;

        HideCharacter();
    }

    private void CheckGatheringAppearance()
    {
        if (gatheringHasAppeared) return;
        if (state != DialogueState.IntroLines) return;
        if (gatheringAppearsAtIntroLine < 0) return;
        if (currentLineIndex < gatheringAppearsAtIntroLine) return;

        if (gatheringCharactersObject != null)
        {
            gatheringCharactersObject.SetActive(true);
            gatheringHasAppeared = true;
        }
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
            if (choices != null && choices.Length > 0)
                ShowChoices();
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

    if (choicePanel != null)
        choicePanel.SetActive(false);

    // alle Antwort-Buttons ausblenden
    foreach (Button button in choiceButtons)
    {
        if (button != null)
            button.gameObject.SetActive(false);
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(autoNextSceneName);
        }
        else if (nextSceneButton != null)
        {
            nextSceneButton.SetActive(true);
        }
    }
}