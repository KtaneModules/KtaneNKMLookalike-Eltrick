using KeepCoding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class TortureScript : ModuleScript
{
    private KMBombModule _module;
    private KMBombInfo _info;
    private System.Random _rnd;

    [SerializeField]
    private KMSelectable _referencePoint, _loggingKey;
    [SerializeField]
    internal Material[] _colours;
    [SerializeField]
    internal AudioClip[] _sounds;
    [SerializeField]
    private Material _moduleRender;
    [SerializeField]
    private TortureScriptTP _tortureScriptTP;

    internal int Height = 4, Width = 4;
    internal int GridSize { get { return Height * Width; } }
    internal KMSelectable[] _grid;

    internal class TortureSettings
    {
        public int Modulus = 10;
        public int MinAffected = 5;
        public int MaxAffected = 7;
        public int Height = 4;
        public int Width = 4;
    }
    internal TortureSettings Settings = new TortureSettings();
    private static Dictionary<string, object>[] _tweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "TortureSettings.json" },
            { "Name", "Torture" },
            { "Listings", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "Key", "Modulus" },
                        { "Text", "The modulus the module uses. Cannot be 1 or less." }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", "MinAffected" },
                        { "Text", "Minimum number of affected buttons. Clamps at a minimum of 3." }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", "MaxAffected" },
                        { "Text", "Maximum number of affected buttons. Clamps at a minimum of 4." }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", "Height" },
                        { "Text", "Height of Grid. Full size of the grid must at least be 5 tiles. Also Clamps at 26." }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", "Width" },
                        { "Text", "Width of Grid. Full size of the grid must at least be 5 tiles. Also Clamps at 26." }
                    }
                }
            }
        }
    };

    private string[] _solveMessages = { "WHAT", "AUTOSOLVECHEATER" };
    internal bool IsModuleSolved, IsSeedSet, IsNotEnoughTime, IsLogging, IsAutosolve, IsRalpMode = false, IsAprilFools, IsFirstTime = true, IsMission = false;
    private int _seed;
    internal int Modulus, MinAffected, MaxAffected;
    internal int[] TwitchPlaysAutosolver;
    private float[] _solveTimings = { 0.485f, 0.86f, 1.235f, 1.61f, 1.985f, 2.36f, 2.735f, 3.485f, 3.86f, 4.235f, 4.61f, 4.985f, 5.735f, 5.985f, 6.235f, 6.485f, 6.86f, 7.235f, 7.485f, 7.735f, 7.985f, 8.36f, 8.735f, 9.11f, 9.485f, 9.86f, 10.235f, 10.61f, 10.985f, 11.36f, 11.735f, 12.11f, 12.485f, 13.61f, 13.985f, 14.36f, 14.735f, 15.11f, 15.485f, 15.985f, 16.235f, 16.485f, 16.735f, 16.985f, 17.36f, 17.735f, 18.485f, 18.86f, 18.985f, 19.11f, 19.235f, 19.36f, 19.485f, 19.61f, 19.735f, 19.86f, 19.985f, 20.11f, 20.235f, 20.36f, 20.485f, 20.61f, 20.735f, 20.828f, 20.922f, 21.016f, 21.11f, 21.203f, 21.297f, 21.391f, 21.485f, 21.61f, 21.735f, 21.86f, 21.985f, 22.11f, 22.235f, 22.36f, 22.485f, 22.61f, 22.735f, 22.86f, 22.985f, 23.078f, 23.172f, 23.266f, 23.36f, 23.453f, 23.547f, 23.641f, 23.735f, 23.828f, 23.922f, 24.016f, 24.11f, 24.203f, 24.297f, 24.391f, 24.285f };

    // Use this for initialization
    private void Start()
    {
        IsAprilFools = DateTime.Now.Day == 1 && DateTime.Now.Month == 4;

        _moduleRender.color = new Color32(255, 255, 255, 255);
        if (!IsSeedSet)
        {
            _seed = Rnd.Range(int.MinValue, int.MaxValue);
            Log("The seed is: " + _seed.ToString());
            IsSeedSet = true;
        }

        _rnd = new System.Random(_seed);
        // SET SEED ABOVE IN CASE OF BUGS!!
        // _rnd = new System.Random(loggedSeed);
        _module = Get<KMBombModule>();
        _info = Get<KMBombInfo>();

        _loggingKey.Assign(onInteract: () => { PressLogKey(); });

        ModConfig<TortureSettings> Config = new ModConfig<TortureSettings>("TortureSettings");
        Settings = Config.Read();

        Width = IsAprilFools ? _rnd.Next(1, 256) : Settings.Width;
        Height = IsAprilFools ? _rnd.Next(1, 256) : Settings.Height;

        // _width = 5;
        // _height = 1;

        if (Width * Height < 5 && !IsAprilFools)
        {
            if (Width == Height)
                Width = (int)Mathf.Ceil(5f / Height);
            else if (Width < Height)
                Height = 5;
            else
                Width = 5;
            Settings.Height = Height;
            Settings.Width = Width;
        }

        _grid = new KMSelectable[GridSize];

        MinAffected = Mathf.Clamp(Settings.MinAffected, GridSize / 3, GridSize);
        MaxAffected = Mathf.Clamp(Settings.MaxAffected, Mathf.Max((int)(GridSize / 1.5f), MinAffected), GridSize);

        Config.Write(Settings);

        Modulus = Settings.Modulus <= 1 ? 10 : Settings.Modulus;
        Modulus = Mathf.Clamp(Settings.Modulus, 2, int.MaxValue);

        MissionDescription();

        GenerateGrid(_rnd.Next(0, Modulus));
    }

    private void MissionDescription()
    {
        string missionDescription = Game.Mission.Description;
        Regex regex = new Regex(@"\[Torture\]\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)");

        if (missionDescription == null)
            return;

        var match = regex.Match(missionDescription);
        if (!match.Success)
            return;

        IsMission = true;

        Modulus = Mathf.Clamp(int.Parse(match.Groups[1].Value), 2, int.MaxValue);
        MinAffected = int.Parse(match.Groups[2].Value);
        MaxAffected = int.Parse(match.Groups[3].Value);
        Height = int.Parse(match.Groups[4].Value);
        Width = int.Parse(match.Groups[5].Value);
    }

    internal void GenerateGrid(int initialFinalValue)
    {
        if (!IsFirstTime)
            _grid.ForEach(x => Destroy(x.gameObject));

        _referencePoint.gameObject.SetActive(true);
        IsFirstTime = false;

        _grid = new KMSelectable[GridSize];

        _tortureScriptTP.UpdateHelpMessage(GridSize);

        _module.GetComponent<KMSelectable>().Children = new KMSelectable[GridSize + 1];
        _referencePoint.transform.localScale = new Vector3(0.03f * (4f / Width), 0.001f, 0.03f * (4f / Height));
        _referencePoint.transform.localPosition = new Vector3(-0.0787f + 0.0315f * (2f / Width), 0.0151f, 0.047f - 0.0315f * (2f / Height));

        for (int i = 0; i < GridSize; i++)
        {
            int x = i;
            _grid[i] = Instantiate(_referencePoint, _module.transform);
            _grid[i].GetComponent<Selectable>().SetValues(i, IsRalpMode ? _rnd.Next(GridSize / 2, GridSize + 1) : _rnd.Next(MinAffected, MaxAffected + 1), GridSize, initialFinalValue, Modulus);
            _grid[i].GetComponent<Selectable>().SetText(initialFinalValue.ToString());
            _grid[i].GetComponent<Selectable>().SetColour(i, false, IsLogging);

            _grid[i].transform.localPosition += new Vector3(0.0315f * (4f / Width) * (i % Width), 0, -0.0315f * (4f / Height) * (i / Width));
            _grid[i].Parent = _module.GetComponent<KMSelectable>();

            _grid[i].GetComponent<MeshRenderer>().material = _colours[((i % Width) ^ (i / Width)) & 1];
            _module.GetComponent<KMSelectable>().Children[x] = _grid[x];
        }
        _module.GetComponent<KMSelectable>().Children[GridSize] = _loggingKey;

        _referencePoint.gameObject.SetActive(false);
        _module.GetComponent<KMSelectable>().UpdateChildrenProperly();

        Randomise();
    }

    private void Randomise()
    {
        TwitchPlaysAutosolver = new int[GridSize];
        for (int i = 0; i < GridSize; i++)
        {
            int random = _rnd.Next(0, Modulus);
            TwitchPlaysAutosolver[i] = (Modulus - random) % Modulus;
            for (int j = 0; j < random; j++)
                _grid[i].GetComponent<Selectable>().ApplyChanges();
        }
        for (int i = 0; i < GridSize; i++)
            _grid[i].GetComponent<Selectable>().SetColour(i, false, IsLogging);
        GenerateLogging();
    }

    private void GenerateLogging(bool forced = false)
    {
        if (!forced)
        {
            string grid = _grid.Select(x => x.GetComponent<Selectable>().GetValue()).Join("");
            for (int i = 0; i < Height - 1; i++)
                grid = grid.Insert(Width * (i + 1) + i, "|");
            Log("The initial state is: " + grid);
            for (int i = 0; i < GridSize; i++)
            {
                string j = _grid[i].GetComponent<Selectable>().GetOffsets().Join("");
                for (int k = 0; k < Height - 1; k++)
                    j = j.Insert(Width * (k + 1) + k, "|");

                string logColumn = IntToString(i % Width, "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray());
                string real = "";

                if (logColumn.Length > 1)
                {
                    for (int k = 0; k < logColumn.Length - 1; k++)
                        real += ((char)(logColumn[k] - 1)).ToString();
                }
                real += logColumn[logColumn.Length - 1].ToString();

                Log("The matrix for tile " + real + (i / Width + 1).ToString() + " is: " + j);
            }
        }
        string log = TwitchPlaysAutosolver.Join("");
        for (int i = 0; i < Height - 1; i++)
            log = log.Insert(Width * (i + 1) + i, "|");
        Log("The solution grid is: " + log);
    }

    public static string IntToString(int value, char[] baseChars)
    {
        string result = string.Empty;
        int targetBase = baseChars.Length;

        do
        {
            result = baseChars[value % targetBase] + result;
            value /= targetBase;
        }
        while (value > 0);

        return result;
    }


    internal void PressLogKey()
    {
        if (IsModuleSolved)
            return;

        IsLogging = true;
        _moduleRender.color = new Color32(192, 0, 0, 255);
        Enumerable.Range(0, GridSize).Where(x => (((x % Width) ^ (x / Width)) & 1) == 1).ForEach(x => _grid[x].GetComponent<Selectable>().SetTileColour(x, 3));
        GenerateLogging(true);
    }

    private string StringToBinary(string s)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char c in s)
            stringBuilder.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
        return stringBuilder.ToString();
    }

    internal void SolveModule()
    {
        IsModuleSolved = true;
        Destroy(_loggingKey.gameObject);

        Log("Module solved! Congratulations!");
        for (int i = 0; i < GridSize; i++)
            _grid[i].GetComponent<Selectable>().SetText("");
        if (_info.GetTime() < 60 || _info.GetStrikes() + 1 == Game.Mission.GeneratorSetting.NumStrikes)
            IsNotEnoughTime = true;

        StartCoroutine(SolveAnimation());
    }

    private IEnumerator SolveAnimation()
    {
        if (IsNotEnoughTime)
            _module.HandlePass();

        _grid.ForEach(x => x.GetComponent<Selectable>().SetTileColour(x.GetComponent<Selectable>().GetIndex(), 0));

        if (!IsAutosolve)
        {
            PlaySound(_module.transform, false, _sounds[2]);
            yield return new WaitForSeconds(1f);

            PlaySound(_module.transform, false, _sounds[1]);
            float elapsed = 0;

            string test = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam condimentum lorem quis volutpat lortis. Nunc suscipit odio velit, sed tempor mauris ullamcorper id. Interdum et malesuada fames ac anteor";
            string[] structure = StringToBinary(test).Split(GridSize).ToArray();

            int part = -1;
            while (elapsed < 24.3f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (_solveTimings.Any(x => elapsed - x < 0.0175f && elapsed - x > 0f))
                {
                    part++;
                    part %= structure.Length;
                    for (int i = 0; i < GridSize; i++)
                        _grid[i].GetComponent<Selectable>().SetTileColour(i, _rnd.Next(0, 2) == 1 ? (!IsLogging ? 2 : 4) : (IsLogging ? ((((i % Width) ^ (i / Width)) & 1) == 1 ? 3 : 0) : 0)); // _grid[i].GetComponent<Selectable>().SetSolvedColour(i, structure[part][i] == '1');
                }
            }
        }

        //for (int i = 0; i < _gridSize; i++)
        //    _grid[i].GetComponent<Selectable>().SetTileColour(i, !IsLogging ? 2 : 3);
        if (!IsNotEnoughTime)
            _module.HandlePass();

        int messagePicker = IsAutosolve ? 1 : 0;
        string message = _solveMessages[messagePicker];

        for (int i = 0; i < message.Length * 2; i++)
        {
            _grid.ForEach(x => x.GetComponent<Selectable>().SetTileColour(x.GetComponent<Selectable>().GetIndex(), 0));

            DrawCharacter(message[i % message.Length]);
            yield return new WaitForSeconds(.25f);
        }

        _grid.ForEach(x => Destroy(x.gameObject));

        //if (_gridSize >= 64)
        //{
        //    message = "WHAT";
        //    for (int i = 0; i < _gridSize; i++)
        //        _grid[i].GetComponent<Selectable>().SetText(message[i % message.Length].ToString());
        //}
        //for (int i = 0; i < Math.Min(_gridSize, message.Length); i++)
        //    _grid[i].GetComponent<Selectable>().SetText(message[i].ToString());

        // Extras
        // yield return new WaitForSeconds(3f);
        // int[] randomiser = Enumerable.Range(0, _gridSize).ToArray().Shuffle();
        // for (int i = 0; i < _gridSize; i++)
        // {
        //     _grid[randomiser[i]].gameObject.SetActive(false);
        //     yield return new WaitForSeconds(.15f);
        // }
    }

    private void DrawCharacter(char character)
    {
        Vector2 bias = new Vector2((float)_rnd.NextDouble() / 10 - .05f, (float)_rnd.NextDouble() / 10 - .05f);
        switch (character)
        {
            case 'W':
                DrawLine(new Vector2(0, 0), new Vector2(.25f, 1), bias);
                DrawLine(new Vector2(.25f, 1), new Vector2(.5f, .5f), bias);
                DrawLine(new Vector2(.5f, .5f), new Vector2(.75f, 1), bias);
                DrawLine(new Vector2(.75f, 1), new Vector2(1, 0), bias);
                break;
            case 'H':
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(1, 0), new Vector2(1, 1), bias);
                DrawLine(new Vector2(0, .5f), new Vector2(1, .5f), bias);
                break;
            case 'A':
                DrawLine(new Vector2(0, 1), new Vector2(.5f, 0), bias);
                DrawLine(new Vector2(.5f, 0), new Vector2(1, 1), bias);
                DrawLine(new Vector2(.25f, .5f), new Vector2(.75f, .5f), bias);
                break;
            case 'T':
                DrawLine(new Vector2(0, 0), new Vector2(1, 0), bias);
                DrawLine(new Vector2(.5f, 0), new Vector2(.5f, 1), bias);
                break;
            case 'U':
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(0, 1), new Vector2(1, 1), bias);
                DrawLine(new Vector2(1, 1), new Vector2(1, 0), bias);
                break;
            case 'O':
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(0, 1), new Vector2(1, 1), bias);
                DrawLine(new Vector2(1, 1), new Vector2(1, 0), bias);
                DrawLine(new Vector2(1, 0), new Vector2(0, 0), bias);
                break;
            case 'S':
                DrawLine(new Vector2(1, 0), new Vector2(0, 0), bias);
                DrawLine(new Vector2(0, 0), new Vector2(0, .5f), bias);
                DrawLine(new Vector2(0, .5f), new Vector2(1, .5f), bias);
                DrawLine(new Vector2(1, .5f), new Vector2(1, 1), bias);
                DrawLine(new Vector2(1, 1), new Vector2(0, 1), bias);
                break;
            case 'L':
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(0, 1), new Vector2(1, 1), bias);
                break;
            case 'V':
                DrawLine(new Vector2(0, 0), new Vector2(.5f, 1), bias);
                DrawLine(new Vector2(.5f, 1), new Vector2(1, 0), bias);
                break;
            case 'E':
                DrawLine(new Vector2(1, 0), new Vector2(0, 0), bias);
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(0, 1), new Vector2(1, 1), bias);
                DrawLine(new Vector2(0, .5f), new Vector2(1, .5f), bias);
                break;
            case 'C':
                DrawLine(new Vector2(1, 0), new Vector2(0, 0), bias);
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(0, 1), new Vector2(1, 1), bias);
                break;
            case 'R':
                DrawLine(new Vector2(1, 0), new Vector2(0, 0), bias);
                DrawLine(new Vector2(0, 0), new Vector2(0, 1), bias);
                DrawLine(new Vector2(1, 0), new Vector2(1, .5f), bias);
                DrawLine(new Vector2(0, .5f), new Vector2(1, .5f), bias);
                DrawLine(new Vector2(.5f, .5f), new Vector2(1, 1), bias);
                break;
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Vector2 bias)
    {
        Vector2 resolution = new Vector2(Width, Height);

        start = Vector2.Scale(start, resolution - Vector2.one);
        end = Vector2.Scale(end, resolution - Vector2.one);

        Vector2 difference = end - start;

        Vector2 step;
        int stepCount;

        if (difference.sqrMagnitude < 1)
        {
            Vector2 v = start + bias;
            v = new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));

            int index = (int)v.x + Width * (int)v.y;
            _grid[index].GetComponent<Selectable>().SetTileColour(index, IsAutosolve ? 4 : 2);

            return;
        }
        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            step = new Vector2(Mathf.Sign(difference.x), difference.y / Mathf.Abs(difference.x));
            stepCount = Mathf.FloorToInt(Mathf.Abs(difference.x));
        }
        else
        {
            step = new Vector2(difference.x / Mathf.Abs(difference.y), Mathf.Sign(difference.y));
            stepCount = Mathf.FloorToInt(Mathf.Abs(difference.y));
        }


        for (int i = 0; i <= stepCount; i++)
        {
            Vector2 v = start + step * i + bias;
            v = new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));

            int index = (int)v.x + Width * (int)v.y;
            _grid[index].GetComponent<Selectable>().SetTileColour(index, IsAutosolve ? 4 : 2);
        }
    }
}
