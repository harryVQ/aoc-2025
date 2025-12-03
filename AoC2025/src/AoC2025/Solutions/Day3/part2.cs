using System;
using System.Collections.Generic;

var lines = new List<string>();
string? line;
while ((line = Console.ReadLine()) != null)
{
    if (line.Length == 0) continue;
    lines.Add(line);
}

long total = 0;
foreach (var s in lines)
{
    string best = MaxSubsequence(s, 12);
    total += long.Parse(best);
}
Console.WriteLine(total);

static string MaxSubsequence(string s, int k)
{
    int n = s.Length;
    if (n <= k) return s;
    var stack = new List<char>();
    for (int i = 0; i < n; i++)
    {
        char c = s[i];
        while (stack.Count > 0 &&
               stack[stack.Count - 1] < c &&
               stack.Count - 1 + (n - i) >= k)
        {
            stack.RemoveAt(stack.Count - 1);
        }
        if (stack.Count < k)
        {
            stack.Add(c);
        }
    }
    return new string(stack.ToArray());
}
