VAR Day = 0

{Day:
- 0: -> Day0
- 1: -> Day1
- 2: -> Day2
- 3: -> Day3
- 100: -> Day100
}

=== Day0 ===
Bathroom text
-> END

=== Day1 ===
Bathroom text
-> END

=== Day2 ===
Bathroom text
-> END

=== Day3 ===
I’m cleaning the place that cleans me.
Am I using a tool?
Am I the tool?
A cleaning tool.
A cleaning tool that gets dirty.
And I clean the dirty tool with a tool that gets dirty.
So I clean it.
And it gets dirtier.
-> Clean01

=== Clean01 ===
+ [I clean it]
And dirtier.
-> Clean02

=== Clean02 ===
+ [I have to clean]
And dirtier.
-> Clean03

=== Clean03 ===
+ [Cleaning keeps the dirt away]
And dirtier.
-> Clean04

=== Clean04 ===
+ [So I clean]
And dirtier.
-> Clean05

=== Clean05 ===
+ [And I keep cleaning]
And dirtier.
-> Clean06

=== Clean06 ===
+ [And cleaning]
And dirtier.
-> Clean07

=== Clean07 ===
+ [Or else i get dirty]
And dirtier.
-> Clean08

=== Clean08 ===
+ [I don't want to get dirty]
And dirtier.
-> Clean09

=== Clean09 ===
+ [I don't want it]
And dirtier.
-> Reflection

+ [Stop cleaning]
This isn't a choice.
-> Reflection

=== Reflection ===
So I clean it.

I don't want to get dirty again.

-> END

=== Day100 ===
I already drank my coffee...
-> END
