# Day 3 Part 2: Part Two

## Solution Summary
The program reads each line of digits, selects the largest possible 12‑digit subsequence preserving order (using a greedy stack algorithm), sums those numbers as 64‑bit integers, and prints the total.

## Algorithm Explanation
For each line we need the maximum integer formed by keeping exactly 12 digits in their original order. The classic “remove‑k digits” greedy algorithm works: iterate through the digits, and while the current digit is larger than the last chosen digit and we still have enough remaining digits to reach 12, pop the last digit. Then push the current digit if we haven't yet chosen 12. The resulting stack is the optimal subsequence. Each subsequence is parsed to a long and added to a running total. The algorithm runs in linear time per line and uses constant extra space besides the stack.

## Sample Output
```
3121910778619
```

## Real Answer
```
172787336861064
```

## Performance
- Code Generation: 59.92s
- Code Runtime: 260.00ms
- Input Tokens: 12378
- Output Tokens: 1930