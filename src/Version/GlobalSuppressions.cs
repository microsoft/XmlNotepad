// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "XML Notepad is not yet localized.")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Silly for command line tools.")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "This is not a framework, so doesn't need such things.")]
[assembly: SuppressMessage("Security", "CA3075:Insecure DTD processing in XML", Justification = "This is not a framework, and we need full DTD processing.")]

[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "I don't like this rule")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "I don't like this rule")]
