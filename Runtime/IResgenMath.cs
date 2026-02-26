using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Noya.Resgen
{
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

	[PublicAPI]
	public class FloatMath : IResgenMath<float>
	{
		public float Zero => 0f;
		public float One => 1f;
		public float Add(float a, float b) => a + b;
		public float Subtract(float a, float b) => a - b;
		public float Multiply(float a, float b) => a * b;
		public float Divide(float a, float b) => a / b;
		public float Power(float a, float b) => Mathf.Pow(a, b);
		public float ShiftMagnitude(ref float a, int b) => a * Mathf.Pow(10, b);
		
		public bool Equals(float a, float b) => Mathf.Approximately(a, b);
		public int GetHashCode(float a) => a.GetHashCode();
		public int Compare(float a, float b) => a.CompareTo(b);
		
		public float AsFloat(float a) => a;
		public int AsInt(float a) => (int)a;
	}
	
	[PublicAPI]
	public class IntMath : IResgenMath<int>
	{
		public int Zero => 0;
		public int One => 1;
		public int Add(int a, int b) => a + b;
		public int Subtract(int a, int b) => a - b;
		public int Multiply(int a, int b) => a * b;
		public int Divide(int a, int b) => a / b;
		public int Power(int a, float b) => (int)Mathf.Pow(a, b);
		public int ShiftMagnitude(ref int a, int b) => (int)(a * Mathf.Pow(10, b));
		
		public bool Equals(int a, int b) => Mathf.Approximately(a, b);
		public int GetHashCode(int a) => a.GetHashCode();
		public int Compare(int a, int b) => a.CompareTo(b);
		
		public float AsFloat(int a) => a;
		public int AsInt(int a) => a;
	}
}