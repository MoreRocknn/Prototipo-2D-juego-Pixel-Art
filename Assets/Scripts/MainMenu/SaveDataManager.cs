using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// ============================================================
//  SaveDataManager.cs
//  Maneja la existencia y lectura básica del archivo de guardado.
//  El menú lo usa solo para saber si "Continuar" debe estar activo.
// ============================================================

public static class SaveDataManager
{
    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "savegame.dat");

    /// <summary>Devuelve true si existe al menos un archivo de guardado.</summary>
    public static bool HasSaveFile() => File.Exists(SavePath);

    /// <summary>Elimina el archivo de guardado (Nueva Partida).</summary>
    public static void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    // Puedes expandir con Save<T>(T data) y Load<T>() según tu sistema de datos
}
