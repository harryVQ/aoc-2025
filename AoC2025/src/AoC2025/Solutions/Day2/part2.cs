using System;
using System.Linq;

string line = Console.ReadLine();
if (string.IsNullOrWhiteSpace(line))
{
    return;
}

long total = 0;
var ranges = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
foreach (var range in ranges)
{
    var parts = range.Split('-', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2) continue;
    if (!long.TryParse(parts[0], out long start)) continue;
    if (!long.TryParse(parts[1], out long end)) continue;
    for (long id = start; id <= end; id++)
    {
        string s = id.ToString();
        if (IsRepeated(s))
        {
            total += id;
        }
    }
}
Console.WriteLine(total);

bool IsRepeated(string s)
{
    int len = s.Length;
    for (int l = 1; l <= len / 2; l++)
    {
        if (len % l != 0) continue;
        string pattern = s.Substring(0, l);
        bool ok = true;
        for (int pos = l; pos < len; pos += l)
        {
            if (!s.Substring(pos, l).Equals(pattern))
            {
                ok = false;
                break;
            }
        }
        if (ok) return true;
    }
    return false;
}
