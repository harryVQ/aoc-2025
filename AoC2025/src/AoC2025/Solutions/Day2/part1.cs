using System;

class Program
{
    static void Main()
    {
        // Read entire input (may span multiple lines) into a single string.
        string allInput = string.Empty;
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            allInput += line;
        }

        // Split by commas and optional whitespace to get each "start-end" token.
        string[] tokens = allInput.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        long sum = 0;

        foreach (string token in tokens)
        {
            string[] parts = token.Split('-');
            if (parts.Length != 2) continue;
            if (!long.TryParse(parts[0], out long start)) continue;
            if (!long.TryParse(parts[1], out long end)) continue;

            for (long i = start; i <= end; i++)
            {
                string s = i.ToString();
                int len = s.Length;
                if (len % 2 != 0) continue; // Must have even number of digits
                string firstHalf = s.Substring(0, len / 2);
                string secondHalf = s.Substring(len / 2);
                if (firstHalf == secondHalf)
                {
                    sum += i;
                }
            }
        }

        Console.WriteLine(sum);
    }
}
