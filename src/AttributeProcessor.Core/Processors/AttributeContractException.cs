using System;

namespace AttributeProcessor.Core.Processors;

public class AttributeContractException : Exception {
    public AttributeContractException(string message) : base(message) { }
}
