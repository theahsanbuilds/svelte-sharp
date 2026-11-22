
using System;

namespace SvelteSharp.Compiler;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ParameterAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StateAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class BindableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class EffectAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class OnMountAttribute : Attribute { }
