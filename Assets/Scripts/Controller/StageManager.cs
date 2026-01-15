using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private GameObject elementsParent;

    [Header("Stage Settings")]
    [SerializeField] private float movementDistance = 8f;
    [SerializeField] private float transitionDuration = 2f;

    private List<GameObject> elementsL = new List<GameObject>();
    private List<GameObject> elementsR = new List<GameObject>();

    private bool loaded = false;

    private void Start()
    {
        foreach(Transform child in elementsParent.transform)
        {
            if(child.name.StartsWith("L"))
            {
                elementsL.Add(child.gameObject);
            }
            else if(child.name.StartsWith("R"))
            {
                           }
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.L) && !loaded)
        {
            LoadStage(0);
            loaded = true;
        }

        if(Input.GetKeyDown(KeyCode.U) && loaded)
        {
            UnloadStage(0);
            loaded = false;
        }
    }

    private void UnloadStage(int v)
    {
        MoveElements(elementsL, -movementDistance);
        MoveElements(elementsR, movementDistance);
    }

    public void LoadStage(int stageIndex)
    {
        MoveElements(elementsL, movementDistance);
        MoveElements(elementsR, -movementDistance);
    }

    private void MoveElements(List<GameObject> elements, float offsetX)
    {
        foreach(GameObject element in elements)
        {
            float newPosX = element.transform.position.x + offsetX;
            StartCoroutine(MoveElement(element, new Vector2(newPosX, element.transform.position.y), transitionDuration));
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
