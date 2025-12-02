using System;

int position = 50;
int count = 0;

while (true)
{
    string? line = Console.ReadLine();
    if (line == null) break;
    line = line.Trim();
    if (line.Length == 0) continue;

    char dir = line[0];
    if (!int.TryParse(line.Substring(1), out int value)) continue;

    if (dir == 'L')
    {
        position = (position - value) % 100;
        if (position < 0) position += 100;
    }
    else if (dir == 'R')
    {
        position = (position + value) % 100;
    }
    else
    {
        continue;
    }

    if (position == 0) count++;
}

Console.WriteLine(count);