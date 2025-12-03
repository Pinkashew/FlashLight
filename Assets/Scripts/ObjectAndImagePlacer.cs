using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ImageLoaderAndPlacer : MonoBehaviour
{
    [Header("Image Settings")]
    public string imageFolderPath = "Images";
    public List<Texture2D> loadedTextures = new List<Texture2D>();

    [Header("Object Placement Settings")]
    public int gridSize = 25;
    public float cellSize = 1f;
    public Vector3 gridCenter = Vector3.zero;
    public GameObject objectPrefab;
    public int numberOfObjects = 10;
    public Vector3 objectOffset = Vector3.zero;

    [Header("Material Settings")]
    public Material baseMaterial; // Assign a working material here in Inspector

    [Header("Visualization")]
    public bool showGridGizmos = true;
    public Color gridColor = Color.white;

    private List<Vector2Int> usedPositions = new List<Vector2Int>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        LoadImagesAndPlaceObjects();
    }

    [ContextMenu("Load Images and Place Objects")]
    public void LoadImagesAndPlaceObjects()
    {
        LoadImagesFromFolder();
        PlaceObjectsWithRandomImages();
    }

    [ContextMenu("Load Images From Folder")]
    public void LoadImagesFromFolder()
    {
        loadedTextures.Clear();

        // Method 1: Try Resources folder first
        string resourcesPath = imageFolderPath;
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcesPath);

        if (textures.Length > 0)
        {
            loadedTextures.AddRange(textures);
            Debug.Log($"Loaded {loadedTextures.Count} images from Resources/{imageFolderPath}");
            return;
        }

        // Method 2: Try StreamingAssets
        LoadFromStreamingAssets();

        if (loadedTextures.Count == 0)
        {
            Debug.LogWarning("No images found! Please add images to Resources/Images or StreamingAssets/Images folder");
        }
    }

    private void LoadFromStreamingAssets()
    {
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, imageFolderPath);

        if (!Directory.Exists(streamingAssetsPath))
        {
            Debug.LogWarning($"StreamingAssets folder not found: {streamingAssetsPath}");
            return;
        }

        string[] imageFiles = Directory.GetFiles(streamingAssetsPath, "*.*", SearchOption.AllDirectories)
            .Where(file =>
                file.ToLower().EndsWith(".png") ||
                file.ToLower().EndsWith(".jpg") ||
                file.ToLower().EndsWith(".jpeg"))
            .ToArray();

        foreach (string filePath in imageFiles)
        {
            Texture2D texture = LoadTextureFromFile(filePath);
            if (texture != null)
            {
                loadedTextures.Add(texture);
            }
        }

        Debug.Log($"Loaded {loadedTextures.Count} images from StreamingAssets/{imageFolderPath}");
    }

    private Texture2D LoadTextureFromFile(string filePath)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                texture.name = Path.GetFileNameWithoutExtension(filePath);
                return texture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading texture {filePath}: {e.Message}");
        }

        return null;
    }

    [ContextMenu("Place Objects with Random Images")]
    public void PlaceObjectsWithRandomImages()
    {
        ClearExistingObjects();
        usedPositions.Clear();

        if (objectPrefab == null)
        {
            Debug.LogError("Object Prefab is not assigned!");
            return;
        }

        if (numberOfObjects > gridSize * gridSize)
        {
            numberOfObjects = gridSize * gridSize;
        }

        // Test material creation first
        if (!TestMaterialCreation())
        {
            Debug.LogError("Material creation failed! Objects will be placed without textures.");
        }

        for (int i = 0; i < numberOfObjects; i++)
        {
            Vector2Int randomPosition = GetUniqueRandomPosition();
            if (randomPosition.x == -1) break;

            Vector3 worldPosition = GridToWorldPosition(randomPosition);
            GameObject newObject = Instantiate(objectPrefab, worldPosition, Quaternion.identity, transform);
            spawnedObjects.Add(newObject);

            if (loadedTextures.Count > 0)
            {
                ApplyRandomTextureToObject(newObject);
            }
            usedPositions.Add(randomPosition);
        }

        Debug.Log($"Placed {spawnedObjects.Count} objects");
    }

    private bool TestMaterialCreation()
    {
        // Create a test object to verify material works
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "MaterialTest";

        Renderer renderer = testCube.GetComponent<Renderer>();
        Material testMaterial = GetOrCreateMaterial();

        if (testMaterial == null)
        {
            Debug.LogError("Failed to create test material!");
            DestroyImmediate(testCube);
            return false;
        }

        if (loadedTextures.Count > 0)
        {
            testMaterial.mainTexture = loadedTextures[0];
        }

        renderer.material = testMaterial;
        Debug.Log("Material test successful!");

        // Clean up test object
        DestroyImmediate(testCube);
        return true;
    }

    private Material GetOrCreateMaterial()
    {
        // If baseMaterial is assigned, use it
        if (baseMaterial != null)
        {
            return new Material(baseMaterial);
        }

        // Try to find a working shader
        Shader[] shaderOptions = {
            Shader.Find("Standard"),
            Shader.Find("Universal Render Pipeline/Lit"),
            Shader.Find("HDRP/Lit"),
            Shader.Find("Legacy Shaders/Diffuse"),
            Shader.Find("Sprites/Default")
        };

        Shader workingShader = null;
        foreach (Shader shader in shaderOptions)
        {
            if (shader != null)
            {
                workingShader = shader;
                break;
            }
        }

        if (workingShader == null)
        {
            Debug.LogError("No working shader found!");
            return null;
        }

        Material newMaterial = new Material(workingShader);
        newMaterial.name = "DynamicMaterial_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        return newMaterial;
    }

    private void ApplyRandomTextureToObject(GameObject targetObject)
    {
        if (loadedTextures.Count == 0) return;

        Texture2D randomTexture = loadedTextures[Random.Range(0, loadedTextures.Count)];
        ApplyTextureToObject(targetObject, randomTexture);
    }

    private void ApplyTextureToObject(GameObject targetObject, Texture2D texture)
    {
        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = targetObject.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            Material newMaterial = GetOrCreateMaterial();
            if (newMaterial != null)
            {
                newMaterial.mainTexture = texture;
                newMaterial.name = "Mat_" + texture.name;
                renderer.material = newMaterial;
            }
            else
            {
                Debug.LogError($"Failed to create material for {targetObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"No Renderer found on {targetObject.name}");
        }
    }

    [ContextMenu("Clear All Objects")]
    public void ClearExistingObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
    }

    [ContextMenu("Fix All Materials")]
    public void FixAllMaterials()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && (renderer.material == null || renderer.material.shader == null))
                {
                    ApplyRandomTextureToObject(obj);
                }
            }
        }
        Debug.Log("Attempted to fix all materials");
    }

    private Vector2Int GetUniqueRandomPosition()
    {
        if (usedPositions.Count >= gridSize * gridSize)
            return new Vector2Int(-1, -1);

        Vector2Int randomPos;
        int attempts = 0;
        const int maxAttempts = 1000;

        do
        {
            randomPos = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
            attempts++;

            if (attempts > maxAttempts)
                return new Vector2Int(-1, -1);

        } while (usedPositions.Contains(randomPos));

        return randomPos;
    }

    private Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        float x = (gridPosition.x - gridSize * 0.5f + 0.5f) * cellSize + gridCenter.x;
        float z = (gridPosition.y - gridSize * 0.5f + 0.5f) * cellSize + gridCenter.z;
        return new Vector3(x, gridCenter.y, z) + objectOffset;
    }

    void OnDrawGizmos()
    {
        if (!showGridGizmos) return;

        Gizmos.color = gridColor;
        for (int x = 0; x <= gridSize; x++)
        {
            Vector3 start = GridToWorldPosition(new Vector2Int(x, 0)) - new Vector3(cellSize * 0.5f, 0, 0);
            Vector3 end = GridToWorldPosition(new Vector2Int(x, gridSize)) - new Vector3(cellSize * 0.5f, 0, 0);
            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= gridSize; y++)
        {
            Vector3 start = GridToWorldPosition(new Vector2Int(0, y)) - new Vector3(0, 0, cellSize * 0.5f);
            Vector3 end = GridToWorldPosition(new Vector2Int(gridSize, y)) - new Vector3(0, 0, cellSize * 0.5f);
            Gizmos.DrawLine(start, end);
        }
    }
}