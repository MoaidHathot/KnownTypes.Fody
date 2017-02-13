using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using KnownType.Fody;
using KnownTypes.Fody;
using Mono.Cecil;
using NUnit.Framework;

[TestFixture]
public class WeaverTests
{
    private Assembly _assembly;
    private string _newAssemblyPath;
    private string _assemblyPath;

    [TestFixtureSetUp]
    public void Setup()
    {
        var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
        _assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");

        _newAssemblyPath = _assemblyPath.Replace(".dll", "2.dll");
        File.Copy(_assemblyPath, _newAssemblyPath, true);

        var moduleDefinition = ModuleDefinition.ReadModule(_newAssemblyPath);

        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = moduleDefinition
        };

        weavingTask.Execute();
        moduleDefinition.Write(_newAssemblyPath);

        _assembly = Assembly.LoadFile(_newAssemblyPath);
    }

    [Test]
    public void ValidateKnownTypeAttributesAreInjected()
    {
        var type = _assembly.GetType("AssemblyToProcess.A");
        var attributes = type.GetCustomAttributes(typeof(KnownTypeAttribute), false);

        Assert.AreEqual(2, attributes.Length);
    }

    [Test]
    public void ValidateKnowsDeriveTypesAttributeRemoved()
    {
        var type = _assembly.GetType("AssemblyToProcess.A");
        var attributes = type.GetCustomAttributes(typeof(KnowsDeriveTypesAttribute), false);

        Assert.AreEqual(0, attributes.Length);
    }

#if(DEBUG)
    [Test]
    public void PeVerify()
    {
        Verifier.Verify(_assemblyPath,_newAssemblyPath);
    }
#endif
}