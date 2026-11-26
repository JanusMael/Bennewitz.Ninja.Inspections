using Bennewitz.Ninja.CodeQuality;
using Bennewitz.Ninja.CodeQuality.Rules;
using Microsoft.CodeAnalysis;

namespace CodeQuality.Tests.Rules;

/// <summary>
/// BNCQ1012 reads the template of every call plans/00014 decision 3 names, through an interface and a class alike,
/// and reports the first place it leaves the form, a template that is not a constant, a <c>:l</c> hole and an enum
/// given to a hole; and nothing in the form, nothing suppressed, nothing generated, and nothing at all where neither
/// logger can be named.
/// </summary>
public sealed class LogLineFormTests
{
    private const string Usings = """
        using System;
        using Microsoft.Extensions.Logging;

        """;

    private static Task Run(string source) =>
        new RuleTest<LogLineFormAnalyzer>(Usings + source) { ReferenceAssemblies = LoggingReferences.Both }.Run();

    /// <summary>The messages of every finding over <paramref name="files"/>, in source order.</summary>
    private static async Task<List<string>> Messages(params (string Path, string Source)[] files)
    {
        Plain.Run run = await Plain.Analyze(
            new LogLineFormAnalyzer(),
            files.Select(file => (file.Path, WithUsings(file.Source))),
            await LoggingReferences.Resolve(LoggingReferences.Both));
        Assert.Empty(run.Errors);
        return [.. run.Diagnostics.OrderBy(d => d.Location.SourceSpan.Start).Select(d => d.GetMessage())];
    }

    private static Task<List<string>> Messages(string source) => Messages(("/Test0.cs", source));

    /// <summary>The usings after any comment lines a source starts with, so a generated header stays its first line.</summary>
    private static string WithUsings(string source)
    {
        string[] lines = source.Split('\n');
        int header = lines.TakeWhile(line => line.TrimStart().StartsWith("//", StringComparison.Ordinal)).Count();
        return string.Join("\n", lines.Take(header)) + (header > 0 ? "\n" : "") + Usings + string.Join("\n", lines.Skip(header));
    }

    /// <summary>
    /// Every call decision 3 names, each given a template with no tag: the extension methods on an <c>ILogger</c> and
    /// an <c>ILogger&lt;T&gt;</c>, <c>Log</c> and <c>Define</c>, and Serilog's on the static <c>Log</c>, on its
    /// interface, on <c>Serilog.Core.Logger</c> and on a type of the consumer's that implements it.
    /// </summary>
    [Fact]
    public async Task Every_call_it_reads_fires_through_an_interface_and_a_class_alike() =>
        await Run("""
            public sealed class Calls
            {
                public void Write(ILogger mel, ILogger<Calls> typed, Serilog.ILogger seri, Serilog.Core.Logger core, Own own, Exception error)
                {
                    mel.LogTrace({|BNCQ1012:"Saved"|});
                    mel.LogDebug({|BNCQ1012:"Saved"|});
                    mel.LogInformation({|BNCQ1012:"Saved"|});
                    mel.LogWarning(error, {|BNCQ1012:"Saved"|});
                    mel.LogError(new EventId(1), {|BNCQ1012:"Saved"|});
                    mel.LogCritical({|BNCQ1012:"Saved"|});
                    mel.Log(LogLevel.Information, {|BNCQ1012:"Saved"|});
                    typed.LogInformation({|BNCQ1012:"Saved"|});
                    var saved = LoggerMessage.Define(LogLevel.Information, new EventId(2), {|BNCQ1012:"Saved"|});
                    Serilog.Log.Verbose({|BNCQ1012:"Saved"|});
                    Serilog.Log.Debug({|BNCQ1012:"Saved"|});
                    Serilog.Log.Information({|BNCQ1012:"Saved"|});
                    Serilog.Log.Warning({|BNCQ1012:"Saved"|});
                    Serilog.Log.Error(error, {|BNCQ1012:"Saved"|});
                    Serilog.Log.Fatal({|BNCQ1012:"Saved"|});
                    Serilog.Log.Write(Serilog.Events.LogEventLevel.Information, {|BNCQ1012:"Saved"|});
                    seri.Information({|BNCQ1012:"Saved"|});
                    core.Information({|BNCQ1012:"Saved"|});
                    own.Information({|BNCQ1012:"Saved"|});
                }
            }

            public sealed class Own : Serilog.ILogger
            {
                public void Write(Serilog.Events.LogEvent logEvent) { }

                public void Information(string messageTemplate) { }
            }
            """);

