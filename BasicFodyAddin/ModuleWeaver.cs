using System.Linq;
using KnownTypes.Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace KnownType.Fody
{
    public class ModuleWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            foreach (var type in ModuleDefinition.GetTypes().Where(HasKnownDeriveTypesAttribute))
            {
                AddKnownTypeAttributes(ModuleDefinition, type);
                RemoveKnowsDeriveTypesAttribute(type);
            }
        }

        //A helper method
        //For filtering the types without the KnowsDeriveTypes Attribute.
        bool HasKnownDeriveTypesAttribute(TypeDefinition type)
            => type.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == typeof(KnowsDeriveTypesAttribute).FullName);

        void AddKnownTypeAttributes(ModuleDefinition module, TypeDefinition baseType)
        {
            //Locate derived types
            var derivedTypes = GetDerivedTypes(module, baseType);

            //Gets a TypeDefinition representing the KnownTypeAttribute type.
            var knownTypeAttributeTypeDefinition = GetTypeDefinition(module, "System.Runtime.Serialization", "KnownTypeAttribute");

            //Gets the constructor for the KnownTypeAttribute type.
            var knownTypeConstrcutor = GetConstructorForKnownTypeAttribute(module, knownTypeAttributeTypeDefinition);


            //Adds KnownType attribute for each derive type
            foreach (var derivedType in derivedTypes)
            {
                var attribute = new CustomAttribute(knownTypeConstrcutor);
                attribute.ConstructorArguments.Add(new CustomAttributeArgument(knownTypeConstrcutor.Parameters.First().ParameterType, derivedType));

                baseType.CustomAttributes.Add(attribute);
            }
        }

        //A helper method. Given a module and a baes type:
        //It returns all derived types of that base type.
        TypeDefinition[] GetDerivedTypes(ModuleDefinition module, TypeDefinition baseType)
            => module.GetTypes()
                .Where(type => type.BaseType?.FullName == baseType.FullName)
                .ToArray();

        //A helper method. Given an assembly and a type name:
        //It returns a TypeDefinision for that type.
        TypeDefinition GetTypeDefinition(ModuleDefinition module, string assemblyName, string typeName)
            => module.AssemblyResolver
                .Resolve(assemblyName)
                .MainModule.Types.Single(type => type.Name == typeName);

        //A helper method. Given a module and type definition  for the KnownType Attribute:
        //It returns the constructor for the attribute.
        MethodReference GetConstructorForKnownTypeAttribute(ModuleDefinition module, TypeDefinition knownTypeAttributeTypeDefinition)
        {
            var constructorMethodToImport = knownTypeAttributeTypeDefinition.GetConstructors().Single(ctor => 1 == ctor.Parameters.Count && "System.Type" == ctor.Parameters[0].ParameterType.FullName);

            return module.Import(constructorMethodToImport);
        }

        void RemoveKnowsDeriveTypesAttribute(TypeDefinition baseType)
        {
            var foundAttribute = baseType.CustomAttributes
                .Single(attribute => attribute.AttributeType.FullName == typeof(KnowsDeriveTypesAttribute).FullName);

            baseType.CustomAttributes.Remove(foundAttribute);
        }
    }
}