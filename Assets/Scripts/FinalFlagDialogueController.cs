using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class FinalFlagDialogueController : MonoBehaviour
{
    private enum DialogueState
    {
        IntroDialogue,
        RouteDialogue,
        Finished
    }

    [System.Serializable]
    public class SpeakerChange
    {
        public int lineIndex;
        public string speakerName;
    }

    [System.Serializable]
    public class BackgroundChange
    {
        public int lineIndex;
        public Sprite backgroundSprite;
    }

    [System.Serializable]
    public class FinalRoute
    {
        [Header("Route Info")]
        public string routeName;

        [Header("Flag Conditions")]
        [Tooltip("Diese Flags MUSS der Spieler besitzen, damit diese Route gespielt wird.")]
        public string[] requiredFlags;

        [Tooltip("Diese Flags DARF der Spieler NICHT besitzen, damit diese Route gespielt wird.")]
        public string[] forbiddenFlags;

        [Header("Character")]
        public bool useCharacter = false;
        public GameObject characterObject;
        public Image characterImage;
        public Sprite characterSprite;

        [Tooltip("Bei welcher Route-Dialog-Zeile der Character erscheint. 0 = erste Zeile, -1 = nie.")]
        public int characterAppearsAtLine = 0;

        [Tooltip("Bei welcher Route-Dialog-Zeile der Character verschwindet. -1 = nie.")]
        public int characterLeavesAtLine = -1;

        public bool fadeCharacter = true;
        public float characterFadeDuration = 1f;

        [Header("Dialogue")]
        [TextArea(2, 5)]
        public string[] dialogueLines;

        public SpeakerChange[] speakerChanges;

        [Header("Background Changes")]
        public BackgroundChange[] backgroundChanges;
    }

    [Header("UI Elements")]
    public GameObject textBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Speaker")]
    public string defaultSpeakerName = "YOU";

    [Header("Intro Dialogue")]
    [TextArea(2, 5)]
    public string[] introDialogueLines;

    public SpeakerChange[] introSpeakerChanges;

    [Header("Intro Background Changes")]
    public SpriteRenderer backgroundRenderer;
    public BackgroundChange[] introBackgroundChanges;

    [Header("Flag Routes")]
    [Tooltip("Routes werden von oben nach unten geprüft. Die erste passende Route wird gespielt.")]
    public FinalRoute[] routes;

    [Tooltip("Diese Route wird gespielt, wenn keine Route passt. Optional.")]
    public FinalRoute fallbackRoute;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    [Header("End")]
    public GameObject nextSceneButton;
    public string autoNextSceneName = "";

    [Header("Debug")]
    public bool debugRoutes = true;

    private DialogueState state = DialogueState.IntroDialogue;

    private string[] currentLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;

    private Coroutine typingCoroutine;
    private Coroutine characterFadeCoroutine;

    private FinalRoute currentRoute;

    private bool[] introBackgroundTriggered;
    private bool[] routeBackgroundTriggered;

    private bool characterHasAppeared = false;
    private bool characterHasLeft = false;

    private void Start()
    {
        if (nextSceneButton != null)
            nextSceneButton.SetActive(false);

        if (textBox != null)
            textBox.SetActive(true);

        if (nameText != null)
            nameText.text = defaultSpeakerName;

        SetupBackgroundTriggers();
        SetupCharactersAtStart();

        StartLines(introDialogueLines, DialogueState.IntroDialogue);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (state == DialogueState.IntroDialogue ||
                state == DialogueState.RouteDialogue)
            {
                HandleEnter();
            }
        }
    }

    private void SetupBackgroundTriggers()
    {
        if (introBackgroundChanges != null)
            introBackgroundTriggered = new bool[introBackgroundChanges.Length];
        else
            introBackgroundTriggered = new bool[0];

        routeBackgroundTriggered = new bool[0];
    }

    private void SetupCharactersAtStart()
    {
        if (routes != null)
        {
            foreach (FinalRoute route in routes)
            {
                SetupRouteCharacterAtStart(route);
            }
        }

        SetupRouteCharacterAtStart(fallbackRoute);
    }

    private void SetupRouteCharacterAtStart(FinalRoute route)
    {
        if (route == null)
            return;

        if (!route.useCharacter)
            return;

        if (route.characterImage != null && route.characterSprite != null)
        {
            route.characterImage.sprite = route.characterSprite;
            route.characterImage.preserveAspect = true;
        }

        if (route.characterImage != null)
        {
            Color color = route.characterImage.color;
            color.a = 0f;
            route.characterImage.color = color;
        }

        if (route.characterObject != null)
            route.characterObject.SetActive(false);
    }

    private void StartLines(string[] lines, DialogueState newState)
    {
        state = newState;
        currentLines = lines;
        currentLineIndex = 0;
        isTyping = false;

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
        CheckBackgroundChange();
        CheckCharacterAppearance();
        CheckCharacterLeaves();
        ApplySpeakerName();

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
            if (dialogueText != null)
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

        if (dialogueText != null && currentLines != null && currentLineIndex < currentLines.Length)
            dialogueText.text = currentLines[currentLineIndex];

        isTyping = false;
        typingCoroutine = null;
    }

    private void ApplySpeakerName()
    {
        if (nameText == null)
            return;

        string speakerToUse = defaultSpeakerName;

        if (state == DialogueState.IntroDialogue && introSpeakerChanges != null)
        {
            foreach (SpeakerChange speakerChange in introSpeakerChanges)
            {
                if (speakerChange != null && speakerChange.lineIndex == currentLineIndex)
                    speakerToUse = speakerChange.speakerName;
            }
        }
        else if (state == DialogueState.RouteDialogue &&
                 currentRoute != null &&
                 currentRoute.speakerChanges != null)
        {
            foreach (SpeakerChange speakerChange in currentRoute.speakerChanges)
            {
                if (speakerChange != null && speakerChange.lineIndex == currentLineIndex)
                    speakerToUse = speakerChange.speakerName;
            }
        }

        nameText.text = speakerToUse;
    }

    private void CheckBackgroundChange()
    {
        if (backgroundRenderer == null)
            return;

        if (state == DialogueState.IntroDialogue)
        {
            ApplyBackgroundChanges(introBackgroundChanges, introBackgroundTriggered);
        }
        else if (state == DialogueState.RouteDialogue && currentRoute != null)
        {
            ApplyBackgroundChanges(currentRoute.backgroundChanges, routeBackgroundTriggered);
        }
    }

    private void ApplyBackgroundChanges(BackgroundChange[] changes, bool[] triggered)
    {
        if (changes == null || triggered == null)
            return;

        for (int i = 0; i < changes.Length; i++)
        {
            BackgroundChange change = changes[i];

            if (change == null)
                continue;

            if (i < triggered.Length && triggered[i])
                continue;

            if (change.backgroundSprite == null)
                continue;

            if (currentLineIndex < change.lineIndex)
                continue;

            backgroundRenderer.sprite = change.backgroundSprite;

            if (i < triggered.Length)
                triggered[i] = true;
        }
    }

    private void CheckCharacterAppearance()
    {
        if (state != DialogueState.RouteDialogue)
            return;

        if (currentRoute == null)
            return;

        if (!currentRoute.useCharacter)
            return;

        if (characterHasAppeared)
            return;

        if (currentRoute.characterAppearsAtLine < 0)
            return;

        if (currentLineIndex < currentRoute.characterAppearsAtLine)
            return;

        ShowRouteCharacter(currentRoute);
    }

    private void ShowRouteCharacter(FinalRoute route)
    {
        characterHasAppeared = true;

        if (route.characterObject != null)
            route.characterObject.SetActive(true);

        if (route.characterImage != null && route.characterSprite != null)
        {
            route.characterImage.sprite = route.characterSprite;
            route.characterImage.preserveAspect = true;
        }

        if (route.characterImage != null)
        {
            if (characterFadeCoroutine != null)
                StopCoroutine(characterFadeCoroutine);

            if (route.fadeCharacter)
            {
                characterFadeCoroutine = StartCoroutine(FadeCharacter(route, 0f, 1f, false));
            }
            else
            {
                Color color = route.characterImage.color;
                color.a = 1f;
                route.characterImage.color = color;
            }
        }
    }

    private void CheckCharacterLeaves()
    {
        if (state != DialogueState.RouteDialogue)
            return;

        if (currentRoute == null)
            return;

        if (!currentRoute.useCharacter)
            return;

        if (characterHasLeft)
            return;

        if (currentRoute.characterLeavesAtLine < 0)
            return;

        if (currentLineIndex < currentRoute.characterLeavesAtLine)
            return;

        HideRouteCharacter(currentRoute);
    }

    private void HideRouteCharacter(FinalRoute route)
    {
        characterHasLeft = true;

        if (route.characterImage != null)
        {
            if (characterFadeCoroutine != null)
                StopCoroutine(characterFadeCoroutine);

            if (route.fadeCharacter)
            {
                characterFadeCoroutine = StartCoroutine(FadeCharacter(route, 1f, 0f, true));
            }
            else
            {
                Color color = route.characterImage.color;
                color.a = 0f;
                route.characterImage.color = color;

                if (route.characterObject != null)
                    route.characterObject.SetActive(false);
            }
        }
        else
        {
            if (route.characterObject != null)
                route.characterObject.SetActive(false);
        }
    }

    private IEnumerator FadeCharacter(FinalRoute route, float from, float to, bool deactivateAtEnd)
    {
        float timer = 0f;
        float duration = Mathf.Max(0.01f, route.characterFadeDuration);

        if (route.characterImage != null)
        {
            Color startColor = route.characterImage.color;
            startColor.a = from;
            route.characterImage.color = startColor;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, timer / duration);

            if (route.characterImage != null)
            {
                Color color = route.characterImage.color;
                color.a = alpha;
                route.characterImage.color = color;
            }

            yield return null;
        }

        if (route.characterImage != null)
        {
            Color finalColor = route.characterImage.color;
            finalColor.a = to;
            route.characterImage.color = finalColor;
        }

        if (deactivateAtEnd && route.characterObject != null)
            route.characterObject.SetActive(false);

        characterFadeCoroutine = null;
    }

    private void FinishCurrentLines()
    {
        if (state == DialogueState.IntroDialogue)
        {
            SelectAndStartRoute();
        }
        else if (state == DialogueState.RouteDialogue)
        {
            Finish();
        }
    }

    private void SelectAndStartRoute()
    {
        currentRoute = GetMatchingRoute();

        if (currentRoute == null)
        {
            Debug.LogWarning("No matching final route found and no fallback route assigned.");
            Finish();
            return;
        }

        if (debugRoutes)
            Debug.Log("Selected Final Route: " + currentRoute.routeName);

        characterHasAppeared = false;
        characterHasLeft = false;

        if (currentRoute.backgroundChanges != null)
            routeBackgroundTriggered = new bool[currentRoute.backgroundChanges.Length];
        else
            routeBackgroundTriggered = new bool[0];

        StartLines(currentRoute.dialogueLines, DialogueState.RouteDialogue);
    }

    private FinalRoute GetMatchingRoute()
    {
        if (routes != null)
        {
            for (int i = 0; i < routes.Length; i++)
            {
                FinalRoute route = routes[i];

                if (route == null)
                    continue;

                bool requiredOkay = HasAllFlags(route.requiredFlags);
                bool forbiddenFound = HasAnyFlag(route.forbiddenFlags);

                if (debugRoutes)
                {
                    Debug.Log("Checking Final Route: " + route.routeName);
                    Debug.Log("Required OK: " + requiredOkay);
                    Debug.Log("Forbidden Found: " + forbiddenFound);
                }

                if (requiredOkay && !forbiddenFound)
                    return route;
            }
        }

        return fallbackRoute;
    }

    private bool HasAllFlags(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return true;

        foreach (string flag in flags)
        {
            string normalizedFlag = NormalizeFlag(flag);

            if (string.IsNullOrEmpty(normalizedFlag))
                continue;

            if (!HasFlag(normalizedFlag))
                return false;
        }

        return true;
    }

    private bool HasAnyFlag(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return false;

        foreach (string flag in flags)
        {
            string normalizedFlag = NormalizeFlag(flag);

            if (string.IsNullOrEmpty(normalizedFlag))
                continue;

            if (HasFlag(normalizedFlag))
                return true;
        }

        return false;
    }

    private bool HasFlag(string flag)
    {
        if (GameState.Instance == null)
            return false;

        string normalizedFlag = NormalizeFlag(flag);

        if (string.IsNullOrEmpty(normalizedFlag))
            return false;

        if (NormalizeFlag(GameState.Instance.routeFlag) == normalizedFlag)
            return true;

        if (GameState.Instance.routeFlags != null)
        {
            foreach (string savedFlag in GameState.Instance.routeFlags)
            {
                if (NormalizeFlag(savedFlag) == normalizedFlag)
                    return true;
            }
        }

        return false;
    }

    private string NormalizeFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag))
            return "";

        return flag.Trim();
    }

    private void Finish()
    {
        state = DialogueState.Finished;

        if (textBox != null)
            textBox.SetActive(false);

        HideAllRouteCharacters();

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
            Debug.LogWarning("Final dialogue finished, but no nextSceneButton or autoNextSceneName is assigned.");
        }
    }

    private void HideAllRouteCharacters()
    {
        if (routes != null)
        {
            foreach (FinalRoute route in routes)
            {
                HideRouteCharacterInstant(route);
            }
        }

        HideRouteCharacterInstant(fallbackRoute);
    }

    private void HideRouteCharacterInstant(FinalRoute route)
    {
        if (route == null)
            return;

        if (!route.useCharacter)
            return;

        if (route.characterImage != null)
        {
            Color color = route.characterImage.color;
            color.a = 0f;
            route.characterImage.color = color;
        }

        if (route.characterObject != null)
            route.characterObject.SetActive(false);
    }
}