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

        ///The result samples in windows of the given size, rather than individually
        val take : int -> RVar<'T> -> RVar<'T array>

        ///Create a random variable that samples two random variables, one after the other
        val zip : RVar<'T> -> RVar<'U> -> RVar<'T * 'U>

        ///Create a random variable that ignores the underlying stream and simply returns a constant (unit function).
        val constant : 'T -> RVar<'T>