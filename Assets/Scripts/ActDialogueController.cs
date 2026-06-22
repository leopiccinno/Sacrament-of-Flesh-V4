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
        CardResultLines,
        Finished
    }

    private enum CardChoiceContext
    {
        None,
        Ending,
        GatheringCharacter
    }

    public enum DialogueSection
    {
        IntroDialogue,
        AfterNoteDialogue,
        CharacterIntroLines,
        BranchLines,
        EndingLines,
        CardResultLines
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
    public class FadingCharacter
    {
        public string characterName;
        public GameObject characterObject;
        public Image characterImage;
        public Sprite characterSprite;

        [Tooltip("Bei welcher Intro-Zeile erscheinen. -1 = nie.")]
        public int appearsAtIntroLine = -1;

        [Tooltip("Bei welcher Intro-Zeile verschwinden. -1 = nie.")]
        public int leavesAtIntroLine = -1;

        [HideInInspector] public bool hasAppeared;
        [HideInInspector] public bool hasLeft;
        [HideInInspector] public Coroutine fadeCoroutine;
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
    public class FlagBasedCardDialogueRule
    {
        [Header("Rule Name")]
        public string ruleName;

        [Header("Flag Conditions")]
        [Tooltip("Diese Flags MUSS der Spieler besitzen, damit diese Regel gilt.")]
        public string[] requiredFlags;

        [Tooltip("Diese Flags DARF der Spieler NICHT besitzen, damit diese Regel gilt.")]
        public string[] forbiddenFlags;

        [Header("Dialogue")]
        [TextArea(2, 5)]
        public string[] dialogueLines;

        public SpeakerChangeInChoice[] speakerChanges;

        [Header("Optional Route Flag")]
        [Tooltip("Wenn false, wird durch diese Regel keine neue Route Flag gesetzt.")]
        public bool setRouteFlag = false;

        public string routeFlagToSet = "";

        [Tooltip("Nur für Debug/Status im GameState.")]
        public bool countsAsSpecialRoute = false;
    }

    [System.Serializable]
    public class CharacterCardChoiceSettings
    {
        [Header("Enable")]
        public bool useCardChoiceAfterIntro = false;

        [Header("Flag Rules Before Card Check")]
        [Tooltip("Diese Regeln werden zuerst geprüft. Wenn eine Regel passt, ist die gespielte Karte egal.")]
        public FlagBasedCardDialogueRule[] flagRules;

        [Header("Special Card")]
        public CardData specialCard;

        [Header("Flags If No Flag Rule Matches")]
        [Tooltip("Flag, die gesetzt wird, wenn die Special Card gespielt wird.")]
        public string specialRouteFlag = "SPECIAL_CHARACTER_CARD_ROUTE";

        [Tooltip("Flag, die gesetzt wird, wenn eine andere Karte gespielt wird.")]
        public string normalRouteFlag = "NORMAL_CHARACTER_CARD_ROUTE";

        [Header("Dialogue If Special Card Is Played")]
        [TextArea(2, 5)]
        public string[] specialCardDialogueLines;

        public SpeakerChangeInChoice[] specialCardSpeakerChanges;

        [Header("Dialogue If Any Other Card Is Played")]
        [TextArea(2, 5)]
        public string[] normalCardDialogueLines;

        public SpeakerChangeInChoice[] normalCardSpeakerChanges;
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

        [Header("Optional Card Choice After Intro")]
        public CharacterCardChoiceSettings cardChoiceSettings;

        [Header("Dialogue Choices")]
        public DialogueChoice[] choices;

        [Header("Runtime")]
        public bool hasTalkedToCharacter = false;
    }

    [System.Serializable]
    public class EndingCardChoiceSettings
    {
        [Header("Special Card")]
        public CardData specialCard;

        [Header("Route Flags")]
        public string specialRouteFlag = "SPECIAL_CARD_ROUTE";
        public string normalRouteFlag = "NORMAL_CARD_ROUTE";

        [Header("Dialogue If Special Card Is Played")]
        [TextArea(2, 5)]
        public string[] specialCardDialogueLines;

        public SpeakerChangeInChoice[] specialCardSpeakerChanges;

        [Header("Dialogue If Any Other Card Is Played")]
        [TextArea(2, 5)]
        public string[] normalCardDialogueLines;

        public SpeakerChangeInChoice[] normalCardSpeakerChanges;
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

    [Tooltip("Wenn true, kann der Spieler nur EINEN Gathering Character auswählen. Danach starten direkt die Ending Lines.")]
    public bool endGatheringAfterFirstCharacter = false;

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

    [Header("Ending Card Choice")]
    public bool useCardChoiceAfterEnding = false;
    public int maxDisplayedCardChoices = 2;
    public EndingCardChoiceSettings endingCardChoiceSettings;

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

    [Header("Multiple Fading Characters")]
    public FadingCharacter[] fadingCharacters;

    [Header("Background Change")]
    public SpriteRenderer backgroundRenderer;
    public BackgroundChange[] backgroundChanges;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private DialogueState state = DialogueState.IntroDialogue;
    private CardChoiceContext currentCardChoiceContext = CardChoiceContext.None;

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
    private SpeakerChangeInChoice[] currentCardResultSpeakerChanges;

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
        SetupFadingCharactersAtStart();
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
                state == DialogueState.EndingLines ||
                state == DialogueState.CardResultLines)
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

    private void SetupFadingCharactersAtStart()
    {
        if (fadingCharacters == null)
            return;

        foreach (FadingCharacter fc in fadingCharacters)
        {
            if (fc == null)
                continue;

            fc.hasAppeared = false;
            fc.hasLeft = false;

            if (fc.characterImage != null && fc.characterSprite != null)
            {
                fc.characterImage.sprite = fc.characterSprite;
                fc.characterImage.preserveAspect = true;
            }

            if (fc.characterObject != null)
                fc.characterObject.SetActive(false);

            if (fc.characterImage != null)
            {
                Color color = fc.characterImage.color;
                color.a = 0f;
                fc.characterImage.color = color;
            }
        }
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
        CheckFadingCharacters();
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

    private void CheckFadingCharacters()
    {
        if (fadingCharacters == null)
            return;

        if (state != DialogueState.IntroDialogue)
            return;

        foreach (FadingCharacter fc in fadingCharacters)
        {
            if (fc == null)
                continue;

            if (!fc.hasAppeared &&
                fc.appearsAtIntroLine >= 0 &&
                currentLineIndex >= fc.appearsAtIntroLine)
            {
                fc.hasAppeared = true;
                ShowFadingCharacter(fc);
            }

            if (!fc.hasLeft &&
                fc.leavesAtIntroLine >= 0 &&
                currentLineIndex >= fc.leavesAtIntroLine)
            {
                fc.hasLeft = true;
                HideFadingCharacter(fc);
            }
        }
    }

    private void ShowFadingCharacter(FadingCharacter fc)
    {
        if (fc.characterObject != null)
            fc.characterObject.SetActive(true);

        if (fc.characterImage != null && fc.characterSprite != null)
        {
            fc.characterImage.sprite = fc.characterSprite;
            fc.characterImage.preserveAspect = true;
        }

        if (fc.characterImage != null)
        {
            if (fc.fadeCoroutine != null)
                StopCoroutine(fc.fadeCoroutine);

            if (fadeCharacterIn)
                fc.fadeCoroutine = StartCoroutine(FadeFadingCharacter(fc, 0f, 1f, false));
            else
            {
                Color color = fc.characterImage.color;
                color.a = 1f;
                fc.characterImage.color = color;
            }
        }
    }

    private void HideFadingCharacter(FadingCharacter fc)
    {
        if (fc.characterImage != null && fadeCharacterIn)
        {
            if (fc.fadeCoroutine != null)
                StopCoroutine(fc.fadeCoroutine);

            fc.fadeCoroutine = StartCoroutine(FadeFadingCharacter(fc, 1f, 0f, true));
        }
        else
        {
            if (fc.characterImage != null)
            {
                Color color = fc.characterImage.color;
                color.a = 0f;
                fc.characterImage.color = color;
            }

            if (fc.characterObject != null)
                fc.characterObject.SetActive(false);
        }
    }

    private IEnumerator FadeFadingCharacter(FadingCharacter fc, float from, float to, bool deactivateAtEnd)
    {
        float timer = 0f;

        if (fc.characterImage != null)
        {
            Color startColor = fc.characterImage.color;
            startColor.a = from;
            fc.characterImage.color = startColor;
        }

        while (timer < characterFadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, timer / characterFadeDuration);

            if (fc.characterImage != null)
            {
                Color color = fc.characterImage.color;
                color.a = alpha;
                fc.characterImage.color = color;
            }

            yield return null;
        }

        if (fc.characterImage != null)
        {
            Color finalColor = fc.characterImage.color;
            finalColor.a = to;
            fc.characterImage.color = finalColor;
        }

        if (deactivateAtEnd && fc.characterObject != null)
            fc.characterObject.SetActive(false);

        fc.fadeCoroutine = null;
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

        if (state == DialogueState.CardResultLines &&
            currentCardResultSpeakerChanges != null)
        {
            foreach (SpeakerChangeInChoice speakerChange in currentCardResultSpeakerChanges)
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

        if (state == DialogueState.CardResultLines)
            return DialogueSection.CardResultLines;

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
            if (currentCharacter != null &&
                currentCharacter.cardChoiceSettings != null &&
                currentCharacter.cardChoiceSettings.useCardChoiceAfterIntro)
            {
                ShowGatheringCharacterCardChoices();
            }
            else if (currentCharacter != null)
            {
                ShowChoicesForCharacter(currentCharacter);
            }
            else
            {
                ShowGatheringCharacters();
            }
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
            if (useCardChoiceAfterEnding)
                ShowEndingCardChoices();
            else
                Finish();
        }
        else if (state == DialogueState.CardResultLines)
        {
            FinishCardResultLines();
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

    private void ShowEndingCardChoices()
    {
        currentCardChoiceContext = CardChoiceContext.Ending;
        ShowCardChoicesForCollectedCards(OnEndingCardSelected);
    }

    private void ShowGatheringCharacterCardChoices()
    {
        currentCardChoiceContext = CardChoiceContext.GatheringCharacter;
        ShowCardChoicesForCollectedCards(OnGatheringCharacterCardSelected);
    }

    private delegate void CardChoiceCallback(int cardIndex);

    private void ShowCardChoicesForCollectedCards(CardChoiceCallback callback)
    {
        state = DialogueState.ShowingChoices;

        if (textBox != null)
            textBox.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        if (GameState.Instance == null)
        {
            Debug.LogWarning("GameState.Instance is null. Cannot show card choices.");
            Finish();
            return;
        }

        if (GameState.Instance.collectedCards == null || GameState.Instance.collectedCards.Count == 0)
        {
            Debug.LogWarning("Player has no collected cards.");
            Finish();
            return;
        }

        if (choiceButtons == null || choiceButtons.Length == 0)
        {
            Debug.LogWarning("No choice buttons assigned.");
            Finish();
            return;
        }

        int availableCardCount = GameState.Instance.collectedCards.Count;
        int displayLimit = Mathf.Min(maxDisplayedCardChoices, availableCardCount, choiceButtons.Length);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
                continue;

            if (i < displayLimit)
            {
                CardData card = GameState.Instance.collectedCards[i];

                choiceButtons[i].gameObject.SetActive(true);

                if (choiceButtonLabels != null &&
                    i < choiceButtonLabels.Length &&
                    choiceButtonLabels[i] != null)
                {
                    choiceButtonLabels[i].text = card.cardName;
                }

                int cardIndex = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => callback(cardIndex));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnEndingCardSelected(int cardIndex)
    {
        if (GameState.Instance == null)
            return;

        if (GameState.Instance.collectedCards == null)
            return;

        if (cardIndex < 0 || cardIndex >= GameState.Instance.collectedCards.Count)
            return;

        HideChoiceButtons();

        CardData selectedCard = GameState.Instance.collectedCards[cardIndex];

        bool isSpecialCard =
            endingCardChoiceSettings != null &&
            endingCardChoiceSettings.specialCard != null &&
            selectedCard == endingCardChoiceSettings.specialCard;

        if (isSpecialCard)
        {
            GameState.Instance.SetPlayedCardRoute(
                selectedCard,
                endingCardChoiceSettings.specialRouteFlag,
                true
            );

            currentCardResultSpeakerChanges = endingCardChoiceSettings.specialCardSpeakerChanges;

            StartLines(
                endingCardChoiceSettings.specialCardDialogueLines,
                DialogueState.CardResultLines
            );
        }
        else
        {
            string normalFlag = "NORMAL_CARD_ROUTE";
            string[] normalLines = null;

            if (endingCardChoiceSettings != null)
            {
                normalFlag = endingCardChoiceSettings.normalRouteFlag;
                normalLines = endingCardChoiceSettings.normalCardDialogueLines;
                currentCardResultSpeakerChanges = endingCardChoiceSettings.normalCardSpeakerChanges;
            }
            else
            {
                currentCardResultSpeakerChanges = null;
            }

            GameState.Instance.SetPlayedCardRoute(
                selectedCard,
                normalFlag,
                false
            );

            StartLines(
                normalLines,
                DialogueState.CardResultLines
            );
        }
    }

    public void OnGatheringCharacterCardSelected(int cardIndex)
    {
        if (currentCharacter == null)
            return;

        if (currentCharacter.cardChoiceSettings == null)
            return;

        if (GameState.Instance == null)
            return;

        if (GameState.Instance.collectedCards == null)
            return;

        if (cardIndex < 0 || cardIndex >= GameState.Instance.collectedCards.Count)
            return;

        HideChoiceButtons();

        CardData selectedCard = GameState.Instance.collectedCards[cardIndex];
        CharacterCardChoiceSettings settings = currentCharacter.cardChoiceSettings;

        FlagBasedCardDialogueRule matchingFlagRule = GetMatchingFlagRule(settings.flagRules);

        if (matchingFlagRule != null)
        {
            if (matchingFlagRule.setRouteFlag)
            {
                GameState.Instance.SetPlayedCardRoute(
                    selectedCard,
                    matchingFlagRule.routeFlagToSet,
                    matchingFlagRule.countsAsSpecialRoute
                );
            }

            currentCardResultSpeakerChanges = matchingFlagRule.speakerChanges;

            StartLines(
                matchingFlagRule.dialogueLines,
                DialogueState.CardResultLines
            );

            return;
        }

        bool isSpecialCard =
            settings.specialCard != null &&
            selectedCard == settings.specialCard;

        if (isSpecialCard)
        {
            GameState.Instance.SetPlayedCardRoute(
                selectedCard,
                settings.specialRouteFlag,
                true
            );

            currentCardResultSpeakerChanges = settings.specialCardSpeakerChanges;

            StartLines(
                settings.specialCardDialogueLines,
                DialogueState.CardResultLines
            );
        }
        else
        {
            GameState.Instance.SetPlayedCardRoute(
                selectedCard,
                settings.normalRouteFlag,
                false
            );

            currentCardResultSpeakerChanges = settings.normalCardSpeakerChanges;

            StartLines(
                settings.normalCardDialogueLines,
                DialogueState.CardResultLines
            );
        }
    }

    private FlagBasedCardDialogueRule GetMatchingFlagRule(FlagBasedCardDialogueRule[] rules)
    {
        if (rules == null || rules.Length == 0)
            return null;

        if (GameState.Instance == null)
            return null;

        foreach (FlagBasedCardDialogueRule rule in rules)
        {
            if (rule == null)
                continue;

            bool hasRequiredFlags = GameState.Instance.HasAllRouteFlags(rule.requiredFlags);
            bool hasForbiddenFlags = GameState.Instance.HasAnyRouteFlag(rule.forbiddenFlags);

            if (hasRequiredFlags && !hasForbiddenFlags)
            {
                return rule;
            }
        }

        return null;
    }

    private void FinishCardResultLines()
    {
        currentCardResultSpeakerChanges = null;

        if (currentCardChoiceContext == CardChoiceContext.GatheringCharacter)
        {
            currentCardChoiceContext = CardChoiceContext.None;

            if (currentCharacter != null)
            {
                if (currentCharacter.choices != null && currentCharacter.choices.Length > 0)
                {
                    ShowChoicesForCharacter(currentCharacter);
                }
                else
                {
                    FinishCurrentCharacterConversation();
                }
            }
            else
            {
                ShowGatheringCharacters();
            }

            return;
        }

        if (currentCardChoiceContext == CardChoiceContext.Ending)
        {
            currentCardChoiceContext = CardChoiceContext.None;
            Finish();
            return;
        }

        currentCardChoiceContext = CardChoiceContext.None;
        Finish();
    }

    private void ShowGatheringCharacters()
    {
        state = DialogueState.SelectingCharacter;

        if (textBox != null)
            textBox.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (gatheringCharacters == null || gatheringCharacters.Length == 0)
        {
            StartEndingDialogue();
            return;
        }

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
        else if (selectedCharacter.cardChoiceSettings != null &&
                 selectedCharacter.cardChoiceSettings.useCardChoiceAfterIntro)
        {
            ShowGatheringCharacterCardChoices();
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

                if (character.characterButton != null)
                    character.characterButton.interactable = false;
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

        if (endGatheringAfterFirstCharacter)
        {
            HideAllGatheringCharacters();
            StartEndingDialogue();
            return;
        }

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
        {
            if (useCardChoiceAfterEnding)
                ShowEndingCardChoices();
            else
                Finish();
        }
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

            if (character != null && character.characterButton != null)
                character.characterButton.interactable = false;
        }
    }
}