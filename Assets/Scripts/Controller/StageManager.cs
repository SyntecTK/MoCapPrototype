using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private GameObject elementsParent;

    [Header("Stage Settings")]
    [SerializeField] private float movementDistance = 8f;
    [SerializeField] private float transitionDuration = 2f;

    private List<GameObject> elementsL = new List<GameObject>();
    private List<GameObject> elementsR = new List<GameObject>();
    private List<GameObject> elementsT = new List<GameObject>();

    private bool loaded = false;

    private void Start()
    {
        foreach (Transform child in elementsParent.transform)
        {
            if (child.name.StartsWith("L"))
            {
                elementsL.Add(child.gameObject);
            }
            else if (child.name.StartsWith("R"))
            {
                elementsR.Add(child.gameObject);
            }
            else if (child.name.StartsWith("T"))
            {
                elementsT.Add(child.gameObject);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && !loaded)
        {
            LoadFirstStage(0);
            loaded = true;
        }

        if (Input.GetKeyDown(KeyCode.U) && loaded)
        {
            UnloadFirstStage(0);
            loaded = false;
        }
    }

    private void UnloadFirstStage(int v)
    {
        MoveElements(elementsL, -movementDistance, true);
        MoveElements(elementsR, movementDistance, true);
        MoveElements(elementsT, 5f, false);
    }

    public void LoadFirstStage(int stageIndex)
    {
        MoveElements(elementsL, movementDistance, true);
        MoveElements(elementsR, -movementDistance, true);
        MoveElements(elementsT, -5f, false);
    }

    private void MoveElements(List<GameObject> elements, float offsetX, bool horizontal)
    {
        foreach (GameObject element in elements)
        {
            float newPosX = element.transform.position.x + offsetX;
            float newPosY = element.transform.position.y + offsetX;
            if (horizontal)
                StartCoroutine(MoveElement(element, new Vector2(newPosX, element.transform.position.y), transitionDuration));
            else
                StartCoroutine(MoveElement(element, new Vector2(element.transform.position.x, newPosY), transitionDuration));
        }
    }

    private IEnumerator MoveElement(GameObject element, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = element.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            element.transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        element.transform.position = targetPosition;
    }

    private IEnumerator RemoveElement(GameObject element, float duration)
    {
        Vector3 startPosition = element.transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y - 10f, startPosition.z);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            element.transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(element);
    }
}
