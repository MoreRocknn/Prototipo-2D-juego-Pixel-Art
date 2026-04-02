using UnityEngine;

[System.Serializable]
public class BackgroundElement
{
    public SpriteRenderer BackgroundSprite;// El SpriteRenderer del fondo
    [Range(0,1)]public float scrollSpeed; // Velocidad de desplazamiento del fondo
    [HideInInspector] public Material SpriteMaterial; // Material del fondo para ajustar el offset
}

public class BackgroundScrolling : MonoBehaviour
{
    private const float SCROLL_MILTIPLIER = 0.01f; // Multiplicador para ajustar la velocidad de desplazamiento
    [Header("Elementos del Fondo")]
    [SerializeField] private BackgroundElement[] backgroundElements; // Array de elementos del fondo
    private void Start()
    {
        // Inicializamos los materiales de cada elemento del fondo
        foreach (BackgroundElement element in backgroundElements)
        {
            element.SpriteMaterial = element.BackgroundSprite.material;
        }
    }
    private void Update()
    {
        // Desplazamos cada elemento del fondo según su velocidad
        foreach (BackgroundElement element in backgroundElements)
        {
            element.SpriteMaterial.mainTextureOffset = new Vector2(transform.position.x * element.scrollSpeed * SCROLL_MILTIPLIER, 0);
        }
    }
}
