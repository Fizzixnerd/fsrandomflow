# fsrandomflow

Copyright 2017, Barend Venter

**fsrandomflow** is an F# library that allows for random computations to be expressed without explicitly sampling a random generator until the end. This means that you can seed the generator once you are ready to run the full computation.
It was inspired by the Haskell libraries **MonadRandom** and **random-fu**. While less important in an effectful functional language like F#, the determinism and syntax sugar allowed by typed random variables provides the motivation here.

The provided random computations are built to be entirely deterministic on the original seed. By combining the original generators (and obviously, avoiding IO), any random variable a user creates should have this same deterministic property.

Unlike ```System.Random```, a random variable can only produce one type of value, but the type is completely generic and operations like ```map``` are supported.

A workflow builder is provided to give lightweight syntax for these combinations, as well as operators for transforming values in the piped style.

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

## Stability

This library is quite rough. In particular, no effort is currently made to ensure branched generators are functionally independent of one another.

Seeds will likely be broken by any future improvements. Due to this, major versions are liable to increase frequently.

## Limitations

Right now, there is virtually no test coverage for this library as I am somewhat unsure how to test it. Likely these will take the form of statistical tests run using some fixed seed.

The only theory of random number generation provided right now is MT19937. Branching is handled by changing the seed, which can potentially introduce spurious functional relationships, which means that this library is probably not appropriate for use in simulations yet. More theories should be implemented, and future versions of the library should probably use xorshift128+ by default rather than MT19937.

## Documentation

User documentation is pretty much done.

Developer documentation is still quite sparse, and a fixed interface for theories of random number generator to code against is still in the works (the MT19937 implementation would have to be retrofit for it).

A Sandcastle Help File Builder project is provided to build the HTML documentation if you want it, although its output is not hosted on the repository for the moment.