    /// <summary>A template in the form, through each call, and what decision 3 leaves out, whatever it is given.</summary>
    [Fact]
    public async Task The_form_and_the_calls_it_does_not_read_stay_silent() =>
        await Run("""
            public sealed class Fine
            {
                private const string Tag = "[Backup.Tab]";

                public void Write(ILogger mel, Serilog.ILogger seri, int index, string tab)
                {
                    mel.LogInformation("[Backup.Tab] index={Index} tab=\"{Tab}\"", index, tab);
                    mel.Log(LogLevel.Debug, "[Backup.Start]");
                    var saved = LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), "[Backup.Tab] index={Index}");
                    seri.Information("[Backup.Tab] elapsedMs={Ms}ms", 3);
                    Serilog.Log.Information("[Backup.Tab] " + "index={Index}", index);
                    Serilog.Log.Information($"{Tag} braces={{literal}}");
                    using IDisposable? scope = mel.BeginScope("Not a line {Scope}", 1);
                    mel.Log(LogLevel.Information, new EventId(1), tab, null, (state, _) => state);
                    Console.WriteLine("Not a line {Index}", index);
                }
            }
            """);

    /// <summary>Each clause of decision 5 kept: the templates that are in the form.</summary>
    [Theory]
    [InlineData("[Backup.Start]")]
    [InlineData("[Backup.Tab] index={Index}")]
    [InlineData("[Backup.Tab] index={Index} tab=\"{Tab}\" elapsedMs={ElapsedMs}")]
    [InlineData("[Backup.Tab] elapsedMs={Ms}ms")]
    [InlineData("[Backup.Tab] braces={{literal}}")]
    [InlineData("[Backup.Tab] name=\"{First} {Last}\"")]
    [InlineData("[Backup.Tab] note=\"free text, with = and [brackets]\"")]
    [InlineData("[Backup.Tab] user={@User} raw={$Raw} n={N,5:000}")]
    [InlineData("[Http2.Get2] key2=v")]
    public void A_template_in_the_form_does_not_leave_it(string template) =>
        Assert.Null(LogTemplates.FirstDeparture(template));

    /// <summary>
    /// Each way out of the form decision 5 names, and where: the index of the first character the template leaves
    /// it at.
    /// </summary>
    [Theory]
    [InlineData("Backup started for {Tab}", "no tag", 0)]
    [InlineData("", "no tag", 0)]
    [InlineData(" [Backup.Tab]", "no tag", 0)]
    [InlineData("{{Backup}} done", "no tag", 0)]
    [InlineData("[Backup] index={Index}", "[Area] alone", 0)]
    [InlineData("[backup.Tab]", "not PascalCase", 1)]
    [InlineData("[Backup.tab]", "not PascalCase", 8)]
    [InlineData("[Backup.Tab.Row]", "not PascalCase", 8)]
    [InlineData("[{Area}.Tab]", "not PascalCase", 1)]
    [InlineData("[Backup_Old.Tab]", "not PascalCase", 7)]
    [InlineData("[Backup.Tab] Started the backup", "a sentence", 13)]
    [InlineData("[Backup.Tab] index={Index} done", "a sentence", 27)]
    [InlineData("[Backup.Tab] Index={Index}", "not camelCase", 13)]
    [InlineData("[Backup.Tab] elapsed_ms={Ms}", "not camelCase", 13)]
    [InlineData("[Backup.Tab] {Key}={Value}", "not camelCase", 13)]
    [InlineData("[Backup.Tab] {{key}}=1", "not camelCase", 13)]
    [InlineData("[Backup.Tab]  index={Index}", "two spaces", 12)]
    [InlineData("[Backup.Tab] index={Index}  tab=x", "two spaces", 26)]
    [InlineData("[Backup.Tab] tab=\"{Tab}\"x", "after the last pair", 24)]
    [InlineData("[Backup.Tab] tab=a\"b\"", "after the last pair", 18)]
    [InlineData("[Backup.Tab]index={Index}", "against the tag", 12)]
    [InlineData("[Backup.Tab] index={Index} ", "a space at the end", 26)]
    [InlineData("[Backup.Tab] index=", "no value", 13)]
    [InlineData("[Backup.Tab] index= tab=x", "no value", 13)]
    [InlineData("[Backup.Tab] tab=\"{Tab}", "does not close", 17)]
    public void A_template_out_of_the_form_is_named_where_it_leaves_it(string template, string what, int at)
    {
        LogTemplates.Departure? departure = LogTemplates.FirstDeparture(template);

        Assert.NotNull(departure);
        Assert.Contains(what, departure.What, StringComparison.Ordinal);
        Assert.Equal(at, departure.At);
    }

