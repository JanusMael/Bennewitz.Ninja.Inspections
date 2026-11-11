// The segment `System` shadows the root namespace of a referenced assembly, which is exactly the
// defect BNAQ1004 exists for. Nothing in this file uses a qualified name through it — which is the
// point: the bug is invisible until somebody does, possibly years later.
// BNCQ1004: the shadow is the defect this fixture exists to hold, so it is suppressed here.
#pragma warning disable BNCQ1004
namespace AssemblyQuality.Tests.Fixtures.Shadow.System;

/// <summary>A public type whose namespace shadows a referenced root.</summary>
public sealed class Shadowing;
