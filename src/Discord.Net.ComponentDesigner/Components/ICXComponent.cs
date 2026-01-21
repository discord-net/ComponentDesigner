using System.Collections.Immutable;

namespace Discord;

public interface IBuildableCXComponent<TSelf, TResult> : ICXComponent<TSelf>
    where TSelf : IBuildableCXComponent<TSelf, TResult>
{
    TResult Build();

    static abstract TSelf From(TResult result);
    
    static abstract implicit operator TResult(TSelf self);
    static abstract implicit operator TSelf(TResult result);
}

public interface ICXComponent<out TSelf> : ICXComponent
    where TSelf : ICXComponent<TSelf>
{
    static abstract TSelf Empty { get; }
}

public interface ICXComponent
{
    IReadOnlyList<IMessageComponentBuilder> Builders { get; }
    IReadOnlyList<IMessageComponent> Components { get; }
}