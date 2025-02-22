using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;


public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float _defaultSize;
    [SerializeField] float _scaledSize;
    [SerializeField] float _transitionSpeed;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOScale(endValue: new Vector3(x: _scaledSize, y: _scaledSize, z: _scaledSize), _transitionSpeed);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOScale(endValue: new Vector3(x: _defaultSize, y: _defaultSize, z: _defaultSize), _transitionSpeed);
    }
}
