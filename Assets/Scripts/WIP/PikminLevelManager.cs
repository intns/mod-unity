using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public enum TimeSettingType
{
    Night = 0,
    Morning,
    Day,
    Evening,
    Movie,
}

public class Light
{
    public int Type { get; set; }
    public int Attach { get; set; }
    public float _FOV { get; set; }
    public int[] Colour { get; set; }

    public Vector3 _Position;
    public Vector3 _Direction;
}

public class INIFile
{
    public string _MapPath;
    public string _CinePath;

    public int _StageIndex;

    public float _DayMultiply;
}

public class PikminLevelManager : MonoBehaviour
{
    [Serializable]
    public enum StageLoad
    {
        ImpactSite,
        ForestOfHope,
        ForestNavel,
        DistantSpring,
        FinalTrial,
    }

    string _DataDirPath = "Assets/~DataDir/";

    [Header("Settings")]
    [SerializeField]
    StageLoad _ToLoad = StageLoad.ImpactSite;

    [SerializeField]
    INIFile _SettingsFile;

    private string[] GetStageList()
    {
        return Directory.GetFiles(_DataDirPath + "Stages/", "*.ini", SearchOption.TopDirectoryOnly);
    }

    private void Awake()
    {
        string[] stages = GetStageList();
        string targetPath = string.Empty;
        switch (_ToLoad)
        {
            case StageLoad.ImpactSite:
                targetPath = _DataDirPath + "Stages/practice.ini";
                break;
            case StageLoad.ForestOfHope:
                targetPath = _DataDirPath + "Stages/stage1.ini";
                break;
            case StageLoad.ForestNavel:
                targetPath = _DataDirPath + "Stages/stage2.ini";
                break;
            case StageLoad.DistantSpring:
                targetPath = _DataDirPath + "Stages/stage3.ini";
                break;
            case StageLoad.FinalTrial:
                targetPath = _DataDirPath + "Stages/last.ini";
                break;
        }

        using StreamReader sr = new(targetPath);
        _SettingsFile = new();
        string script = sr.ReadToEnd();
    }
}