    /// <summary>The finding names what leaves the form and the template from there.</summary>
    [Fact]
    public async Task The_finding_names_what_leaves_the_form_and_where() =>
        Assert.Equal(
            ["This log template is not in the family's one form: a sentence, where only key=value pairs may follow the tag, at \"Started the backup {Tab}\". A line is a tag, then key=value pairs only, camelCase keys and free text quoted, as [Backup.Tab] index={Index} tab=\"{Tab}\", so one search finds every line of a kind. Rewrite it in the form; a line that passes on one already written is suppressed where it is, with its reason."],
            await Messages("""
                public static class Lines
                {
                    public static void Write(ILogger mel) => mel.LogInformation("[Backup.Tab] Started the backup {Tab}", "t");
                }
                """));

    /// <summary>A hole whose format is <c>l</c>, by decision 6, and no other format.</summary>
    [Fact]
    public async Task A_hole_whose_format_is_l_fires() =>
        await Run("""
            public static class Lines
            {
                public static void Write(Serilog.ILogger seri, string tab)
                {
                    seri.Information({|BNCQ1012:"[Backup.Tab] tab={Tab:l}"|}, tab);
                    seri.Information("[Backup.Tab] tab={Tab:lj} n={N:000}", tab, 1);
                }
            }
            """);

    /// <summary>
    /// An enum and a nullable enum, through decision 7's mappings: by position, by number, to Serilog's generic
    /// value, seen through a cast to <c>object</c>; and not its name, nor a number made of it.
    /// </summary>
    [Fact]
    public async Task An_enum_given_to_a_hole_fires_by_position_and_by_number() =>
        Assert.Equal(
            [
                "The hole {Mode} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {Maybe} is given an enum, Mode?, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {0} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {0} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {Mode} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {@Mode} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {Mode} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
            ],
            await Messages("""
                public enum Mode { Fast, Slow }

                public static class Lines
                {
                    public static void Write(ILogger mel, Serilog.ILogger seri, Mode mode, Mode? maybe)
                    {
                        mel.LogInformation("[Backup.Tab] mode={Mode}", mode);
                        mel.LogInformation("[Backup.Tab] maybe={Maybe}", maybe);
                        mel.LogInformation("[Backup.Tab] a={0} b={0}", mode);
                        seri.Information("[Backup.Tab] mode={Mode}", mode);
                        seri.Information("[Backup.Tab] mode={@Mode}", mode);
                        mel.LogInformation("[Backup.Tab] mode={Mode}", (object)mode);
                        mel.LogInformation("[Backup.Tab] mode={Mode}", mode.ToString());
                        mel.LogInformation("[Backup.Tab] mode={Mode}", (int)mode);
                    }
                }
                """));

    /// <summary>
    /// Holes and values that do not pair place no enum, as escaped braces are no hole: that mapping is left to the
    /// compiler's own analyzers. An array made elsewhere and passed whole is one value, an array.
    /// </summary>
    [Fact]
    public async Task An_enum_that_cannot_be_placed_is_not_reported() =>
        Assert.Empty(await Messages("""
            public enum Mode { Fast, Slow }

            public static class Lines
            {
                public static void Write(ILogger mel, Mode mode)
                {
                    mel.LogInformation("[Backup.Tab] mode={Mode}", mode, 2);
                    mel.LogInformation("[Backup.Tab] mode={Mode} n={N}", mode);
                    mel.LogInformation("[Backup.Tab] a={1} b={1}", mode);
                    mel.LogInformation("[Backup.Tab] braces={{Mode}}", mode);
                    object?[] values = { mode };
                    mel.LogInformation("[Backup.Tab] mode={Mode}", values);
                }
            }
            """));

