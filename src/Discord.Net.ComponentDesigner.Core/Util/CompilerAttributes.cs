#if NETSTANDARD

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName)
        {
            BuilderType = builderType;
            MethodName = methodName;
        }

        public Type BuilderType { get; }
        public string MethodName { get; }
    }
    
    internal sealed class IsExternalInit : Attribute;

    internal sealed class CompilerFeatureRequiredAttribute(string s) : Attribute;

    internal sealed class RequiredMemberAttribute : Attribute;
}

namespace System.Diagnostics.CodeAnalysis
{
    internal sealed class MemberNotNullAttribute(params string[] members) : Attribute
    {
        public string[] Members { get; } = members;

        public MemberNotNullAttribute(string member) : this([member])
        {
        }
    }

    internal sealed class MemberNotNullWhenAttribute(bool returnValue, params string[] members) : Attribute
    {
        public string[] Members { get; } = members;
        public bool ReturnValue { get; } = returnValue;

        public MemberNotNullWhenAttribute(bool returnValue, string member) : this(returnValue, [member])
        {
        }
    }

    internal sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }

    internal sealed class NotNullIfNotNullAttribute(string parameterName) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }
}
#endif