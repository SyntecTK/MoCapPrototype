using System.Collections;
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

    private void Start()
    {
        LoadStage(0);
    }

    public void LoadStage(int stageIndex)
    {
        float movementValue = 1f;
        foreach(Transform child in elementsParent.transform)
        {
            if(child.name.StartsWith("R"))
            {                
                movementValue = -1f;
            }

            float newPosX = child.position.x + movementValue * movementDistance;
            StartCoroutine(MoveElement(child.gameObject, new Vector2(newPosX, child.position.y), transitionDuration));
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
}
