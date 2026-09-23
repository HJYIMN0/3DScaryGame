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
Now you're in this story.
You're called to make a choice.
The outcome is irrelevant

+ [Drink the coffee]
You chose to drink the coffee.
A small prize for a small choice.
    -> Reflection

+ [Don't drink the coffee]
A chance you don't like coffee.
Is it really something you choose?
    -> Reflection

=== Reflection ===
You made your choice.
-> END

=== Day100 ===
I already drank my coffee...
-> END