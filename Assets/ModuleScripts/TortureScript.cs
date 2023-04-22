using KeepCoding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    internal int _height = 4, _width = 4;
    internal int _gridSize = 4;
    internal KMSelectable[] _grid;

    class TortureSettings
    {
        public int Modulus = 10;
        public int MinAffected = 5;
        public int MaxAffected = 7;
        public int Height = 4;
        public int Width = 4;
    }
    private TortureSettings _settings = new TortureSettings();
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
                        { "Text", "Height of Grid. Full size of the grid must at least be 5 tiles." }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", "Width" },
                        { "Text", "Width of Grid. Full size of the grid must at least be 5 tiles." }
                    }
                }
            }
        }
    };

    private string[] _solveMessages = { "CONGRATULATIONS!", "おめでとう！ありがとうございます", "MOD  ULESOL  VED!" };
    internal bool _isModuleSolved, _isSeedSet, _isNotEnoughTime, _isLogging, _isAutosolve, _isRalpMode = false, _isAprilFools;
    private int _seed, _modulus;
    internal int[] _twitchPlaysAutosolver;
    private double[] _solveTimings = { 0.485, 0.86, 1.235, 1.61, 1.985, 2.36, 2.735, 3.485, 3.86, 4.235, 4.61, 4.985, 5.735, 5.985, 6.235, 6.485, 6.86, 7.235, 7.485, 7.735, 7.985, 8.36, 8.735, 9.11, 9.485, 9.86, 10.235, 10.61, 10.985, 11.36, 11.735, 12.11, 12.485, 13.61, 13.985, 14.36, 14.735, 15.11, 15.485, 15.985, 16.235, 16.485, 16.735, 16.985, 17.36, 17.735, 18.485, 18.86, 18.985, 19.11, 19.235, 19.36, 19.485, 19.61, 19.735, 19.86, 19.985, 20.11, 20.235, 20.36, 20.485, 20.61, 20.735, 20.828, 20.922, 21.016, 21.11, 21.203, 21.297, 21.391, 21.485, 21.61, 21.735, 21.86, 21.985, 22.11, 22.235, 22.36, 22.485, 22.61, 22.735, 22.86, 22.985, 23.078, 23.172, 23.266, 23.36, 23.453, 23.547, 23.641, 23.735, 23.828, 23.922, 24.016, 24.11, 24.203, 24.297, 24.391, 24.285 };

    // Use this for initialization
    private void Start()
    {
        _isAprilFools = DateTime.Now.Day == 1 && DateTime.Now.Month == 4;

        _moduleRender.color = new Color32(255, 255, 255, 255);
        if (!_isSeedSet)
        {
            _seed = Rnd.Range(int.MinValue, int.MaxValue);
            Log("The seed is: " + _seed.ToString());
            _isSeedSet = true;
        }

        _rnd = new System.Random(_seed);
        // SET SEED ABOVE IN CASE OF BUGS!!
        // _rnd = new System.Random(loggedSeed);
        _module = Get<KMBombModule>();
        _info = Get<KMBombInfo>();

        _loggingKey.Assign(onInteract: () => { PressLogKey(); });

        ModConfig<TortureSettings> Config = new ModConfig<TortureSettings>("TortureSettings");
        _settings = Config.Read();

        // _width = _isAprilFools ? _rnd.Next(1, 256) : _settings.Width;
        // _height = _isAprilFools ? _rnd.Next(1, 256) : _settings.Height;

        _width = 26;
        _height = 26;

        if(_width * _height < 5 && !_isAprilFools)
        {
            if (_width == _height)
                _width = (int)Mathf.Ceil(5f / _height);
            else if (_width < _height)
                _height = 5;
            else
                _width = 5;
            _settings.Height = _height;
            _settings.Width = _width;
        }

        _gridSize = _width * _height;

        _grid = new KMSelectable[_gridSize];

        _settings.MinAffected = Mathf.Clamp(_settings.MinAffected, _gridSize / 3, _gridSize);
        _settings.MaxAffected = Mathf.Clamp(_settings.MaxAffected, Mathf.Max((int)(_gridSize / 1.5f), _settings.MinAffected), _gridSize);

        Config.Write(_settings);

        _modulus = _settings.Modulus <= 1 ? 10 : _settings.Modulus;
        GenerateGrid(_rnd.Next(0, _modulus));
    }

    private void GenerateGrid(int initialFinalValue)
    {
        _module.GetComponent<KMSelectable>().Children = new KMSelectable[_grid.Length + 1];
        _referencePoint.transform.localScale = new Vector3(0.03f * (4f / _width), 0.001f, 0.03f * (4f / _height));
        _referencePoint.transform.localPosition = new Vector3(-0.0787f + 0.0315f * (2f / _width), 0.0151f, 0.047f - 0.0315f * (2f / _height));

        for (int i = 0; i < _grid.Length; i++)
        {
            int x = i;
            _grid[i] = Instantiate(_referencePoint, _module.transform);
            _grid[i].GetComponent<Selectable>().SetValues(i, _isRalpMode ? _rnd.Next(_gridSize / 2, _gridSize + 1) : _rnd.Next(Math.Max(_settings.MinAffected, _gridSize / 3), Math.Max(_settings.MaxAffected + 1, (int)(_gridSize / 1.5f))), _grid.Length, initialFinalValue, _modulus);
            _grid[i].GetComponent<Selectable>().SetText(initialFinalValue.ToString());
            _grid[i].GetComponent<Selectable>().SetColour(i, false, _isLogging);

            _grid[i].transform.localPosition += new Vector3(0.0315f * (4f / _width) * (i % _width), 0, -0.0315f * (4f / _height) * (i / _width));
            _grid[i].Parent = _module.GetComponent<KMSelectable>();

            _grid[i].GetComponent<MeshRenderer>().material = _colours[((i % _width) ^ (i / _width)) & 1];
            _module.GetComponent<KMSelectable>().Children[x] = _grid[x];
        }
        _module.GetComponent<KMSelectable>().Children[_grid.Length] = _loggingKey;

        _referencePoint.gameObject.SetActive(false);
        _module.GetComponent<KMSelectable>().UpdateChildrenProperly();
        Randomise();
    }

    private void Randomise()
    {
        _twitchPlaysAutosolver = new int[_grid.Length];
        for (int i = 0; i < _grid.Length; i++)
        {
            int random = _rnd.Next(0, _modulus);
            _twitchPlaysAutosolver[i] = (_modulus - random) % _modulus;
            for (int j = 0; j < random; j++)
                _grid[i].GetComponent<Selectable>().ApplyChanges();
        }
        for (int i = 0; i < _grid.Length; i++)
            _grid[i].GetComponent<Selectable>().SetColour(i, false, _isLogging);
        GenerateLogging();
    }

    private void GenerateLogging(bool forced = false)
    {
        if (!forced)
        {
            string grid = _grid.Select(x => x.GetComponent<Selectable>().GetValue()).Join("");
            for (int i = 0; i < _height - 1; i++)
                grid = grid.Insert(_width * (i + 1) + i, "|");
            Log("The initial state is: " + grid);
            for (int i = 0; i < _grid.Length; i++)
            {
                string j = _grid[i].GetComponent<Selectable>().GetOffsets().Join("");
                for (int k = 0; k < _height - 1; k++)
                    j = j.Insert(_width * (k + 1) + k, "|");
                Log("The matrix for tile " + "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[i % _width].ToString() + "123456789abcdefghijklmnopqrstuvwxyz"[i / _width].ToString() + " is: " + j);
            }
        }
        string log = _twitchPlaysAutosolver.Join("");
        for (int i = 0; i < _height - 1; i++)
            log = log.Insert(_width * (i + 1) + i, "|");
        Log("The solution grid is: " + log);
    }

    internal void PressLogKey()
    {
        if (_isModuleSolved)
            return;

        _isLogging = true;
        _moduleRender.color = new Color32(192, 0, 0, 255);
        Enumerable.Range(0, _grid.Length).Where(x => (((x % _width) ^ (x / _width)) & 1) == 1).ForEach(x => _grid[x].GetComponent<Selectable>().SetTileColour(x, 3));
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
        _isModuleSolved = true;
        Log("Module solved! Congratulations!");
        for (int i = 0; i < _grid.Length; i++)
            _grid[i].GetComponent<Selectable>().SetText("");
        if (_info.GetTime() < 60 || _info.GetStrikes() + 1 == Game.Mission.GeneratorSetting.NumStrikes)
            _isNotEnoughTime = true;

        if(!_isAutosolve)
            StartCoroutine(SolveAnimation());
        else
        {
            _module.HandlePass();
            string text = "AUTOSOLVECHEATER";

            for (int i = 0; i < _grid.Length; i++)
            {
                _grid[i].GetComponent<Selectable>().SetText(text[i].ToString());
                _grid[i].GetComponent<Selectable>().SetColour(i, false, _isLogging);
            }
        }
    }

    private IEnumerator SolveAnimation()
    {
        if (_isNotEnoughTime)
            _module.HandlePass();

        PlaySound(_module.transform, false, _sounds[2]);
        yield return new WaitForSeconds(1f);

        PlaySound(_module.transform, false, _sounds[1]);
        double elapsed = 0;

        string test = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam condimentum lorem quis volutpat lortis. Nunc suscipit odio velit, sed tempor mauris ullamcorper id. Interdum et malesuada fames ac anteor";
        string[] structure = StringToBinary(test).Split(_grid.Length).ToArray();

        int part = -1;
        while (elapsed < 24.3)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (_solveTimings.Any(x => elapsed - x < 0.0175 && elapsed - x > 0))
            {
                part++;
                part %= structure.Length;
                for (int i = 0; i < _grid.Length; i++)
                    _grid[i].GetComponent<Selectable>().SetTileColour(i, _rnd.Next(0, 2) == 1 ? (!_isLogging ? 2 : 4) : (_isLogging ? ((((i % _width) ^ (i / _width)) & 1) == 1 ? 3 : 0) : (((i % _width) ^ (i / _width)) & 1))); // _grid[i].GetComponent<Selectable>().SetSolvedColour(i, structure[part][i] == '1');
            }
        }
        for (int i = 0; i < _grid.Length; i++)
            _grid[i].GetComponent<Selectable>().SetTileColour(i, !_isLogging ? 2 : 3);
        if (!_isNotEnoughTime)
            _module.HandlePass();

        int messagePicker = _rnd.Next(0, _solveMessages.Length);
        for (int i = 0; i < _grid.Length; i++)
            _grid[i].GetComponent<Selectable>().SetText(_solveMessages[messagePicker][i].ToString());

        // Extras
        // yield return new WaitForSeconds(3f);
        // int[] randomiser = Enumerable.Range(0, _grid.Length).ToArray().Shuffle();
        // for (int i = 0; i < _grid.Length; i++)
        // {
        //     _grid[randomiser[i]].gameObject.SetActive(false);
        //     yield return new WaitForSeconds(.15f);
        // }
    }
}
