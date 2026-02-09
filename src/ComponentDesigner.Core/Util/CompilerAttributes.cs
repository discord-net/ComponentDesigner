#if NETSTANDARD

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName)
        {
            BuilderType = builderType;
            MethodName = methodName;
        }

        public Type BuilderType { get; }
        public string MethodName { get; }
    }
    
    public sealed class IsExternalInit : Attribute;

    public sealed class CompilerFeatureRequiredAttribute(string s) : Attribute;

    public sealed class RequiredMemberAttribute : Attribute;
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class MemberNotNullAttribute(params string[] members) : Attribute
    {
        public string[] Members { get; } = members;

        public MemberNotNullAttribute(string member) : this([member])
        {
        }
    }

    public sealed class MemberNotNullWhenAttribute(bool returnValue, params string[] members) : Attribute
    {
        public string[] Members { get; } = members;
        public bool ReturnValue { get; } = returnValue;

        public MemberNotNullWhenAttribute(bool returnValue, string member) : this(returnValue, [member])
        {
        }
    }

    public sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }
    
    public sealed class MaybeNullAttribute() : Attribute
    {
    }

    public sealed class NotNullIfNotNullAttribute(string parameterName) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }
}
#endif