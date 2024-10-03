using UnityEngine;
using UnityEditor;
using System.IO;

public class Manager : MonoBehaviour
{
    [SerializeField]
    MODUnity.CreateFlags _Flags = MODUnity.CreateFlags.CreateSkeleton;

    [SerializeField]
    MODUnity _Mod = null;

    [SerializeField]
    string _OutputPath = "Assets/";

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        string filePath = EditorUtility.OpenFilePanel("Select a file", "", "");
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        using FileStream fileStream = new(filePath, FileMode.Open);
        using BinaryReader reader = new(fileStream);

        _Mod = new MODUnity(reader);
        _Mod.Create(_Flags, transform);
    }
}
