// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Runtime.CompilerServices;
using EncosyTower.Debugging;

namespace EncosyTower.Common
{
    public static partial class HashHelpers
    {
        public const int HASH_COLLISION_THRESHOLD = 100;

        // This is the maximum prime smaller than Array.MaxArrayLength
        public const int MAX_PRIME_ARRAY_LENGTH = 0x7FEFFFFD;

        public const int HashPrime = 101;

        // Table of prime numbers to use as hash table sizes.
        // A typical resize algorithm would pick the smallest prime number in this array
        // that is larger than twice the previous capacity.
        // Suppose our Hashtable currently has capacity x and enough elements are added
        // such that a resize needs to occur. Resizing first computes 2x then finds the
        // first prime in the table greater than 2x, i.e. if primes are ordered
        // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
        // Doubling is important for preserving the asymptotic complexity of the
        // hashtable operations such as add.  Having a prime guarantees that double
        // hashing does not lead to infinite loops.  IE, your hash function will be
        // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
        // We prefer the low computation costs of higher prime numbers over the increased
        // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
        private static readonly int[] s_primes = {
            1, 3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        public static ReadOnlySpan<int> Primes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_primes;
        }

        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                var limit = (int)Math.Sqrt(candidate);
                for (var divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }

        public static int GetPrime(int min)
        {
            if (min < 0)
                throw new ArgumentException("Cannot get the next prime from a negative number.");

            var primes = Primes;

            for (var i = 0; i < primes.Length; i++)
            {
                var prime = primes[i];
                if (prime >= min)
                    return prime;
            }

            //outside of our predefined table.
            //compute the hard way.
            for (var i = (min | 1); i < int.MaxValue; i += 2)
            {
                if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                    return i;
            }
            return min;
        }

        // Returns size of hashtable to grow to.
        public static int ExpandPrime(int oldSize)
        {
            var newSize = 2 * oldSize;

            // Allow the hash tables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newSize <= MAX_PRIME_ARRAY_LENGTH || MAX_PRIME_ARRAY_LENGTH <= oldSize)
            {
                return GetPrime(newSize);
            }

            Checks.IsTrue(MAX_PRIME_ARRAY_LENGTH == GetPrime(MAX_PRIME_ARRAY_LENGTH), "Invalid MaxPrimeArrayLength");
            return MAX_PRIME_ARRAY_LENGTH;
        }

        /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
        /// <remarks>This should only be used on 64-bit.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetFastModMultiplier(uint divisor)
            => ulong.MaxValue / (ulong)divisor + 1UL;

        /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
        /// <remarks>This should only be used on 64-bit.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FastMod(uint value, uint divisor, ulong multiplier)
            => (uint)(((multiplier * (ulong)value >> 32) + 1UL) * (ulong)divisor >> 32);
    }
}
