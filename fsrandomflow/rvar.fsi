//Copyright 2017 Barend Venter
//This code is licensed under the MIT license, see LICENSE

namespace fsrandomflow
    open System.Collections.Generic

//    /// A random variable of a particular type. Don't implement this interface, use the RVar combinators instead.
//    [<Interface>]
//    type RVar<'T> =
//        ///Using a stream of random values, generate an example value of type 'T
//        abstract member sample : IEnumerator<int> -> 'T

    module RVar =
        val randomly : IRandomlyBuilder

        ///This random variable exposes the underlying stream of uniformly distributed positive random integers
        val StdUniform : RVar<int>

        ///Run a random variable using a given seed
        val runrvar : int -> RVar<'T> -> 'T

        ///Run a random variable using the current time from IO
        val runrvarIO : RVar<'T> -> 'T

        ///Map a function over a random variable, yielding a new random variable
        val map : ('T -> 'U) -> RVar<'T> -> RVar<'U>

        ///Apply a random argument to a random function (applicative interface).
        val apply : RVar<'T> -> RVar<'T -> 'U> -> RVar<'U>

        ///Pipe random inputs into a function producing random outputs (monadic interface).
        val concatMap : ('T -> RVar<'U>) -> RVar<'T> -> RVar<'U>

        ///Sample another random variable a given number of times, rather than just once
        val take : int -> RVar<'T> -> RVar<'T []>

        ///Sample a random variable a given number of times in parallel
        val takeParallel : int -> RVar<'T> -> RVar<'T []>

        ///Create a random variable that samples another random variables infinite times
        val repeat : RVar<'T> -> RVar<'T seq>

        ///Create a random variable that samples two random variables, one after the other
        val zip : RVar<'T> -> RVar<'U> -> RVar<'T * 'U>

        ///Create a random variable that samples three random variables, one after the other
        val zip3 : RVar<'T> -> RVar<'U> -> RVar<'V> -> RVar<'T * 'U * 'V>

        ///Create a random variable that ignores the underlying stream and simply returns a constant (unit function).
        val constant : 'T -> RVar<'T>

        ///For some sequence of random computations, make a random computation that runs the sequence in order
        val sequence : RVar<'T> seq -> RVar<'T seq>

        ///For a finite input of random variables, sample them all in parallel and return the results in an array
        val sequenceParallel : RVar<'T> seq -> RVar<'T []>

        ///Removes values that fail the given test from the random variable. If you remove all values, you will loop infinitely
        val filter : ('T -> bool) -> RVar<'T> -> RVar<'T>

        ///Removes values that fail a test from the random variable. The test is itself allowed to be randomized. If you remove all 
        /// values, you will loop infinitely.
        val filterRandomly : ('T -> RVar<bool>) -> RVar<'T> -> RVar<'T>

        ///Perform a single coin flip (a "Bernoulli trial": this is the Bernoulli distribution)
        val CoinFlip : RVar<bool>

        ///Randomly flip the sign of an integer
        val RandomlySignedInt : int -> RVar<int>

        ///Randomly flip the sign of a double
        val RandomlySignedDouble : float -> RVar<float>

        ///Randomly flip the sign of a float
        val RandomlySignedFloat : float32 -> RVar<float32>

        ///A non-negative integer less than n
        val UniformZeroAndUpBelow : int -> RVar<int>

        ///An integer inbetween the two given bounds, inclusive
        val UniformInt : int * int -> RVar<int>

        ///Randomly gets a double between 0 and 1, inclusive
        val UniformZeroToOne : RVar<float>

        ///Randomly gets a double within an interval, allowing for either bound to be open or closed
        val UniformInterval : float * float * bool * bool -> RVar<float>

        ///Gets a Double bound inside a given closed interval
        val UniformIntervalClosed : float * float -> RVar<float>

        ///Gets a double bound inside a given open interval
        val UniformIntervalOpen : float * float -> RVar<float>

        ///A boolean, the chance of it being true is the given probability
        val probability : float -> RVar<bool>

        ///Randomly picks one possibility out many
        val oneOf : seq<'T> -> RVar<'T>
        
        ///Randomly picks one possibility out of many, with given probabilities (normalized to 1)
        val oneOfWeighted : seq<float * 'T> -> RVar<'T>

        ///Shuffles a finite input. A new array is allocated to hold the output.
        val shuffle : seq<'T> -> RVar<'T []>

        ///Randomly gets a double from the standard normal distribution
        val StandardNormal : RVar<float>

        ///Randomly gets a double from a normal distribution with the given mean and standard deviation
        val Normal : (float * float) -> RVar<float>
