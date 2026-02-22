using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Noya.Resgen
{
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
		private readonly Dictionary<GeneratorScriptableObject<TResource, TValue>, GeneratorInstance<TResource, TValue>> activeGenerators = new();
		private readonly Dictionary<TResource, TValue> cachedGeneration = new();
		private readonly HashSet<TResource> dirtyResources = new();
		

		public Resgen(IResgenMath<TValue> math)
		{
			this.math = math;
		}
		
		/// <summary>
		/// Adds a generator to the list of active generators.
		/// </summary>
		public void AddGenerator(GeneratorScriptableObject<TResource, TValue> generatorData, TValue count)
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
		
		/// <inheritdoc cref="AddGenerator(GeneratorScriptableObject{TResource, TValue}, TValue)"/>
		public void AddGenerator(GeneratorScriptableObject<TResource, TValue> generatorData) => AddGenerator(generatorData, math.One);
		
		/// <summary>
		/// Removes a number of generator from the list of active generators.
		/// </summary>
		/// <returns>true if item is successfully removed; otherwise.</returns>
		public bool RemoveGenerator(GeneratorScriptableObject<TResource, TValue> generatorData, TValue generatorsToRemove)
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

		/// <inheritdoc cref="RemoveGenerator(GeneratorScriptableObject{TResource, TValue}, TValue)"/>
		public void RemoveGenerator(GeneratorScriptableObject<TResource, TValue> generatorData) => RemoveGenerator(generatorData, math.One);

		
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
	}
	
	public interface IResgenMath<TValue> : IEqualityComparer<TValue>, IComparer<TValue>
	{
		TValue Zero { get; }
		TValue One { get; }
		TValue Add(TValue a, TValue b);
		TValue Subtract(TValue a, TValue b);
		TValue Multiply(TValue a, TValue b);
		TValue Divide(TValue a, TValue b);
		TValue Power(TValue a, float b);
		TValue ShiftMagnitude(ref TValue a, int b);
		// TODO warn the user about Pow capping at ^float.MaxValue
		public float AsFloat(TValue a);
		public int AsInt(TValue a);
	}

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

	public abstract class GeneratorScriptableObject<TResource, TValue> : ScriptableObject where TResource : struct, Enum
	{
		public GeneratorType GeneratorType;
		public TResource Resource;
		public TValue Value;
	}
	
	internal class GeneratorInstance<TResource, TValue> where TResource : struct, Enum
	{
		public GeneratorScriptableObject<TResource, TValue> Data { get; }
		public TValue Count { get; set; }
		public TResource Resource => Data.Resource;
		public TValue Value => Data.Value;
		public GeneratorType GeneratorType => Data.GeneratorType;
		

		internal GeneratorInstance(GeneratorScriptableObject<TResource, TValue> data, TValue initialCount)
		{
			Data = data;
			Count = initialCount;
		}
	}
}
