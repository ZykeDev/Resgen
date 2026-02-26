using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Noya.Resgen
{
    [PublicAPI]
    public abstract class GeneratorData<TResource, TValue> : ScriptableObject where TResource : struct, Enum
    {
        public GeneratorType GeneratorType { get; protected set; }
        public TResource Resource { get; protected set; }
        public TValue Value { get; protected set; }
    }
}