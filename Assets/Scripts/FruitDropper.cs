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
    public float boundaryPadding = 0.1f; // Extra padding from screen edges
    
    [Header("Preview Settings")]
    public Transform previewParent; // Parent object for the preview (optional)
    public Vector3 previewOffset = new Vector3(0, 1.5f, 0); // Offset from dropper position
    public float previewScale = 0.7f; // Scale of the preview fruit
    public Material previewMaterial; // Optional transparent material for preview
    
    private InputSystem_Actions inputActions;
    private Vector2 currentMoveInput;
    private Vector2 smoothedMoveInput;
    private bool isTouching = false;
    
    // Screen boundary variables
    private Vector2 screenBounds;
    private float objectWidth;
    private float objectHeight;
    
    // Preview system variables
    private GameObject currentPreview;
    private GameObject nextFruitToDropPrefab;
    
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
        
        // Select the first fruit to preview
        SelectNextFruit();
    }
    
    void Start()
    {
        // Show initial preview
        ShowPreview();
    }
    
    void OnEnable()
    {
        inputActions.Enable();
        
        GameManager.OnGamePaused += OnGamePaused;
        GameManager.OnGameResumed += OnGameResumed;
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
        
        GameManager.OnGamePaused -= OnGamePaused;
        GameManager.OnGameResumed -= OnGameResumed;
    
        inputActions.Player.Move.performed -= OnTouchMove;
        inputActions.Player.Move.canceled -= OnTouchMoveStop;
        inputActions.Player.TouchPress.started -= OnTouchStart;
        inputActions.Player.TouchPress.canceled -= OnTouchEnd;
        
        inputActions.Disable();
    }
    
    void OnDestroy()
    {
        inputActions?.Dispose();
        
        // Clean up preview
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
        }
    }
    private void OnGamePaused()
{
    // Disable input when paused
    inputActions.Disable();
}

private void OnGameResumed()
{
    // Re-enable input when resumed
    inputActions.Enable();
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
    
    private void SelectNextFruit()
    {
        // Select a random fruit for the next drop
        int randomIndex = Random.Range(0, fruits.Length);
        nextFruitToDropPrefab = fruits[randomIndex];
    }
    
    private void ShowPreview()
    {
        if (nextFruitToDropPrefab == null) return;
        
        // Destroy existing preview
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
        }
        
        // Create new preview
        Vector3 previewPosition = transform.position + previewOffset;
        currentPreview = Instantiate(nextFruitToDropPrefab, previewPosition, Quaternion.identity);
        
        // Set parent if specified
        if (previewParent != null)
        {
            currentPreview.transform.SetParent(previewParent);
        }
        
        // Scale down the preview
        currentPreview.transform.localScale = Vector3.one * previewScale;
        
        // Remove physics components from preview
        RemovePhysicsFromPreview(currentPreview);
        
        // Apply preview material if specified
        ApplyPreviewMaterial(currentPreview);
        
        // Add a tag or layer to identify it as preview
        currentPreview.tag = "Preview";
        currentPreview.name = nextFruitToDropPrefab.name + "_Preview";
    }
    
    private void RemovePhysicsFromPreview(GameObject preview)
    {
        // Remove rigidbody to prevent physics interactions
        Rigidbody rb = preview.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
        
        // Set colliders as triggers or remove them
        Collider[] colliders = preview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.isTrigger = true; // Make it a trigger so it doesn't interfere
            // Or you can destroy it: DestroyImmediate(col);
        }
    }
    
    private void ApplyPreviewMaterial(GameObject preview)
    {
        if (previewMaterial == null) return;
        
        // Apply preview material to all renderers
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = previewMaterial;
            }
            renderer.materials = materials;
        }
    }
    
    private void UpdatePreviewPosition()
    {
        if (currentPreview != null)
        {
            currentPreview.transform.position = transform.position + previewOffset;
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
    if (!GameManager.Instance.IsGameActive()) return; // Don't process if game is paused
    
    isTouching = true;
    Debug.Log("Touch started");
    ShowPreview();
}

    // Called when touch ends - This is where you drop the fruit

    private void OnTouchEnd(InputAction.CallbackContext context)
    {
        if (!GameManager.Instance.IsGameActive()) return; // Don't process if game is paused

        isTouching = false;
        currentMoveInput = Vector2.zero;
        Debug.Log("Touch ended - Dropping fruit!");
        DropFruit();
        HidePreview();
        SelectNextFruit();
        ShowPreview();
    }
    
    private void DropFruit()
    {
        // Drop the fruit that was being previewed
        if (nextFruitToDropPrefab != null)
        {
            Instantiate(nextFruitToDropPrefab, transform.position, Quaternion.identity);
        }
    }
    
    private void HidePreview()
    {
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
            currentPreview = null;
        }
    }
    
    void Update()
    {
        // Smooth the movement input for better feel
        smoothedMoveInput = Vector2.Lerp(smoothedMoveInput, currentMoveInput, smoothing);
        
        // Apply movement to the fruit dropper with boundary constraints
        MoveDropper(smoothedMoveInput);
        
        // Update preview position to follow the dropper
        UpdatePreviewPosition();
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
