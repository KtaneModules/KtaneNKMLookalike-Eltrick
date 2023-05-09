using System;
using KeepCoding;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class TortureScriptTP : TPScript<TortureScript>
{
    private string TwitchHelpMessage = "<!{0} <16 digits>> to construct the array for which to press each position in reading order that number of times. <!{0} [coordinate]> to press that specific coordinate, with the column being specified by letters, with the column after Z being AA, and the row being specified by a number. The first command type is not chainable, nor can you use both types of commands in one, however the second command type can have multiple coordinates separated by spaces. <!{0} <16 numbers separated by spaces>> to also construct a more precise array for which to press the buttons, similar to the first command. <!{0} (resize|setsize) [height] [width]> to resize the grid, and reset all values. However, width * height must be at least 5. This command is disallowed in a mission.";
    private int _offset = 55; // Offset multiplier

    public override IEnumerator ForceSolve()
    {
        Module.IsLogging = true;
        Module.IsAutosolve = true;

        Module.PressLogKey();

        yield return null;
        for (int i = 0; i < Module._grid.Length; i++)
            while (Module.TwitchPlaysAutosolver[i] != 0)
            {
                Module._grid[i].Button.OnInteract();
                yield return null;
            }
        while (Module.IsModuleSolved)
            yield return true;
    }

    public override IEnumerator Process(string command)
    {
        if (Module.IsModuleSolved)
            yield break;

        command = command.ToUpperInvariant();

        yield return null;
        if (Regex.IsMatch(command, "[0-9]{" + Module.GridSize.ToString() + "}") && Module.Modulus <= 10)
            for (int i = 0; i < Module._grid.Length; i++)
                for (int j = 0; j < int.Parse(command[i].ToString()); j++)
                {
                    Module._grid[i].Button.OnInteract();
                    yield return new WaitForSeconds(.1f / Mathf.Pow(Module.GridSize / 16f, 1.5f));
                }
        else if (Regex.IsMatch(command, "([0-9]+\\s){" + (Module.GridSize - 1).ToString() + "}[0-9]+"))
        {
            string[] numbers = command.Split(" ");
            float time = 0;
            int p = 0;

            for(int i = 0; i < Module._grid.Length; i++)
            {
                int o;
                if (!int.TryParse(numbers[i], out o))
                    continue;

                while(true)
                {
                    if(time * (Mathf.Pow(Module.GridSize / 16f, 1.5f) * Module.Modulus) <= p + 1)
                    {
                        yield return null;
                        if (p > 0)
                        {
                            Module._grid[i].ApplyChanges(p - 1);
                            Module._grid[i].Button.OnInteract();
                        }
                        p = 0;
                        time += Time.deltaTime;
                        if (o <= 0)
                            break;
                        continue;
                    }

                    while(o > 0 && time * (Mathf.Pow(Module.GridSize / 16f, 1.5f) * Module.Modulus) > p + 1)
                    {
                        p++;
                        o--;
                    }
                    time = 0;
                }
            }
        }
        else if (Regex.IsMatch(command, @"(([A-Z])+([0-9])+\s*)+"))
        {
            string[] presses = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < presses.Length; i++)
            {
                string column = Regex.Match(presses[i], "[A-Z]+").Value;
                int c = 0;

                for (int j = 0; j < column.Length; j++)
                    c += (column[j] - 'A' + (j == column.Length - 1 ? 0 : 1)) * (int)Mathf.Pow(26, column.Length - 1 - j);

                int r = int.Parse(Regex.Match(presses[i], "[0-9]+").Value);

                yield return null;
                Module._grid[c + (r - 1) * Module.Width].Button.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        else if(Regex.IsMatch(command, @"(RESIZE|SETSIZE)\s+[0-9]+\s+[0-9]+") && !Module.IsMission)
        {
            var match = Regex.Match(command, @"(RESIZE|SETSIZE)\s+([0-9]+)\s+([0-9]+)");

            int h = int.Parse(match.Groups[2].Value);
            int w = int.Parse(match.Groups[3].Value);

            if (h * w >= 5)
            {
                Module.Height = int.Parse(match.Groups[2].Value);
                Module.Width = int.Parse(match.Groups[3].Value);

                Module.MinAffected = Mathf.Clamp(Module.Settings.MinAffected, Module.GridSize / 3, Module.GridSize);
                Module.MaxAffected = Mathf.Clamp(Module.Settings.MaxAffected, Mathf.Max((int)(Module.GridSize / 1.5f), Module.MinAffected), Module.GridSize);

                Module.GenerateGrid(Module.Modulus);
            }
            else
                yield return "sendtochaterror The grid size must be at least 5 tiles.";
        }
        else
            yield return "sendtochaterror The module did not detect any valid command formats. Check your command.";

        yield return new WaitForSeconds(.125f);
        if (Module.IsModuleSolved)
            yield return AwardPointsOnSolve(GetOffsetScore(_offset, Module.GridSize));
    }

    private static int GetOffsetScore(int offset, int gridSize)
    {
        return Mathf.RoundToInt((Mathf.Pow(gridSize / 16f, 3) - 1) * offset);
    }

    public void UpdateHelpMessage(int gridSize)
    {
        Help = "<!{0} <" + gridSize.ToString() + " digits>> to construct the array for which to press each position in reading order that number of times. <!{0} [coordinate]> to press that specific coordinate, with the column being specified by letters, with the column after Z being AA, and the row being specified by a number. The first command type is not chainable, nor can you use both types of commands in one, however the second command type can have multiple coordinates separated by spaces. <!{0} (resize|setsize) [height] [width]> to resize the grid, and reset all values. However, width * height must be at least 5. This command is disallowed in a mission.";
        TwitchHelpMessage = Help;
    }
}