    /// <summary>
    /// <c>Define</c>'s holes take its type arguments, and <c>[LoggerMessage]</c>'s its parameters by name, ignoring
    /// case, leaving out the logger, the exception and the level.
    /// </summary>
    [Fact]
    public async Task An_enum_given_through_Define_or_LoggerMessage_fires() =>
        Assert.Equal(
            [
                "The hole {Mode} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
                "The hole {MODE} is given an enum, Mode, which the family's {Message:lj} sinks print quoted, as level=\"Warning\", where every other identifier is bare. Pass its name, a string, with ToString().",
            ],
            await Messages(
                ("/Lines.cs", """
                    public enum Mode { Fast, Slow }

                    public static partial class Lines
                    {
                        public static readonly Action<ILogger, Mode, int, Exception?> Saved =
                            LoggerMessage.Define<Mode, int>(LogLevel.Information, new EventId(1), "[Backup.Tab] mode={Mode} n={N}");

                        [LoggerMessage(EventId = 2, Message = "[Backup.Tab] mode={MODE} tab={Tab}")]
                        public static partial void Switched(ILogger logger, LogLevel level, Exception error, Mode mode, string tab);
                    }
                    """),
                ("/Lines.g.cs", Generated("Switched(ILogger logger, LogLevel level, Exception error, Mode mode, string tab)"))));

    /// <summary>
    /// A <c>[LoggerMessage]</c> template is read, by name or by its constructor's argument, although Roslyn takes the
    /// method for generated code, its generated half carrying <c>[GeneratedCode]</c>; that half's own call is not.
    /// </summary>
    [Fact]
    public async Task A_LoggerMessage_template_is_read_though_Roslyn_takes_its_method_for_generated() =>
        await new RuleTest<LogLineFormAnalyzer>(Usings + """
            public static partial class Lines
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = {|BNCQ1012:"Saved {Path}"|})]
                public static partial void Saved(ILogger logger, string path);

                [LoggerMessage(2, LogLevel.Warning, {|BNCQ1012:"[Backup] path={Path}"|})]
                public static partial void Failed(ILogger logger, string path);

                [LoggerMessage(3, LogLevel.Debug, "[Backup.Tab] path={Path}")]
                public static partial void Fine(ILogger logger, string path);
            }
            """)
        {
            ReferenceAssemblies = LoggingReferences.Both,
            TestState =
            {
                Sources =
                {
                    ("/0/Lines.g.cs", Usings + Generated("Saved(ILogger logger, string path)", "Failed(ILogger logger, string path)", "Fine(ILogger logger, string path)")),
                },
            },
        }.Run();

    /// <summary>A template that is not a constant, by decision 4: built at run time, or interpolated from values.</summary>
    [Fact]
    public async Task A_template_that_is_not_a_constant_fires() =>
        await Run("""
            public static class Lines
            {
                public static void Write(ILogger mel, string name, string template)
                {
                    mel.LogInformation({|BNCQ1012:$"[Backup.Tab] name={name}"|});
                    mel.LogInformation({|BNCQ1012:template|});
                    Serilog.Log.Information({|BNCQ1012:"[Backup.Tab] " + name|});
                }
            }
            """);

    /// <summary>A call suppressed where it is written, by decision 8, with <c>#pragma</c> or <c>[SuppressMessage]</c>.</summary>
    [Fact]
    public async Task A_suppressed_call_is_silent() =>
        Assert.Empty(await Messages("""
            public static class Lines
            {
                public static void Forward(ILogger mel, string line)
                {
            #pragma warning disable BNCQ1012 // Writes a line already rendered in the form.
                    mel.LogInformation("{RawLine}", line);
            #pragma warning restore BNCQ1012
                }

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Bennewitz.Ninja.CodeQuality", "BNCQ1012", Justification = "Writes a line already rendered in the form.")]
                public static void Pass(ILogger mel, string line) => mel.LogInformation("{RawLine}", line);
            }
            """));

    /// <summary>
    /// Generated code, by decision 9: a file with a generated header or name, a <c>[LoggerMessage]</c> method declared
    /// in one among them.
    /// </summary>
    [Fact]
    public async Task Generated_code_is_not_analyzed() =>
        Assert.Empty(await Messages(
            ("/Partial.g.cs", """
                public static partial class Gen
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Saved")]
                    public static partial void Saved(ILogger logger);

                    public static partial void Saved(ILogger logger) { }
                }
                """),
            ("/Header.cs", """
                // <auto-generated/>
                public static class Header
                {
                    public static void Write(ILogger mel) => mel.LogInformation("Saved");
                }
                """),
            ("/Named.g.cs", """
                public static class Named
                {
                    public static void Write(ILogger mel) => mel.LogInformation("Saved");
                }
                """)));

