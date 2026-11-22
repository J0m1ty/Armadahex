using UnityEngine;
using TMPro;

// a numbered choice (a weapon or an angle): the key for its number, a label and a small note
[RequireComponent(typeof(UIButton))]
public class ChoiceKey : MonoBehaviour
{
    [SerializeField] private KeyHint number;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text note;

    public UIButton button => GetComponent<UIButton>();

    public void Set(int slot, string label, string detail, bool ready) {
        number.Set("Gameplay/Choice " + slot);
        title.text = ready ? label : "<s>" + label + "</s>";
        note.text = detail;
        note.gameObject.SetActive(!string.IsNullOrEmpty(detail));
        button.interactable = ready;
    }

    public void SetChosen(bool chosen) {
        button.SetLit(chosen);
    }
}
