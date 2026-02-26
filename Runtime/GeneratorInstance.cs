using System;

namespace Noya.Resgen
{
    public class GeneratorInstance<TResource, TValue> where TResource : struct, Enum
    {
        public GeneratorData<TResource, TValue> Data { get; }
        public TValue Count { get; set; }
        public TResource Resource => Data.Resource;
        public TValue Value => Data.Value;
        public GeneratorType GeneratorType => Data.GeneratorType;
		

        internal GeneratorInstance(GeneratorData<TResource, TValue> data, TValue initialCount)
        {
            Data = data;
            Count = initialCount;
        }
    }
}