using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseParallax : MonoBehaviour
{
    [Tooltip("Start from the furthest object to the nearest.")]
    [SerializeField] private GameObject[] parallaxObjects;
    [SerializeField] private float MouseSpeedX = 1f, MouseSpeedY = .2f;
    private Vector3[] originalPos;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        originalPos = new Vector3[parallaxObjects.Length];
        for (int i = 0; i < parallaxObjects.Length; i++)
        {
            originalPos[i] = parallaxObjects[i].transform.position;
        }
    }

    private void FixedUpdate()
    {
        float x, y;
        x = (Input.mousePosition.x - (Screen.width / 2)) * MouseSpeedX / Screen.width;
        y = (Input.mousePosition.y - (Screen.height / 2)) * MouseSpeedY / Screen.height;

        for (int i = 1; i < parallaxObjects.Length + 1; ++i)
        {
            parallaxObjects[i - 1].transform.position = originalPos[i - 1] + (new Vector3(x, y, 0f) * 1 * ((i - 1) - (parallaxObjects.Length / 2)));
        }
    }
}
