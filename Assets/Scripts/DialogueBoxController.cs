using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueBoxController : MonoBehaviour
{
    [Header("Textbox")]
    public GameObject textBox;

    [Header("Text Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Content")]
    public string characterName = "Character";

    [TextArea(3, 6)]
    public string dialogue = "Das ist dein geplanter Text. Er wird Buchstabe für Buchstabe angezeigt.";

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;

    private Coroutine typingCoroutine;

    private void Start()
    {
        if (textBox != null)
        {
            textBox.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }

    public void StartDialogue()
    {
        if (textBox != null)
        {
            textBox.SetActive(true);
        }

        if (nameText != null)
        {
            nameText.text = characterName;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeDialogue());
    }

    private IEnumerator TypeDialogue()
    {
        dialogueText.text = "";

        foreach (char letter in dialogue)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
    }
}