    /// <summary>
    /// A body written by hand in a partial method whose generated half carries <c>[GeneratedCode]</c>, as an MVVM
    /// Toolkit <c>On…Changed</c> hook is, which Roslyn takes for generated code: measured hiding 8 of ClaudeForge's
    /// templates. Only a file a tool wrote is left out, so the attribute decides nothing.
    /// </summary>
    [Fact]
    public async Task A_hand_written_body_whose_generated_half_is_marked_fires() =>
        await new RuleTest<LogLineFormAnalyzer>(Usings + """
            public sealed partial class Settings
            {
                private readonly ILogger log;

                public Settings(ILogger log) => this.log = log;

                partial void OnPathChanged(string value) => log.LogInformation({|BNCQ1012:"Path changed to {Path}"|}, value);
            }
            """)
        {
            ReferenceAssemblies = LoggingReferences.Both,
            TestState =
            {
                Sources =
                {
                    ("/0/Settings.g.cs", """
                        // <auto-generated/>
                        public sealed partial class Settings
                        {
                            [System.CodeDom.Compiler.GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0")]
                            partial void OnPathChanged(string value);
                        }
                        """),
                },
            },
        }.Run();

    private const string Unformed = """
        public static class Lines
        {
            public static void Write(Microsoft.Extensions.Logging.ILogger mel, Serilog.ILogger seri)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(mel, "Saved");
                seri.Information("Saved");
            }
        }
        """;

    /// <summary>On by default, as a warning, by decision 1.</summary>
    /// <remarks>
    /// ⚠ On a plain compilation, not the testing framework, which enables every diagnostic of the analyzer under test
    /// and so cannot show the default.
    /// </remarks>
    [Fact]
    public async Task By_default_a_template_out_of_the_form_is_a_warning()
    {
        Plain.Run run = await Plain.Analyze(new LogLineFormAnalyzer(), Unformed, await LoggingReferences.Resolve(LoggingReferences.Both));

        Assert.Empty(run.Errors);
        Assert.Equal(2, run.Diagnostics.Length);
        Assert.All(run.Diagnostics, finding =>
        {
            Assert.Equal(LogLineFormAnalyzer.Id, finding.Id);
            Assert.Equal(DiagnosticSeverity.Warning, finding.Severity);
        });
    }

    /// <summary>Either logger alone opens the gate, by decision 2.</summary>
    [Fact]
    public async Task Either_logger_alone_opens_the_gate()
    {
        Plain.Run mel = await Plain.Analyze(new LogLineFormAnalyzer(), """
            public static class Lines
            {
                public static void Write(Microsoft.Extensions.Logging.ILogger mel) => Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(mel, "Saved");
            }
            """, await LoggingReferences.Resolve(LoggingReferences.MelOnly));
        Plain.Run serilog = await Plain.Analyze(new LogLineFormAnalyzer(), """
            public static class Lines
            {
                public static void Write() => Serilog.Log.Information("Saved");
            }
            """, await LoggingReferences.Resolve(LoggingReferences.SerilogOnly));

        Assert.Empty(mel.Errors);
        Assert.Empty(serilog.Errors);
        Assert.Single(mel.Diagnostics);
        Assert.Single(serilog.Diagnostics);
    }

    /// <summary>
    /// Where the compilation names neither logger, nothing is registered, by decision 2.
    /// </summary>
    [Fact]
    public async Task Without_either_logger_the_rule_registers_nothing()
    {
        Plain.Run inert = await Plain.Analyze(new LogLineFormAnalyzer(), """
            public static class Lines
            {
                public static void Write() => System.Console.WriteLine("Saved {Path}");
            }
            """, await Plain.Net80());
        Plain.Run control = await Plain.Analyze(new LogLineFormAnalyzer(), Unformed, await LoggingReferences.Resolve(LoggingReferences.Both));

        Assert.Equal(0, inert.Registrations);
        Assert.Empty(inert.Diagnostics);
        Assert.Empty(control.Errors);
        Assert.True(control.Registrations > 0, "In scope, the rule registered nothing, so a zero proves nothing.");
        Assert.Equal(2, control.Diagnostics.Length);
    }

    /// <summary>
    /// The generator's half of each <c>[LoggerMessage]</c> method named, as the generator writes it: in its own file,
    /// marked generated, with a call of its own whose template is out of the form.
    /// </summary>
    private static string Generated(params string[] signatures) =>
        "// <auto-generated/>\npublic static partial class Lines\n{\n"
        + string.Concat(signatures.Select(signature =>
            "    [System.CodeDom.Compiler.GeneratedCode(\"Microsoft.Extensions.Logging.Generators\", \"10.0.12\")]\n"
            + "    public static partial void " + signature + " => Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(logger, \"Not the form\");\n"))
        + "}\n";
}
