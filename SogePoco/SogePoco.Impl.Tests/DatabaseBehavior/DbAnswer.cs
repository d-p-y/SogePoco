using System;
using System.Collections.Generic;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public record DbAnswer() {
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(200);
    
    public record Started() : DbAnswer();
    public record Error(string Msg) : DbAnswer();
    public record Ended() : DbAnswer();
    public record Ok() : DbAnswer();
    public record OkSingleValue(object? Val) : DbAnswer();
    public record OkSingleRow(IDictionary<string,object?> Row) : DbAnswer();
    public record Timeouted() : DbAnswer();
}
