using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class FruitDropper : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject[] fruits; // Array to hold the fruit prefabs
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Speed at which the fruit dropper moves
    public float smoothing = 0.1f; // Smoothing for movement
    
    [Header("Boundary Settings")]
    public float boundaryPadding = 0.5f; // Extra padding from screen edges
    
    private InputSystem_Actions inputActions;
    private Vector2 currentMoveInput;
    private Vector2 smoothedMoveInput;
    private bool isTouching = false;
    
    // Screen boundary variables
    private Vector2 screenBounds;
    private float objectWidth;
    private float objectHeight;
    
    public Dictionary<string, string> nextFruitMapping;
    
    void Awake()
    {
        // Ensure the fruits array is populated with the fruit prefabs
        if (fruits == null || fruits.Length == 0)
        {
            Debug.LogError("Fruits array is not set or empty. Please assign fruit prefabs in the inspector.");
        }
        
        // Initialize fruit mapping
        InitializeFruitMapping();
        
        // Initialize input actions
        inputActions = new InputSystem_Actions();
        
        // Calculate screen boundaries
        CalculateScreenBounds();
    }
    
    void OnEnable()
    {
        inputActions.Enable();
        
        // Subscribe to touch movement (delta tracking)
        inputActions.Player.Move.performed += OnTouchMove;
        inputActions.Player.Move.canceled += OnTouchMoveStop;
        
        // Subscribe to touch press events (start/end detection)
        inputActions.Player.TouchPress.started += OnTouchStart;
        inputActions.Player.TouchPress.canceled += OnTouchEnd;
    }
    
    void OnDisable()
    {
        // Unsubscribe from all events
        inputActions.Player.Move.performed -= OnTouchMove;
        inputActions.Player.Move.canceled -= OnTouchMoveStop;
        inputActions.Player.TouchPress.started -= OnTouchStart;
        inputActions.Player.TouchPress.canceled -= OnTouchEnd;
        
        inputActions.Disable();
    }
    
    void OnDestroy()
    {
        inputActions?.Dispose();
    }
    
    private void CalculateScreenBounds()
    {
        // Get screen boundaries in world space
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            return;
        }
        
        // Convert screen dimensions to world coordinates
        Vector3 screenPoint = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        screenBounds = new Vector2(Mathf.Abs(screenPoint.x), Mathf.Abs(screenPoint.y));
        
        // Get object dimensions (assuming the fruit dropper has a collider or renderer)
        Renderer objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            objectWidth = objectRenderer.bounds.size.x / 2f;
            objectHeight = objectRenderer.bounds.size.y / 2f;
        }
        else
        {
            // Default values if no renderer found
            objectWidth = 0.5f;
            objectHeight = 0.5f;
        }
    }
    
    private void InitializeFruitMapping()
    {
        nextFruitMapping = new Dictionary<string, string>();
        for(int i = 0; i < fruits.Length; i++)
        {
            // Assuming each fruit prefab has a unique tag that corresponds to its type
            string fruitTag = fruits[i].name;
            if (!nextFruitMapping.ContainsKey(fruitTag))
            {
                // Map the fruit tag to the next fruit prefab's tag
                nextFruitMapping[fruitTag] = fruits[(i + 1) % fruits.Length].name; // Wrap around to the first fruit
            }
        }
    }
    
    // Called when touch delta changes (sliding)
    private void OnTouchMove(InputAction.CallbackContext context)
    {
        if (isTouching)
        {
            Vector2 deltaInput = context.ReadValue<Vector2>();
            currentMoveInput = deltaInput;
            Debug.Log("Touch moving - Delta: " + deltaInput);
        }
    }
    
    // Called when touch delta stops changing
    private void OnTouchMoveStop(InputAction.CallbackContext context)
    {
        currentMoveInput = Vector2.zero;
        Debug.Log("Touch movement stopped");
    }
    
    // Called when touch starts
    private void OnTouchStart(InputAction.CallbackContext context)
    {
        isTouching = true;
        Debug.Log("Touch started");
    }
    
    // Called when touch ends - This is where you drop the fruit
    private void OnTouchEnd(InputAction.CallbackContext context)
    {
        isTouching = false;
        currentMoveInput = Vector2.zero;
        
        Debug.Log("Touch ended - Dropping fruit!");
        
        // Drop a random fruit when touch ends
        DropFruit();
    }
    
    private void DropFruit()
    {
        int randomIndex = Random.Range(0, fruits.Length);
        GameObject selectedFruit = fruits[randomIndex];
        Instantiate(selectedFruit, transform.position, Quaternion.identity);
    }
    
    void Update()
    {
        // Smooth the movement input for better feel
        smoothedMoveInput = Vector2.Lerp(smoothedMoveInput, currentMoveInput, smoothing);
        
        // Apply movement to the fruit dropper with boundary constraints
        MoveDropper(smoothedMoveInput);
    }
    
    private void MoveDropper(Vector2 moveInput)
    {
        if (moveInput.magnitude < 0.01f) return;
        
        // Convert touch delta to world movement
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed * Time.deltaTime;
        
        // Calculate new position
        Vector3 newPosition = transform.position + movement;
        
        // Apply boundary constraints
        newPosition = ClampToScreenBounds(newPosition);
        
        // Apply the clamped position
        transform.position = newPosition;
    }
    
    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        // Calculate the effective boundaries accounting for object size and padding
        float minX = -screenBounds.x + objectWidth + boundaryPadding;
        float maxX = screenBounds.x - objectWidth - boundaryPadding;
        float minY = -screenBounds.y + objectHeight + boundaryPadding;
        float maxY = screenBounds.y - objectHeight - boundaryPadding;
        
        // Clamp the position within bounds
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        
        return position;
    }
    
    // Alternative method: Check if trying to move out of bounds and prevent movement
    private bool IsWithinBounds(Vector3 targetPosition)
    {
        float minX = -screenBounds.x + objectWidth + boundaryPadding;
        float maxX = screenBounds.x - objectWidth - boundaryPadding;
        float minY = -screenBounds.y + objectHeight + boundaryPadding;
        float maxY = screenBounds.y - objectHeight - boundaryPadding;
        
        return targetPosition.x >= minX && targetPosition.x <= maxX && 
               targetPosition.y >= minY && targetPosition.y <= maxY;
    }
    
    // Call this if screen size changes or orientation changes
    public void RecalculateScreenBounds()
    {
        CalculateScreenBounds();
    }
    
    public GameObject GetNextFruit(string tag)
    {
        if (nextFruitMapping.ContainsKey(tag))
        {
            string nextFruitTag = nextFruitMapping[tag];
            GameObject nextFruitPrefab = fruits.FirstOrDefault(fruit => fruit.name == nextFruitTag);
            if (nextFruitPrefab != null)
            {
                return nextFruitPrefab;
            }
            else
            {
                Debug.LogError($"Next fruit prefab with tag '{nextFruitTag}' not found in Resources.");
                return null;
            }
        }
        else
        {
            Debug.LogError($"No mapping found for fruit tag '{tag}'.");
            return null;
        }
    }
}
