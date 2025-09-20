using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] private Image background;  // ボタンの背景
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = Color.gray;

    public void SetSelected(bool isSelected)
    {
        if (background != null)
        {
            background.color = isSelected ? selectedColor : normalColor;
        }
    }
}