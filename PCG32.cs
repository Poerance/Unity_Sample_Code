namespace PCG_32 {
  using UnityEngine;
  
  public class PCG32 {
      private ulong state;
      private readonly ulong inc;
      private const float FloatScale = 1.0f / 16777216.0f; // Ensures inclusive upper bound
  
      /// <summary>
      /// Initializes the PCG32 random number generator with a sequence number.
      /// A random seed is automatically generated.
      /// </summary>
      /// <param name="sequence">The sequence number to modify the random stream.</param>
      public PCG32(ulong sequence = 1) {
          ulong seed = (ulong)System.DateTime.Now.Ticks;
  
          state = 0;
          inc = (sequence << 1) | 1;
          NextUInt();
          state += seed;
          NextUInt();
      }
  
      /// <summary>
      /// Generates the next random unsigned 32-bit integer using the PCG algorithm.
      /// </summary>
      /// <returns>A randomly generated 32-bit unsigned integer.</returns>
      private uint NextUInt() {
          ulong oldState = state;
          state = oldState * 6364136223846793005UL + inc;
          uint xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
          uint rot = (uint)(oldState >> 59);
          return (xorshifted >> (int)rot) | (xorshifted << ((-(int)rot) & 31));
      }
  
      /// <summary>
      /// Returns a random integer.
      /// </summary>
      /// <returns>A randomly generated integer.</returns>
      public int Next() {
          return (int)NextUInt();
      }
  
      /// <summary>
      /// Returns a random integer within the specified range (inclusive).
      /// </summary>
      /// <param name="min">The inclusive lower bound.</param>
      /// <param name="max">The inclusive upper bound.</param>
      /// <returns>A random integer in the range [min, max].</returns>
      /// <exception cref="System.ArgumentException">Thrown if min is greater than max.</exception>
      public int Range(int min, int max) {
          if (min > max)
              throw new System.ArgumentException("Min must be less than or equal to Max");
          return min + (int)(NextUInt() % (uint)(max - min + 1));
      }
  
      /// <summary>
      /// Returns a random float within the specified range (inclusive).
      /// </summary>
      /// <param name="min">The inclusive lower bound.</param>
      /// <param name="max">The inclusive upper bound.</param>
      /// <returns>A random float in the range [min, max].</returns>
      /// <exception cref="System.ArgumentException">Thrown if min is greater than max.</exception>
      public float Range(float min, float max) {
          if (min > max)
              throw new System.ArgumentException("Min must be less than or equal to Max");
          return Mathf.Lerp(min, max, RandomFloat());
      }
  
      /// <summary>
      /// Returns a random float in the range [0, 1] (inclusive).
      /// </summary>
      /// <returns>A random float between 0 (inclusive) and 1 (inclusive).</returns>
      public float RandomFloat() {
          return ((NextUInt() >> 8) + 1) * FloatScale; // Ensures inclusivity
      }
  
      /// <summary>
      /// Returns a bool based on chance.
      /// </summary>
      public bool Chance(float chance) {
          if (chance <= 0) {
              return false;
          } else if (chance >= 1) {
              return true;
          }
          return RandomFloat() <= chance;
      }
  }
}
