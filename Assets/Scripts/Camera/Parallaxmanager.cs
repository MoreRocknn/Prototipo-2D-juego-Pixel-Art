using UnityEngine;

// ============================================================
//  ParallaxManager.cs
//  Opcional — ponlo en un GameObject vacío "ParallaxManager".
//  Mantiene una referencia a todas las capas y permite
//  ajustarlas en bloque desde el Inspector.
//  
//  Si prefieres, puedes ignorar este script y solo usar
//  ParallaxLayer.cs en cada capa individualmente.
// ============================================================

public class ParallaxManager : MonoBehaviour
{
    [System.Serializable]
    public class LayerConfig
    {
        public string nombre;
        public ParallaxLayer capa;

        [Range(0f, 1f)]
        public float factorX = 0.5f;

        [Range(0f, 1f)]
        public float factorY = 0f;
    }

    [Header("Capas de parallax (de más lejana a más cercana)")]
    [SerializeField] private LayerConfig[] capas;

    // Aplica los factores definidos aquí a cada ParallaxLayer
    private void OnValidate()
    {
        if (capas == null) return;
        foreach (var c in capas)
        {
            if (c.capa == null) continue;
            // Usa reflection para setear los campos privados en el editor
            var tipo = c.capa.GetType();
            var fx = tipo.GetField("parallaxFactorX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fy = tipo.GetField("parallaxFactorY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fx?.SetValue(c.capa, c.factorX);
            fy?.SetValue(c.capa, c.factorY);
        }
    }
}