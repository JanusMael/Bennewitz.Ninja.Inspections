// The same defect as Shadowed.cs, on an INTERNAL type. BNAQ1004's default reads only what a consumer
// can see and passes over it; NamespaceShadowRule.IncludingInternalTypes() reports it — and must,
// because the CS0234 it causes breaks internal code exactly as hard as public code.
// BNCQ1004: the shadow is the defect this fixture exists to hold, so it is suppressed here.
#pragma warning disable BNCQ1004
namespace AssemblyQuality.Tests.Fixtures.Hidden.System;

/// <summary>An internal type whose namespace shadows a referenced root.</summary>
internal sealed class HiddenShadowing;
