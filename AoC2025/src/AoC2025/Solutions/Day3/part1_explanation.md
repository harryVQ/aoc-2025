# Day 3 Part 1: Day 3: Lobby

## Solution Summary
The program reads each line of digits, finds the largest two-digit number that can be formed by picking two digits in order, sums these maxima over all lines, and outputs the total.

## Algorithm Explanation
For each line, a double loop examines all pairs of indices i<j, forming the two-digit number by concatenating the digits at those positions. The maximum for that line is tracked and added to a running total. This guarantees the maximum possible joltage for each bank is used, and the sum is the required answer.

## Sample Output
```
357
```

## Real Answer
```
17359
```

## Performance
- Code Generation: 29.07s
- Code Runtime: 240.00ms
- Input Tokens: 6973
- Output Tokens: 943