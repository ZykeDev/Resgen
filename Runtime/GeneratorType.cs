using System;

namespace Noya.Resgen
{
    public enum GeneratorType
    {
        /// <summary>
        /// Adds a flat amount of resources.
        /// </summary>
        Flat,
        /// <summary>
        /// Multiplies the amount of resources by a value.
        /// Multiplier values are summed together before being applied (base * (mult1 + mult2 + ...).
        /// </summary>
        LinearMultiplier,
        /// <summary>
        /// Multiplies the amount of resources by a value.
        /// Multiplier values are multiplied together before being applied (base * (mult1 * mult2 * ...)).
        /// </summary>
        GeometricMultiplier,
        /// <summary>
        /// Raises the amount of resources to a power.
        /// </summary>
        Exponential,
        /// <summary>
        /// Increases (or decreases) the exponent of the base value. TValues will always be treated as <see cref="int"/>s.
        /// </summary>
        Magnitude
    }
    
    public static class GeneratorExtensions
    {
        /// <summary>
        /// Returns the symbol associated with a <see cref="GeneratorType"/>.
        /// </summary>
        public static string ToSymbol(this GeneratorType type)
        {
            return type switch
            {
                GeneratorType.Flat => "+",
                GeneratorType.LinearMultiplier => "*",
                GeneratorType.GeometricMultiplier => "**",
                GeneratorType.Exponential => "^",
                GeneratorType.Magnitude => ">",
                var _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}