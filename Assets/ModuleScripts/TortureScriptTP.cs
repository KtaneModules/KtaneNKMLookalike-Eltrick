using System;
using KeepCoding;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class TortureScriptTP : TPScript<TortureScript>
{
    public override IEnumerator ForceSolve()
    {
        Module._isNotEnoughTime = true;
        yield return null;
        for (int i = 0; i < Module._grid.Length; i++)
            while (Module._twitchPlaysAutosolver[i] != 0)
            {
                Module._grid[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
    }

    public override IEnumerator Process(string command)
    {
        command = command.ToUpperInvariant();

        yield return null;
        if (Regex.IsMatch(command, "[0-9]{16}"))
            for (int i = 0; i < Module._grid.Length; i++)
                for (int j = 0; j < int.Parse(command[i].ToString()); j++)
                {
                    Module._grid[i].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
        else if (Regex.IsMatch(command, @"[A-D][1-4]\s?"))
        {
            string[] presses = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < presses.Length; i++)
            {
                yield return null;
                Module._grid[presses[i][0] - 'A' + (presses[i][1] - '1') * 4].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        else
            yield return "sendtochaterror The module did not detect any valid command formats. Check your command.";
    }
}
