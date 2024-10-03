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
    Movie
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

public class TimeSetting
{
    public TimeSettingType Type { get; set; }
    public List<Light> Lights { get; set; }
    public int[] AmbientColour { get; set; }
    public int[] SpecularColour { get; set; }
    public int[] FogColour { get; set; }
    public float[] FogDist { get; set; }

    public void Parse(ref StreamReader sr)
    {
        Lights = new();

        string line;
        int lightIndex = -1;
        while ((line = sr.ReadLine()) != null)
        {
            if (line.Contains("light"))
            {
                Lights.Add(new Light());
                lightIndex++;
            }
            else if (line.Contains("type"))
            {
                Lights[lightIndex].Type = int.Parse(Regex.Match(line, @"\d+").Value);
            }
            else if (line.Contains("attach"))
            {
                Lights[lightIndex].Attach = int.Parse(Regex.Match(line, @"\d+").Value);
            }
            else if (line.Contains("fov"))
            {
                Lights[lightIndex]._FOV = float.Parse(Regex.Match(line, @"-?\d+\.?\d*").Value);
            }
            else if (line.Contains("position"))
            {
                float[] values = Array.ConvertAll(
                    Regex
                        .Matches(line, @"-?\d+\.?\d*")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray(),
                    float.Parse
                );

                Debug.Assert(values.Length == 3, "Invalid number of values for light position");
                Lights[lightIndex]._Position = new Vector3(values[0], values[1], values[2]);
            }
            else if (line.Contains("direction"))
            {
                float[] values = Array.ConvertAll(
                    Regex
                        .Matches(line, @"-?\d+\.?\d*")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray(),
                    float.Parse
                );

                Debug.Assert(values.Length == 3, "Invalid number of values for light direction");
                Lights[lightIndex]._Direction = new Vector3(values[0], values[1], values[2]);
            }
            else if (line.Contains("colour"))
            {
                Lights[lightIndex].Colour = Array.ConvertAll(
                    Regex.Matches(line, @"\d+").Cast<Match>().Select(m => m.Value).ToArray(),
                    int.Parse
                );
            }
            else if (line.Contains("ambient"))
            {
                line = sr.ReadLine();
                AmbientColour = Array.ConvertAll(
                    Regex.Matches(line, @"\d+").Cast<Match>().Select(m => m.Value).ToArray(),
                    int.Parse
                );
            }
            else if (line.Contains("specular"))
            {
                line = sr.ReadLine();
                SpecularColour = Array.ConvertAll(
                    Regex.Matches(line, @"\d+").Cast<Match>().Select(m => m.Value).ToArray(),
                    int.Parse
                );
            }
            else if (line.Contains("fog"))
            {
                line = sr.ReadLine();
                if (line.Contains("colour"))
                {
                    FogColour = Array.ConvertAll(
                        Regex.Matches(line, @"\d+").Cast<Match>().Select(m => m.Value).ToArray(),
                        int.Parse
                    );
                }
                else if (line.Contains("dist"))
                {
                    FogDist = Array.ConvertAll(
                        Regex
                            .Matches(line, @"-?\d+\.?\d*")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToArray(),
                        float.Parse
                    );
                }
            }
        }
    }
}

public class INIFile
{
    public string _MapPath;
    public string _CinePath;

    public int _StageIndex;

    public float _DayMultiply;

    public List<TimeSetting> TimeSettings { get; set; }

    public void Read(IEnumerable<Token> tokens)
    {
        /*string line;
        while ((line = reader.ReadLine()) != null)
        {
            string[] tokens = line.Split(' ');

            for (int i = 0; i < tokens.Length; i++)
            {
                string current = tokens[i];

                if (current == "map_file")
                {
                    i++;
                    _MapPath = tokens[i];
                }
                else if (current == "cine_file")
                {
                    i++;
                    _CinePath = tokens[i];
                }
                else if (current == "stageIndex")
                {
                    i++;
                    _StageIndex = int.Parse(tokens[i]);
                }
                else if (current == "day_multiply")
                {
                    i++;
                    _DayMultiply = float.Parse(tokens[i]);
                }
                else if (current == "dayMgr")
                {
                    i++; // {
                    reader.ReadLine();
                    i++; // numSettings

                    Debug.Log(tokens[i]);
                    int numSettings = int.Parse(tokens[i]);
                    TimeSettings = new();
                    for (int j = 0; j < numSettings; j++)
                    {
                        TimeSettings[j] = new();
                        TimeSettings[j].Parse(ref reader);
                    }
                }
            }
        }*/
    }
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
        return System.IO.Directory.GetFiles(
            _DataDirPath + "Stages/",
            "*.ini",
            System.IO.SearchOption.TopDirectoryOnly
        );
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
        ScriptTokenizer tokenizer = new ScriptTokenizer();
        IEnumerable<Token> tokens = tokenizer.Tokenize(script);
        _SettingsFile.Read(tokens);
    }
}
