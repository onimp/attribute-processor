using System;

namespace AttributeProcessor.Annotations;

[AttributeUsage(AttributeTargets.Class)]
public class ConditionalInvocationAttribute : Attribute {

    public Type DeclaringType { get; }
    public string MethodName { get; }

    public ConditionalInvocationAttribute(Type declaringType, string methodName) {
        DeclaringType = declaringType;
        MethodName = methodName;
    }

}
