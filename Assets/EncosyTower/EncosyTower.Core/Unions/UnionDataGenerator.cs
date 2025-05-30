#if UNITY_EDITOR && ANNULUS_CODEGEN && ENCOSY_UNIONS_GENERATOR

using System.Diagnostics.CodeAnalysis;
using EncosyTower.CodeGen;
using UnityCodeGen;

namespace EncosyTower.Editor.Unions
{
    [Generator]
    internal class UnionDataGenerator : ICodeGenerator
    {
        private const uint SIZE_OF_LONG = 8;
        private const uint MAX_LONG_COUNT = 512;
        private const uint DEFAULT_BYTE_COUNT = SIZE_OF_LONG * 2;
        private const uint MAX_BYTE_COUNT = SIZE_OF_LONG * MAX_LONG_COUNT;

        public void Execute([NotNull] GeneratorContext context)
        {
            if (CodeGenAPI.TryGetOutputFolderPath(nameof(UnionDataGenerator), out var outputPath) == false)
            {
                context.OverrideFolderPath("Assets");
                return;
            }

            var p = Printer.DefaultLarge;
            p.PrintAutoGeneratedBlock(GetType().Name);
            p.PrintEndLine();
            p.PrintLine("#pragma warning disable");
            p.PrintEndLine();
            p.Print($"// For practical reason, UnionData should be {DEFAULT_BYTE_COUNT} bytes by default.").PrintEndLine();
            p.Print($"#define UNION_{DEFAULT_BYTE_COUNT}_BYTES").PrintEndLine();
            p.Print($"#define UNION_{(DEFAULT_BYTE_COUNT / SIZE_OF_LONG) * 2}_INTS").PrintEndLine();
            p.Print($"#define UNION_{DEFAULT_BYTE_COUNT / SIZE_OF_LONG}_LONGS").PrintEndLine();
            p.PrintEndLine();

            p.PrintLine(@"using System.Runtime.InteropServices;");
            p.PrintEndLine();

            p.PrintLine("namespace EncosyTower.Unions");
            p.OpenScope();
            {
                p.PrintLine("/// <summary>");
                p.PrintLine("/// Represents a memory layout that can store the actual data of several types.");
                p.PrintLine($"/// The data size can be between {DEFAULT_BYTE_COUNT} and {MAX_BYTE_COUNT} bytes.");
                p.PrintLine("/// </summary>");
                p.PrintLine("/// <remarks>");
                p.PrintLine($"/// For practical reason, the default size is {DEFAULT_BYTE_COUNT} bytes.");
                p.PrintLine("/// <br />");
                p.PrintLine("/// <br />");
                p.PrintLine("/// To resize, define one of the following symbols:");
                p.PrintLine("/// <list type=\"bullet\">");

                for (var i = 0; i <= 3; i++)
                {
                    var size = DEFAULT_BYTE_COUNT + SIZE_OF_LONG * i;
                    var count = size / SIZE_OF_LONG;

                    p.PrintBeginLine($"/// <item>")
                        .Print($"<c>UNION_{size}_BYTES</c>")
                        .Print("; or ")
                        .Print($"<c>UNION_{count * 2}_INTS</c>")
                        .Print("; or ")
                        .Print($"<c>UNION_{count}_LONGS</c>")
                        .PrintEndLine("</item>");
                }

                p.PrintLine("/// <item><c>...</c></item>");

                for (var i = 2; i >= 0; i--)
                {
                    var size = SIZE_OF_LONG * (MAX_LONG_COUNT - i);
                    var count = size / SIZE_OF_LONG;

                    p.PrintBeginLine($"/// <item>")
                        .Print($"<c>UNION_{size}_BYTES</c>")
                        .Print("; or ")
                        .Print($"<c>UNION_{count * 2}_INTS</c>")
                        .Print("; or ")
                        .Print($"<c>UNION_{count}_LONGS</c>")
                        .PrintEndLine("</item>");
                }

                p.PrintLine("/// </list>");
                p.PrintLine("/// </remarks>");
                p.PrintLine("[StructLayout(LayoutKind.Sequential, Size = UnionData.BYTE_COUNT)]");
                p.PrintLine("public readonly struct UnionData");
                p.OpenScope();
                {
                    p.PrintLine("/// <summary>");
                    p.PrintLine("/// Size of <see cref=\"long\"/> in bytes.");
                    p.PrintLine("/// </summary>");
                    p.PrintLine($"internal const int SIZE_OF_LONG = {SIZE_OF_LONG};");
                    p.PrintEndLine();

                    p.PrintLine($"internal const int MAX_LONG_COUNT = {MAX_LONG_COUNT};");
                    p.PrintLine($"internal const int MAX_INT_COUNT = MAX_LONG_COUNT * 2;");
                    p.PrintLine($"internal const int MAX_BYTE_COUNT = MAX_LONG_COUNT * SIZE_OF_LONG;");
                    p.PrintEndLine();

                    p.Print($"#if (UNION_{MAX_BYTE_COUNT}_BYTES || UNION_{MAX_LONG_COUNT}_LONGS || UNION_{MAX_LONG_COUNT * 2}_INTS)").PrintEndLine();

                    for (var size = MAX_BYTE_COUNT; size >= SIZE_OF_LONG; size -= SIZE_OF_LONG)
                    {
                        var count = size / SIZE_OF_LONG;

                        p.PrintEndLine();
                        p.PrintLine("/// <summary>");
                        p.PrintLine($"/// Equals to {count * 2} × <see cref=\"int\"/>, or {count} × <see cref=\"long\"/>");
                        p.PrintLine("/// </summary>");
                        p.PrintLine($"public const int BYTE_COUNT = {count} * SIZE_OF_LONG;");
                        p.PrintEndLine();

                        var nextSize = size - SIZE_OF_LONG;

                        if (nextSize < SIZE_OF_LONG)
                        {
                            break;
                        }

                        var nextCount = nextSize / SIZE_OF_LONG;

                        p.PrintSelect(
                              $"#else"
                            , $"#elif (UNION_{nextSize}_BYTES || UNION_{nextCount * 2}_INTS || UNION_{nextCount}_LONGS)"
                            , nextSize == SIZE_OF_LONG
                        ).PrintEndLine();
                    }

                    p.Print("#endif").PrintEndLine();
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();

            context.OverrideFolderPath(outputPath);
            context.AddCode($"UnionData.gen.cs", p.Result);
        }
    }
}

#endif
