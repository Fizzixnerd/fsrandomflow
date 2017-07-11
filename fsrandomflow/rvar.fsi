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
        ///A builder for expressions of type RVar
        val randomly : IRandomlyBuilder

        ///<summary>A random integer from <c>0</c> to <c>System.Int32.MaxValue</c>.
        ///</summary>
        ///<remarks>This random variable exposes the underlying
        ///Mersenne-Twister random number generator.
        ///</remarks>
        val StdUniform : RVar<int>

        ///<summary>
        ///Run a random variable using a given seed.
        ///</summary>
        ///<param name="seed">A 32-bit integer used to seed the random number
        ///generator.</param>
        ///<param name="rvar">The random variable to be sampled with using the 
        ///given seed</param>
        ///<returns>A sample taken from the random variable, which should 
        ///correspond to the provided seed</returns>
        ///<remarks>This should be a pure function and it may be treated as one.
        ///If runrvar is not a pure function, it is a bug in the given
        ///random variable parameter <c>rvar</c>. If this random variable
        ///was built only using the given combinators and pure functions, 
        ///this is also a bug in fsrandomflow itself.</remarks>
        val runrvar : int -> RVar<'T> -> 'T

        ///<summary>Run a random variable using some random seed chosen
        ///non-deterministically.</summary>
        ///<param name="rvar">The random variable to be sampled using some 
        ///seed chosen by the system.</param>
        ///<returns>A sample taken from the random variable.</return>
        val runrvarIO : RVar<'T> -> 'T

        ///<summary>Builds a new random variable of type <c>RVar&lt;'U&gt;</c> by
        ///mapping a given function <c>'T -> 'U</c> over a random variable
        ///of type <c>RVar&lt;'T&gt;</c></summary>
        ///<param name="f">The function to be used to transform random samples 
        ///from the given <c>RVar&lt;'T&gt;</c> into random samples from the output
        ///<c>RVar&lt;'U&gt;</c>. If you care about determinism, this should be a 
        ///pure function.</param>
        ///<param name="v">A random variable that is to be sampled to produce samples
        ///using the transform.</param>
        ///<returns>A random variable that samples the image of <c>f</c> over the
        ///set of samples that can be produced by <c>v</c></returns>
        val map : ('T -> 'U) -> RVar<'T> -> RVar<'U>

        ///<summary>Builds a new random variable that applies results sampled from an 
        ///<c>RVar&lt;'T&gt;</c> to functions sampled from an <c>RVar&lt;'T -> 'U&gt;</c>.
        ///</summary>
        ///<param name="fv">The random variable that will by sampled from to get 
        ///some transform of the set of values that can be sampled from <c>vv</c>.
        ///If you care about determinism, these sampled functions should be pure.
        ///</param>
        ///<param name="vv">A random variable that is to be sampled to produce samples
        ///using some transform sampled from <c>vf</c></param>
        val apply : RVar<'T> -> RVar<'T -> 'U> -> RVar<'U>

        ///<summary>Builds a new random variable that uses random results sampled from
        ///an <c>RVar&lt;'T&gt;</c> to produce random variables of type 
        ///<c>RVar&lt;'U&gt;</c> using the provided function. Those random variables
        ///are then sampled by the same random generator.</summary>
        ///<param name="f">Given a sample of <c>'T</c>, this function should fetch 
        ///a random variable of type <c>'U</c> to be sampled from. If you care about
        ///determinism, this function should be pure.
        ///</param>
        ///<param name="v">A random variable that will be sampled for an input to
        ///the given function <c>f</c></param>
        ///<returns>A random variable which samples the results of <c>f</c> when given
        ///samples of <c>v</v>
        val concatMap : ('T -> RVar<'U>) -> RVar<'T> -> RVar<'U>

        ///<summary>Creates a random variable that, when sampled, samples
        ///another random variable a given number of times, and returns
        ///an array of the samples</summary>
        ///<param name="count">The number of times <c>v</c> will be sampled
        ///upon sampling the resulting random variable.</param>
        ///<param name="v">The random variable to sample from when building
        ///the result array</param>
        ///<returns>A random variable, that when sampled, gives a <c>count</c>
        ///length array of samples of <c>v</c></returns>
        val take : int -> RVar<'T> -> RVar<'T []>
        
        ///<summary>Creates a random variable that, when sampled, samples
        ///another random variable a given number of times in parallel,
        ///and returns an array of the samples.</summary>
        ///<param name="count">The number of times <c>v</c> will be sampled
        ///upon sampling the resulting random variable.</param>
        ///<param name="v">The random variable to sample from when building
        ///the result array.</param>
        ///<returns>A random variable, that when sampled, gives a <c>count</c>
        ///length array of samples of <c>v</c></returns>
        ///<remarks>This is ideal if <c>count</c> is 
        ///quite small but sampling <c>v</c> is expensive.
        ///Like any parallel primitive, you should check empirically
        ///that your use of it improves real time performance; it may
        ///make it worse rather than better. To preserve determinism,
        ///no attempt is made to help you with chunking.</remarks>
        val takeParallel : int -> RVar<'T> -> RVar<'T []>

        ///<summary>Creates a random variable that streams the output
        ///of another random variable with a branched random generator.
        ///Polling the enumerator samples from it with the branched
        ///generator.</summary>
        ///<param name="v">A random variable to sample from infinitely</param>
        ///<returns>An infinite stream of <c>v</c> samples.</returns>
        val repeat : RVar<'T> -> RVar<'T seq>

        ///<summary>Creates a random variable out of two random variables,
        ///which, when sampled, returns samples from both variables in a pair.
        ///</summary>
        ///<param name="v1">A random variable to sample for the first value
        ///of the pair</param>
        ///<param name="v2">A random variable to sample for the second value
        ///of the pair</param>
        ///<returns>A random variable that, when sampled, returns a pair
        ///of values, sampling from the given <c>v</c>'s</returns>
        val zip : RVar<'T> -> RVar<'U> -> RVar<'T * 'U>
        
        ///<summary>Creates a random variable out of three random variables,
        ///which, when sampled, returns samples from all three
        ///variables in a triple.
        ///</summary>
        ///<param name="v1">A random variable to sample for the first value
        ///of the pair</param>
        ///<param name="v2">A random variable to sample for the middle value
        ///of the pair</param>
        ///<param name="v3">A random variable to sample for the last value
        ///of the pair</param>
        ///<returns>A random variable that, when sampled, returns a triple
        ///of values, sampling from the given <c>v</c>'s</returns>
        val zip3 : RVar<'T> -> RVar<'U> -> RVar<'V> -> RVar<'T * 'U * 'V>

        ///<summary>Creates a random variable that, when sampled, returns
        ///one and only one value, not polling the underlying random 
        ///generator.</summary>
        ///<param name="v">The value that this random variable will return
        ///</param>
        ///<returns>A random variable that returns the same value
        ///every time that it is sampled</returns>
        val constant : 'T -> RVar<'T>

        ///<summary>Creates a random variable from a sequence of random
        ///variables, that, when sampled, samples each random variable
        ///given in order and samples from it.
        ///</summary>
        ///<param name="xs">A sequence of random variables to sample in order.
        ///The given sequence has to be finite.</param>
        ///<returns>A random variable, that, when sampled, samples a sequence
        ///of provided random variables in order, returning a new sequence.
        ///</returns>
        val sequence : RVar<'T> seq -> RVar<'T []>

        ///<summary>Creates a random variable that, when sampled, 
        ///produces an enumerator that samples each provided random
        ///variable in order using a branched random generator.
        ///</summary>
        ///<param name="xs>A sequence of random variables that will be sampled
        ///from in order using a branch of the random number generator.
        ///This sequence is allowed to be infinite.
        ///</param>
        ///<returns>A random variable that, when sampled, produces a sequence
        ///that, when polled, samples the next random value in a stream
        ///of random variables.
        ///</returns>
        val sequenceStreaming : RVar<'T> seq -> RVar<'T seq>

        ///<summary>Creates a random variable that, when sampled, 
        ///samples each of the provided random variables in parallel.
        ///</summary>
        ///<param name="xs">A collection of random variables to sample from
        ///in parallel. It must be finite.</param>
        ///<returns>A random variable that, when samples, samples many
        ///other random variables in parallel and returns the results in
        ///a new array.</returns>
        val sequenceParallel : RVar<'T> seq -> RVar<'T []>

        ///<summary>Creates a new random variable that, when sampled, 
        ///samples another random variable repeatedly until the sampled
        ///value satisfies a given predicate. If the predicate is not
        ///satisfiable on the sample space for that variable, this
        ///will loop forever when sampled.</summary>
        ///<param name="f">A predicate which any produced sample should
        ///satisfy</param>
        ///<param name="v">A random variable that will be sampled to produce
        ///candidate samples to be tested by the predicate</param>
        ///<returns>A random variable that, when sampled, samples another 
        ///random variable until the sample satisfies some predicate.
        ///</returns>
        val filter : ('T -> bool) -> RVar<'T> -> RVar<'T>

        ///<summary>Creates a new random variable that, when sampled, 
        ///samples another random variable repeatedly until the sampled
        ///value satisfies a given predicate. The predicate is itself
        ///allowed to use randomness to determine if the value satisfies
        ///it or not. If the predicate is not
        ///satisfiable on the sample space for that variable, this
        ///will loop forever when sampled.</summary>
        ///<param name="f">A randomized predicate which any produced
        ///satisfied successfully</param>
        ///<param name="v">A random variable that will be sampled to produce
        ///candidate samples to be tested by the predicate</param>
        ///<returns>A random variable that, when sampled, samples another 
        ///random variable until the sample satisfies some randomized
        ///predicate.
        ///</returns>
        val filterRandomly : ('T -> RVar<bool>) -> RVar<'T> -> RVar<'T>

        ///<summary>Perform a single coin flip.</summary>
        ///<remarks>This is the Bernoulli distribution.</remarks>
        val CoinFlip : RVar<bool>

        ///<summary>A random variable that, when sampled, randomly flips
        ///the sign of an integer and returns the result. The chance
        ///of either result is equal.</summary>
        ///<param name="v">An integer.</param>
        ///<returns>A random variable which returns a constant
        ///int with a random sign when sampled.</returns>
        val RandomlySignedInt : int -> RVar<int>
        
        ///<summary>A random variable that, when sampled, randomly flips
        ///the sign of a double and returns the result. The chance
        ///of either result is equal.</summary>
        ///<param name="v">A double.</param>
        ///<returns>A random variable which returns a constant
        ///double with a random sign when sampled.</returns>
        val RandomlySignedDouble : float -> RVar<float>

        ///<summary>A random variable that, when sampled, randomly flips
        ///the sign of a double and returns the result. The chance
        ///of either result is equal.</summary>
        ///<param name="v">A double.</param>
        ///<returns>A random variable which returns a constant
        ///double with a random sign when sampled.</returns>
        val RandomlySignedFloat : float32 -> RVar<float32>

        ///<summary>A random variable that, when sampled, yields a
        ///non-negative integer less than <c>n</c>.</summary>
        ///<param name="v">An integer exclusive upper bound</param>
        ///<returns>A random variable that returns an integer from
        ///0 to some number.</returns>
        ///<remarks>The underlying generator does some minor extra work
        ///to ensure that you are actually sampling from a uniform
        ///distribution, which the remainder function doesn't 
        ///achieve alone if the upper bound is not a power of two.
        ///The potential deviation from a uniform distribution
        ///increases with the bound in an inverse hyperbolic fashion;
        ///for small bounds or powers of two you might prefer to just use
        ///<c>StdUniform</c> and use the remainder function
        ///to put it into the desired range.</remarks>
        val UniformZeroAndUpBelow : int -> RVar<int>

        ///<summary>A random variable that, when sampled, yields an
        ///integer in between the two given bounds, inclusive. All integers
        ///within these bounds have an equal chance of being selected.
        ///The bounds can be given in any order.
        ///</summary>
        ///<param name=n1>A boundary for a closed interval</param>
        ///<param name=n2>A boundary for a closed interval</param>
        ///<returns>A random variable that, when sampled, returns an integer
        ///bounded in a certain range</returns>
        val UniformInt : int * int -> RVar<int>

        ///A random variable that, when sampled, yields a double between 0 and 1,
        ///inclusive
        val UniformZeroToOne : RVar<float>

        ///A random variable that, when sampled, yields a double  0 and 1,
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

        ///<summary>Create a random variable that samples from some number of other random variables with equal chance</summary>
        ///<param name="xs">The random variable to potentially sample from</param>
        ///<returns>A random variable that, when sampled, samples one of a group of random variables with equal chance</returns>
        val union : RVar<'T> seq -> RVar<'T>

        val unionWeighted : (float * RVar<'T>) seq -> RVar<'T>