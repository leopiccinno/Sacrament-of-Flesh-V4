using UnityEngine;

public class TextBoxController : MonoBehaviour
{
    [Header("Textbox")]
    public GameObject textBox;

    private void Start()
    {
        if (textBox != null)
        {
            textBox.SetActive(false);
        }
    }

    public void ShowTextBox()
    {
        if (textBox != null)
        {
            textBox.SetActive(true);
        }
    }

    public void HideTextBox()
    {
        if (textBox != null)
        {
            textBox.SetActive(false);
        }
    }
}