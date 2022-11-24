using KeepCoding;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
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
    internal KMSelectable[] _grid = new KMSelectable[16];

    private string[] _solveMessages = { "CONGRATULATIONS!", "おめでとう！ありがとうございます", "MOD  ULESOL  VED!" };
    internal bool _isModuleSolved, _isSeedSet, _isNotEnoughTime, _isLogging, _isRalpMode = false;
    private int _seed, _modulus;
    internal int[] _twitchPlaysAutosolver;
    private double[] _solveTimings = { 0.485, 0.86, 1.235, 1.61, 1.985, 2.36, 2.735, 3.485, 3.86, 4.235, 4.61, 4.985, 5.735, 5.985, 6.235, 6.485, 6.86, 7.235, 7.485, 7.735, 7.985, 8.36, 8.735, 9.11, 9.485, 9.86, 10.235, 10.61, 10.985, 11.36, 11.735, 12.11, 12.485, 13.61, 13.985, 14.36, 14.735, 15.11, 15.485, 15.985, 16.235, 16.485, 16.735, 16.985, 17.36, 17.735, 18.485, 18.86, 18.985, 19.11, 19.235, 19.36, 19.485, 19.61, 19.735, 19.86, 19.985, 20.11, 20.235, 20.36, 20.485, 20.61, 20.735, 20.828, 20.922, 21.016, 21.11, 21.203, 21.297, 21.391, 21.485, 21.61, 21.735, 21.86, 21.985, 22.11, 22.235, 22.36, 22.485, 22.61, 22.735, 22.86, 22.985, 23.078, 23.172, 23.266, 23.36, 23.453, 23.547, 23.641, 23.735, 23.828, 23.922, 24.016, 24.11, 24.203, 24.297, 24.391, 24.285 };

    // Use this for initialization
    private void Start()
    {
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

        _modulus = 10;
        GenerateGrid(_rnd.Next(0, _modulus));
    }

    private void GenerateGrid(int initialFinalValue)
    {
        for (int i = 0; i < _grid.Length; i++)
        {
            int x = i;
            _grid[i] = Instantiate(_referencePoint, _module.transform);
            _grid[i].GetComponent<Selectable>().SetValues(i, _isRalpMode ? _rnd.Next(7, 17) : _rnd.Next(5, 8), _grid.Length, initialFinalValue, _modulus);
            _grid[i].GetComponent<Selectable>().SetText(initialFinalValue.ToString());
            _grid[i].GetComponent<Selectable>().SetColour(i, false, _isLogging);

            _grid[i].transform.localPosition += new Vector3(0.0315f * (i % 4), 0, -0.0315f * (i / 4));
            _grid[i].Parent = _module.GetComponent<KMSelectable>();

            _grid[i].GetComponent<MeshRenderer>().material = _colours[(i ^ (i >> 2)) & 1];
            _module.GetComponent<KMSelectable>().Children[x] = _grid[x];
        }
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
            for (int i = 0; i < 3; i++)
                grid = grid.Insert(4 * (i + 1) + i, "|");
            Log("The initial state is: " + grid);
            for (int i = 0; i < _grid.Length; i++)
            {
                string j = _grid[i].GetComponent<Selectable>().GetOffsets().Join("");
                for (int k = 0; k < 3; k++)
                    j = j.Insert(4 * (k + 1) + k, "|");
                Log("The matrix for tile " + "ABCD"[i % 4].ToString() + "1234"[i / 4].ToString() + " is: " + j);
            }
        }
        string log = _twitchPlaysAutosolver.Join("");
        for (int i = 0; i < 3; i++)
            log = log.Insert(4 * (i + 1) + i, "|");
        Log("The solution grid is: " + log);
    }

    private void PressLogKey()
    {
        if (_isModuleSolved)
            return;

        _isLogging = true;
        _moduleRender.color = new Color32(192, 0, 0, 255);
        Enumerable.Range(0, _grid.Length).Where(x => ((x ^ (x >> 2)) & 1) == 1).ForEach(x => _grid[x].GetComponent<Selectable>().SetTileColour(x, 3));
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
        StartCoroutine(SolveAnimation());
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
                    _grid[i].GetComponent<Selectable>().SetTileColour(i, _rnd.Next(0, 2) == 1 ? (!_isLogging ? 2 : 4) : (_isLogging ? (((i ^ (i >> 2)) & 1) == 1 ? 3 : 0) : ((i ^ (i >> 2) ^ 0) & 1))); // _grid[i].GetComponent<Selectable>().SetSolvedColour(i, structure[part][i] == '1');
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
