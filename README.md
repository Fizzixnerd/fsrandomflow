# fsrandomflow

## Introduction

**fsrandomflow** allows random computations to be expressed without explicitly sampling a random generator until the end. This means that you can seed the generator once you are ready to run the full computation.

The provided random computations are built to be entirely deterministic on the original seed. By combining the original generators (and obviously, avoiding IO), any random variable a user creates should have this same deterministic property.

Unlike ```System.Random```, a random variable can only produce one type of value, but the type is completely generic and operations like ```map``` are supported.

A workflow builder is provided to give lightweight syntax for these combinations, as well as operators for transforming values in the piped style.

## Using with MonoDevelop

To use this with MonoDevelop, you will need to explicitly add a package reference the FSharp.Core package on nuget in the main project.

## Example

Say you want to roll an attribute score in D&D 5th edition, and your DM is very generous and wants everyone to have high stats. She tells everyone that if they roll a 1, they are allowed to reroll, and they roll a fourth die, which allows them to drop their lowest roll entirely. **fsrandomflow** lets this be expressed as an ```RVar<int>```:

```F#
///Roll 4 d6, reroll the 1s, drop the lowest
let dnd5eAttrRollHouseRules = randomly {
        //A six-sided die
        let d6 = RVar.UniformInt(1,6)
        //A six-sided die that is rerolled if it lands on 1
        let d6rerolling1 = randomly {
                //Roll a 6 sided die
                let! result = d6
                //If it didn't land on 1, return the result
                if result > 1 then return result
                //Else, roll another d6 and return the result
                else return! d6
            }
        //Roll 4 six-sided die, rerolling the 1s
        let! initialRolls = RVar.take 4 d6rerolling1
        //Drop the lowest die
        let threeBestRolls = Array.sort initialRolls |> Seq.tail
        //Return the sum
        return Seq.sum threeBestRolls
    }
```

# PSU Summer FOSS Project Week 3 Report

While much of the functionality is implemented (with the expression builder basically finished) 
and with the "canon" project providing a good demonstration of the library in about 150 lines, 
there are still several obstacles in the way of an actual binary release.

## Testing

It's not clear how to approach a random generator library using unit testing (either black box
or white box). Almost all of the testing was done by running the code in the interpreter. This 
is not going to improve much, so the focus should perhaps instead be on providing histograms, and
heuristics that should tend to 0 such as calculations of sample variance. Addressing this lack
of testing should be a big priority going forward.

Some specific tests should be added after the version 0 release to test that specific constructed
values remain the same as the library continues to change and evolve. These could then serve as
canaries if seeds get broken by a change, or if a new major version is needed.

## .NET Target

On creation, .NET 4.5 was targeted as a compromise between old and new versions. This was a mistake.
The project should be re-created from scratch as a .NET 4.6.1 project, as this seems to be easier
to deploy, especially cross-platform.

## API Stability

Somewhat surprisingly, the seed was broken very infrequently during initial development. This bodes
well for the future. Once this library is released and versioned, anything that would cause the same
seeds to return different results when run with different versions of the library should result in
a major version change. Adding dynamic creation is anticipated to be one such change. Given the track
record of breaking the seed so far, major versions should be rare.

## Missing functionality

Several common statistical distributions are still missing. These should ideally be added before 
a version 0 release.

The ziggarut algorithm is implemented, but there is no way to discover an actual ziggarut to use
with it for some PDF, and it is not exposed to users anyway. If exposed, especially with a way 
to randomly build the ziggarut using an evolutionary algorithm like harmony search, users could
add specific ditributions, such as a stellar mass distribution, to the library. It could also 
offer an easy answer to adding some of the remaining distributions that were planned 
(Weibull, Chisquare, & Poisson).

Dynamic creation should either be finished or scrapped as a goal for a later major version. Adding
dynamic creation will break the seeds, so it should be done before any official release or postponed 
to some indefinitely far out major version change.

There are still probably some functions missing. Writing more demos and analyzing the existing ones
could yield insights of additional functions to add to the RVar module to easy programming with RVars.

Functions for creating coherent noise and for solving problems using stochastic metaheuristics should
be added in additional modules within the namespace, perhaps along with some functions to sample 
from higher dimensional arrays (like picking a random row or column from a matrix). These could be 
postponed indefinitely because they would not break the seed.
Additional parallel primitives should perhaps be added to allow users to fine-tune chunking to
limit the amount of branching of the random number generator.

## Documentation & Names

Much of the library is now documented and there is also now a well-organized example project
acting as a demo. There is still a bit of work left to be done for some of the functions in 
the RVar module. Documentation for the twister and RVarAST modules is not a priority at the moment.
The RVar module should be fully documented before a version 0 release.

Many of the names chosen at the moment are a bit clunky and are also written with tab-completion
in mind rather than the more capable IntelliSense (hence choices like "oneOfWeighted" rather than
the more sensible "weightedChoice"). While not a pressing concern, any renaming should be done 
before any official release.

Finally, RVar should perhaps be reorganized in a more structured way, at least as far as possible 
without the need for forward declarations.