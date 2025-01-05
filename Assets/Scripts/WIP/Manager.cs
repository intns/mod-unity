using UnityEngine;
using UnityEditor;
using System.IO;

public class Manager : MonoBehaviour
{
    [SerializeField]
    MODUnity.CreateFlags _Flags = MODUnity.CreateFlags.CreateSkeleton;

    [SerializeField]
    Transform _Root = null;

    [SerializeField]
    MODUnity _Mod = null;

    private void Awake()
    {
        string filePath = EditorUtility.OpenFilePanel("Select a Pikmin 1 Model file", "", "mod");
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        using FileStream fileStream = new(filePath, FileMode.Open);
        using BinaryReader reader = new(fileStream);

        _Mod = new MODUnity(reader);
        _Mod.Create(_Flags, _Root != null ? _Root : transform);
    }
}
