using UnityEngine;
using UnityEngine.Rendering;

// ============================================================
//  ParallaxSortingFix.cs
//
//  Cinemachine en Unity 6 puede romper el Order in Layer porque
//  su CinemachineBrain añade un componente que interfiere con
//  el sorting de los SpriteRenderers.
//
//  SOLUCIÓN: No uses Sorting Layers para ordenar el parallax.
//  Usa el eje Z en su lugar. Este script lo gestiona
//  automáticamente según el orden en el array.
//
//  Ponlo en un GameObject vacío "ParallaxRoot" y arrastra
//  todas las capas en orden (de más lejana a más cercana).
// ============================================================

public class ParallaxSortingFix : MonoBehaviour
{
    [Header("Capas de parallax (de más lejana a más cercana)")]
    [SerializeField] private ParallaxLayer[] capas;

    [Header("Separación Z entre capas")]
    [SerializeField] private float separacionZ = 1f;   // 1 unidad entre cada capa

    [Header("Z inicial (la más lejana)")]
    [SerializeField] private float zInicial = 10f;

    private void Awake()
    {
        AplicarOrdenZ();
    }

    // También lo aplica cuando cambias valores en el Inspector
    private void OnValidate()
    {
        if (capas == null) return;
        AplicarOrdenZ();
    }

    private void AplicarOrdenZ()
    {
        for (int i = 0; i < capas.Length; i++)
        {
            if (capas[i] == null) continue;

            // Capa 0 = más lejana = Z más alto
            // Capa N = más cercana = Z más bajo (o negativo)
            float z = zInicial - (i * separacionZ);
            var pos = capas[i].transform.position;
            capas[i].transform.position = new Vector3(pos.x, pos.y, z);

            // Asegura que el SpriteRenderer use el mismo sorting layer
            // y que NO use "Order in Layer" relativo — todo por Z
            var sr = capas[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Background";   // crea este layer en Tags & Layers
                sr.sortingOrder = i;              // por si acaso, también lo seteamos
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Aplicar orden Z ahora")]
    private void AplicarDesdeMenu() => AplicarOrdenZ();
#endif
}