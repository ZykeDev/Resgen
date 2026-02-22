using System;
#nullable disable

// Overrides JetBrains annotations in libraries that don't include Rider as a dependency
namespace JetBrains.Annotations
{
	[AttributeUsage(AttributeTargets.All, Inherited = false)]
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class PublicAPIAttribute : Attribute
	{
		[CanBeNull] public string Comment { get; }
		public PublicAPIAttribute() { }
		public PublicAPIAttribute([NotNull] string comment) => Comment = comment;
	}
}