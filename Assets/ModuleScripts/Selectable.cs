using KeepCoding;
using System.Linq;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public TortureScript Parent;
    public KMSelectable Button { get; private set; }
    public TextMesh ButtonText;

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
        Button.Assign(onInteract: () => { ButtonPress(); });
        SetOffsets();
    }

    private void ButtonPress()
    {
        if (Parent.IsModuleSolved)
            return;
        Parent.PlaySound(Parent.transform, false, Parent._sounds[0]);
        ApplyChanges();
        int check = Parent._grid.GroupBy(i => i.GetComponent<Selectable>().GetValue()).OrderByDescending(i => i.Count()).First().ToArray()[0].GetComponent<Selectable>().GetValue();
        Parent.TwitchPlaysAutosolver[_index] = (Parent.TwitchPlaysAutosolver[_index] + 9) % 10;
        if (Parent._grid.All(x => x.GetComponent<Selectable>().GetValue() == check))
            Parent.SolveModule();
    }

    private void SetOffsets()
    {
        Enumerable.Range(0, _offsets.Length).ToList().Shuffle().Take(_offsetSize).ForEach(x => _offsets[x] = Random.Range(1, _modulus));
    }

    public void ApplyChanges()
    {
        for (int i = 0; i < _offsets.Length; i++)
        {
            Parent._grid[i].GetComponent<Selectable>().Offset(_offsets[i]);
            SetColour(i, _offsets[i] != 0, Parent.IsLogging);
        }
    }

    public void SetColour(int index, bool changed, bool logging)
    {
        if (!changed)
            Parent._grid[index].GetComponentInChildren<TextMesh>().color = (((index % Parent.Width) ^ (index / Parent.Width)) & 1) == 1 ? new Color32(0, 0, 0, 255) : (logging ? new Color32(192, 0, 0, 255) : new Color32(0, 192, 255, 255));
        else
            Parent._grid[index].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 255, 255);
    }

    public void SetTileColour(int index, int colour)
    {
        ButtonText.color = new Color(0, 0, 0);
        Parent._grid[index].GetComponent<MeshRenderer>().material = Parent._colours[colour];
        if (colour == 3)
            Enumerable.Range(0, Parent._grid.Length).Where(x => (((x % Parent.Width) ^ (x / Parent.Width)) & 1) == 0).ForEach(x => Parent._grid[x].GetComponentInChildren<TextMesh>().color = new Color32(192, 0, 0, 255));
    }

    public void SetText(string x)
    {
        ButtonText.text = x.ToString();
    }

    public void Offset(int x)
    {
        _value += x;
        _value %= _modulus;
        SetText(_value.ToString());
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