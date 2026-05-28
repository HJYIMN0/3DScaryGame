VAR Day = 0

{Day:
- 0: -> Day0
- 1: -> Day1
- 2: -> Day2
- 3: -> Day3
- 100: -> Day100
}

=== Day0 ===
Text for hole on day 0
-> END

=== Day1 ===
Nothing better than coffee to start off my day
-> END

=== Day2 ===
Nothing better than coffee to start off my day
-> END

=== Day3 ===
I used to hate coffee.
In truth, I still don't like it.

+ [Drink the coffee]
    Yet I drink it every day.
    -> Reflection

+ [Don't drink the coffee]
    Today, I leave it untouched.
    -> Reflection

=== Reflection ===
Am I more than what I choose to drink?
-> END

=== Day100 ===
I already drank my coffee...
-> END