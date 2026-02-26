using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Noya.Resgen
{
	// ReSharper disable once InvalidXmlDocComment
	/// <summary>
	/// Generates a specific resource based on a list of generators.
	/// </summary>
	/// <remarks>
	/// An <see cref="IResgenMath{TValue}"/> class is required to correctly interpred operations on <see cref="TValue"/>.<br/>
	///	Use <see cref="AddGenerator"/>() to add generators, and <see cref="Generate"/>() to generate a resource.
	/// </remarks>
	[PublicAPI]
	public class Resgen<TResource, TValue>
		where TResource : struct, Enum
		where TValue : struct
	{
		private readonly IResgenMath<TValue> math;
		private readonly Dictionary<GeneratorData<TResource, TValue>, GeneratorInstance<TResource, TValue>> activeGenerators = new();
		private readonly Dictionary<TResource, TValue> cachedGeneration = new();
		private readonly HashSet<TResource> dirtyResources = new();
		
		
		/// <summary>
		/// Creates a new instance of <see cref="Resgen{TResource,TValue}"/> with a custom <see cref="IResgenMath{TValue}"/> implementation.
		/// </summary>
		public Resgen(IResgenMath<TValue> math)
		{
			this.math = math;
		}

		/// <summary>
		/// Creates a new instance of <see cref="Resgen{TResource,TValue}"/> with a default <see cref="IResgenMath{TValue}"/> implementation.
		/// </summary>
		/// <remarks>Default Math types available are <see cref="float"/> and <see cref="int"/>. For other types, pass a custom implementation of <see cref="IResgenMath{TValue}"/></remarks>
		public Resgen()
		{
			if (typeof(TValue) == typeof(float))
			{
				math = new FloatMath() as IResgenMath<TValue>;
				return;
			}
			else if (typeof(TValue) == typeof(int))
			{
				math = new IntMath() as IResgenMath<TValue>;
				return;
			}
			
			throw new ArgumentException($"Unsupported math type {typeof(TValue)} (default is float). " +
			                              $"If you want to use a custom type, please provide an {nameof(IResgenMath<TValue>)} implementation for it.");
		}
		
		/// <summary>
		/// Adds a generator to the list of active generators.
		/// </summary>
		public void AddGenerator(GeneratorData<TResource, TValue> generatorData, TValue count)
		{
			// If the generator already exists, increment its count
			if (activeGenerators.TryGetValue(generatorData, out var generator))
			{
				generator.Count = math.Add(generator.Count, count);
			}
			else // Otherwise, add a new instance
			{
				activeGenerators.Add(generatorData, new GeneratorInstance<TResource, TValue>(generatorData, count));
			}
			
			// Mark the resource for cache recalculation
			dirtyResources.Add(generatorData.Resource);
		}
		
		/// <inheritdoc cref="AddGenerator(GeneratorData{TResource,TValue}, TValue)"/>
		public void AddGenerator(GeneratorData<TResource, TValue> generatorData) => AddGenerator(generatorData, math.One);
		
		/// <summary>
		/// Removes a number of generator from the list of active generators.
		/// </summary>
		/// <returns>true if item is successfully removed; otherwise.</returns>
		public bool RemoveGenerator(GeneratorData<TResource, TValue> generatorData, TValue generatorsToRemove)
		{
			if (!activeGenerators.TryGetValue(generatorData, out var generator))
			{
				return false;
			}

			generator.Count = math.Subtract(generator.Count, generatorsToRemove);

			if (math.Compare(generator.Count, math.Zero) <= 0)
			{
				activeGenerators.Remove(generatorData);
			}
			
			// Mark the resource for cache recalculation
			dirtyResources.Add(generatorData.Resource);

			return true;
		}
		
		/// <inheritdoc cref="RemoveGenerator(GeneratorData{TResource,TValue}, TValue)"/>
		public bool RemoveGenerator(GeneratorData<TResource, TValue> generatorData) => RemoveGenerator(generatorData, math.One);
		
		/// <summary>
		/// Generates a resource based on the active generators.
		/// </summary>
		/// <remarks>This method uses the intenral caching to prevent recalculating the same values.
		/// If you want to explicitly ignore caching and force a recalcualtion, use <see cref="GenerateWithoutCache"/> instead.</remarks>
		public TValue Generate(TResource resource)
		{
			if (dirtyResources.Contains(resource))
			{
				return GenerateWithoutCache(resource);
			}
			else
			{
				return cachedGeneration.TryGetValue(resource, out TValue value) ? value : math.Zero;
			}
		}
		
		/// <summary>
		/// Generates a resource based on the active generators, invalidating the cached production and recalculating it.
		/// </summary>
		/// <remarks>Normally you would want to use <see cref="Generate"/> instead.</remarks>
		public TValue GenerateWithoutCache(TResource resource)
		{
			TValue flat = math.Zero;
			TValue mult = math.Zero;
			TValue exp = math.One;
			int mag = 0;

			foreach (var generatorKvp in activeGenerators)
			{
				var generator = generatorKvp.Value;
				if (!generator.Resource.Equals(resource))
					continue;

				switch (generator.GeneratorType)
				{
					case GeneratorType.Flat:
						flat = math.Add(flat, math.Multiply(generator.Value, generator.Count));
						break;
					
					case GeneratorType.LinearMultiplier:
						mult = math.Add(mult, math.Multiply(generator.Value, generator.Count));
						break;
					
					case GeneratorType.GeometricMultiplier:
						// When adding the first geometric multiplier, the inital value needs to be 1 instead of 0
						if (math.Equals(mult, math.Zero)) mult = math.One;

						// Geometric multipliers are compounding (base * (mult1 * mult2 * ...))
						int count = (int)math.AsFloat(generator.Count);
						for (int i = 0; i < count; i++)
						{
							mult = math.Multiply(mult, generator.Value);
						}
						break;
					
					case GeneratorType.Exponential:
						exp = math.Add(exp, math.Multiply(generator.Value, generator.Count));
						break;
					
					case GeneratorType.Magnitude:
						mag += math.AsInt(math.Multiply(generator.Value, generator.Count));
						break;
					
					default: throw new ArgumentOutOfRangeException();
				}
			}
			
			TValue result = flat;
			// Apply the multipliers
			result = math.Multiply(result, math.Equals(mult, math.Zero) ? math.One : mult);
			// Apply the powers
			if (math.Compare(exp, math.One) > 0)
			{
				result = math.Power(result, math.AsFloat(exp));
			}
			// Apply the magnitudes
			result = math.ShiftMagnitude(ref result, mag);
			
			// Update the cache and remove the dirty resource
			cachedGeneration[resource] = result;
			dirtyResources.Remove(resource);
			
			return result;
		}

		/// <summary>
		/// Returns a readonly list of current generators of a specific resource.
		/// </summary>
		public IReadOnlyList<GeneratorData<TResource, TValue>> GetGenerators(TResource resource)
		{
			return activeGenerators.SelectMany(kvp => kvp.Value.Resource.Equals(resource) 
				? new[] { kvp.Value.Data }
				: Enumerable.Empty<GeneratorData<TResource, TValue>>()).ToList();
		}
	}
}
