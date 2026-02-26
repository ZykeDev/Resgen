using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Noya.Resgen
{
    [PublicAPI]
    public abstract class GeneratorData<TResource, TValue> : ScriptableObject where TResource : struct, Enum
    {
        [field:SerializeField] public GeneratorType GeneratorType { get; protected set; }
        [field:SerializeField] public TResource Resource { get; protected set; }
        [field:SerializeField] public TValue Value { get; protected set; }
    }
}