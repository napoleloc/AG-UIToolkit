#if UNITY_EDITOR && ANNULUS_CODEGEN && ENCOSY_MVVM_ADAPTERS_GENERATOR

using System;
using System.Globalization;
using EncosyTower.CodeGen;
using UnityCodeGen;

namespace EncosyTower.Editor.Mvvm.ViewBinding.Adapters.ResourceKeys
{
    [Generator]
    internal class AnyResourcesAdaptersGenerator : ICodeGenerator
    {
        public void Execute(GeneratorContext context)
        {
            var nameofGenerator = nameof(AnyResourcesAdaptersGenerator);

            if (CodeGenAPI.TryGetOutputFolderPath(nameofGenerator, out var outputPath) == false)
            {
                context.OverrideFolderPath("Assets");
                return;
            }

            var p = Printer.DefaultLarge;
            p.PrintAutoGeneratedBlock(nameofGenerator);
            p.PrintEndLine();

            p.PrintLine(@"#pragma warning disable

using System;
using EncosyTower.Annotations;
using EncosyTower.ResourceKeys;
using EncosyTower.Unions.Converters;
");

            p.PrintEndLine();
            p.PrintLine("namespace EncosyTower.Mvvm.ViewBinding.Adapters.ResourceKeys");
            p.OpenScope();
            {
                var unityTypes = MvvmCodeGenAPI.UnityTypes.AsSpan();
                var serializables = new bool[] { false };

                for (var i = 0; i < unityTypes.Length; i++)
                {
                    var type = unityTypes[i];
                    var name = type.Name;
                    var typeName = type.FullName;

                    p.PrintBeginLine("#region    ").PrintEndLine(typeName.ToUpper(CultureInfo.InvariantCulture));
                    p.PrintBeginLine("#endregion ").Print('=', typeName.Length).PrintEndLine();
                    p.PrintEndLine();

                    foreach (var serializable in serializables)
                    {
                        var subType = serializable ? ".Serializable" : "";
                        var affix = serializable ? "Serializable" : "";

                        p.PrintLine($"[Serializable]");
                        p.PrintLine($"[Label(\"ResourceKey{subType}<{typeName}>.Load()\", \"Default\")]");
                        p.PrintLine($"[Adapter(sourceType: typeof(ResourceKey{subType}<{typeName}>), destType: typeof({typeName}), order: 0)]");
                        p.PrintLine($"public sealed class ResourceKey{affix}To{name}Adapter : ResourceKey{affix}Adapter<{typeName}>");
                        p.OpenScope();
                        {
                            p.PrintBeginLine($"public ")
                                .Print($"ResourceKey{affix}To{name}Adapter")
                                .Print("() : base(CachedUnionConverter<")
                                .Print(typeName)
                                .PrintEndLine(">.Default) { }");
                        }
                        p.CloseScope();
                        p.PrintEndLine();
                    }

                    p.PrintLine($"[Serializable]");
                    p.PrintLine($"[Label(\"Resources.Load<{typeName}>(string)\", \"Default\")]");
                    p.PrintLine($"[Adapter(sourceType: typeof(string), destType: typeof({typeName}), order: 2)]");
                    p.PrintLine($"public sealed class ResourceStringTo{name}Adapter : ResourceStringAdapter<{typeName}>");
                    p.OpenScope();
                    {
                        p.PrintBeginLine($"public ")
                            .Print($"ResourceStringTo{name}Adapter")
                            .Print("() : base(CachedUnionConverter<")
                            .Print(typeName)
                            .PrintEndLine(">.Default) { }");
                    }
                    p.CloseScope();
                    p.PrintEndLine();
                }
            }
            p.CloseScope();
            p.PrintEndLine();

            context.OverrideFolderPath(outputPath);
            context.AddCode($"AnyResourcesAdapters.gen.cs", p.Result);
        }
    }
}

#endif
