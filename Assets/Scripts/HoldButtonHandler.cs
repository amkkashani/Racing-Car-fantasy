using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Assign your UI Image here")] public Image targetImage;

    [Header("Colors")] public Color pressedColor = Color.green;
    private Color normalColor;
    
    [Tooltip("How much bigger the image gets when pressed")]
    public float scaleFactor = 1.5f;

    private Vector3 _originalScale;
    private RectTransform _rectTransform;
    
    

    [SerializeField] public bool isPressed =false;
    

    void Start()
    {
        
        // Auto-assign if script is on same GameObject as the Image
        targetImage = GetComponent<Image>();
        normalColor = targetImage.color;
        _originalScale = targetImage.transform.localScale;
        _rectTransform = GetComponent<RectTransform>();
        
        if (targetImage == null)
            Debug.LogError("TouchClickButton: No Image assigned on " + name);
        else
            targetImage.color = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetImage.color = pressedColor;
        // _rectTransform.localScale = _originalScale * scaleFactor;
        
        _rectTransform.localScale = _originalScale * scaleFactor;
        
        // Debug.Log("Button is pressed");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetImage.color = normalColor;
        _rectTransform.localScale = _originalScale;
        // Debug.Log("Button is released");
    }
}