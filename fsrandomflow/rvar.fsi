//Copyright 2017 Barend Venter
//This code is licensed under the MIT license, see LICENSE

namespace fsrandomflow
    open System.Collections.Generic

    /// A random variable of a particular type. Don't implement this interface, use the RVar combinators instead.
    [<Interface>]
    type RVar<'T> =
        ///Using a stream of random values, generate an example value of type 'T
        abstract member sample : IEnumerator<int> -> 'T
    module RVar =
        ///This random variable exposes the underlying stream of uniformly distributed positive random integers
        val StdUniform : RVar<int>

        ///Map a function over a random variable, yielding a new random variable
        val map : ('T -> 'U) -> RVar<'T> -> RVar<'U>

        ///Apply a random argument to a random function (applicative interface).
        val apply : RVar<'T> -> RVar<'T -> 'U> -> RVar<'U>

        ///Pipe random inputs into a function producing random outputs (monadic interface).
        val concatMap : ('T -> RVar<'U>) -> RVar<'T> -> RVar<'U>

        ///Sample another random variable a given number of times, rather than just once
        val take : int -> RVar<'T> -> RVar<'T array>

        ///Create a random variable that samples two random variables, one after the other
        val zip : RVar<'T> -> RVar<'U> -> RVar<'T * 'U>

        ///Create a random variable that samples three random variables, one after the other
        val zip3 : RVar<'T> -> RVar<'U> -> RVar<'V> -> RVar<'T * 'U * 'V>

        ///Create a random variable that ignores the underlying stream and simply returns a constant (unit function).
        val constant : 'T -> RVar<'T>

        ///For some sequence of random computations, make a random computation that runs the sequence in order
        val sequence : RVar<'T> seq -> RVar<'T seq>

        ///Perform a single coin flip (a "Bernoulli trial": this is the Bernoulli distribution)
        val CoinFlip : RVar<bool>

        ///Randomly flip the sign of an integer
        val RandomlySignedInt : int -> RVar<int>

        ///Randomly flip the sign of a double
        val RandomlySignedDouble : float -> RVar<float>

        ///Randomly flip the sign of a float
        val RandomlySignedFloat : float32 -> RVar<float32>

        ///Randomly gets a positive number (including 0) less than n
        val UniformZeroAndUpBelow : int -> RVar<int>

        ///Randomly gets a positive number inbetween the two given bounds, inclusive
        val UniformInt : int * int -> RVar<int>