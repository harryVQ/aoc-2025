using System;

class Program
{
    static void Main()
    {
        long total = 0;
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length < 2)
                continue;

            int max = 0;
            int n = line.Length;
            for (int i = 0; i < n - 1; i++)
            {
                int d1 = line[i] - '0';
                for (int j = i + 1; j < n; j++)
                {
                    int d2 = line[j] - '0';
                    int value = d1 * 10 + d2;
                    if (value > max)
                        max = value;
                }
            }
            total += max;
        }
        Console.WriteLine(total);
    }
}