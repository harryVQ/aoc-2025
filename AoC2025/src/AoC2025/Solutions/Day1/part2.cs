using System;

int pos = 50;
int count = 0;
string line;
while ((line = Console.ReadLine()) != null)
{
    if (line.Length == 0) continue;
    char dir = line[0];
    if (!int.TryParse(line.Substring(1), out int distance)) continue;
    if (dir == 'L')
    {
        for (int i = 0; i < distance; i++)
        {
            pos = (pos - 1 + 100) % 100;
            if (pos == 0) count++;
        }
    }
    else // 'R'
    {
        for (int i = 0; i < distance; i++)
        {
            pos = (pos + 1) % 100;
            if (pos == 0) count++;
        }
    }
}
Console.WriteLine(count);