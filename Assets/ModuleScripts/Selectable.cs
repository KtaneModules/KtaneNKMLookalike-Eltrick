using KeepCoding;
using System.Linq;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public TortureScript Parent;
    public KMSelectable Button { get; private set; }
    public TextMesh ButtonText;
    public MeshRenderer ButtonMesh { get; private set; }

    private int _index;
    private int _value;
    private int _modulus;
    private int _offsetSize;
    private int[] _offsets;

    public void SetValues(int index, int offsetSize, int gridSize, int value, int modulus)
    {
        _index = index;
        _offsets = new int[gridSize];
        _offsetSize = offsetSize;
        _value = value;
        _modulus = modulus;
        Activate();
    }

    private void Activate()
    {
        Button = GetComponent<KMSelectable>();
        ButtonMesh = GetComponent<MeshRenderer>();
        Button.Assign(onInteract: () => { ButtonPress(); });
        SetOffsets();
    }

    private void ButtonPress()
    {
        if (Parent.IsModuleSolved)
            return;
        Parent.PlaySound(Parent.transform, false, Parent._sounds[0]);
        ApplyChanges();
        Parent.TwitchPlaysAutosolver[_index] = (Parent.TwitchPlaysAutosolver[_index] + 9) % 10;
        CheckSolve();
    }

    private void SetOffsets()
    {
        Enumerable.Range(0, _offsets.Length).ToList().Shuffle().Take(_offsetSize).ForEach(x => _offsets[x] = Random.Range(1, _modulus));
    }

    public void ApplyChanges(int t = 1)
    {
        if (Parent.IsModuleSolved)
            return;

        for (int i = 0; i < _offsets.Length; i++)
        {
            Parent._grid[i].Offset((int)((long)_offsets[i] * t % _modulus));
            SetColour(i, _offsets[i] != 0, Parent.IsLogging);
        }
    }

    public void SetColour(int index, bool changed, bool logging)
    {
        if (!changed)
            Parent._grid[index].ButtonText.color = (((index % Parent.Width) ^ (index / Parent.Width)) & 1) == 1 ? new Color32(0, 0, 0, 255) : (logging ? new Color32(192, 0, 0, 255) : new Color32(0, 192, 255, 255));
        else
            Parent._grid[index].ButtonText.color = new Color32(255, 255, 255, 255);
    }

    public void SetTileColour(int index, int colour)
    {
        ButtonText.color = new Color(0, 0, 0);
        Parent._grid[index].ButtonMesh.material = Parent._colours[colour];
        if (colour == 3)
            Enumerable.Range(0, Parent._grid.Length).Where(x => (((x % Parent.Width) ^ (x / Parent.Width)) & 1) == 0).ForEach(x => Parent._grid[x].ButtonText.color = new Color32(192, 0, 0, 255));
    }

    public void SetText(string x)
    {
        ButtonText.text = x;
        ButtonText.transform.localScale = new Vector3(0.06f / Mathf.Max(ButtonText.text.Length, 1), 0.06f, 0.06f);
    }

    public void Offset(int x)
    {
        _value = (int)((_value + (long)x) % _modulus);
        SetText(_value.ToString());
    }

    public void CheckSolve()
    {
        if (Parent.IsModuleSolved)
            return;

        int check = Parent._grid.GroupBy(i => i.GetValue()).OrderByDescending(i => i.Count()).First().ToArray()[0].GetValue();
        if (Parent._grid.All(x => x.GetValue() == check))
            Parent.SolveModule();
    }

    public int GetValue()
    {
        return _value;
    }

    public int[] GetOffsets()
    {
        return _offsets;
    }

    public int GetIndex()
    {
        return _index;
    }
}