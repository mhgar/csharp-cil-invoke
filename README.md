# csharp-cil-invoke
A faster CIL emission replacement for MethodInfo.Invoke(object[]). It provides run-time speed in exchange for the upfront cost of emitting and caching. Since it's written using ILGenerator.Emit() it could always use a cleanup, but it works fine.

### Testing code
```csharp
void Time(string name, int count, Action action) {
    var time = Stopwatch.StartNew();
    for (int i = 0; i < count; i++) action();
    time.Stop();

    Console.WriteLine($"{name}: {time.Elapsed.TotalMilliseconds}");
}


var thing = new Thing();
var add = typeof(Thing).GetMethod("Add"); // Add is defined as (int a, int b) => a + b           

var cilInvoke = CILInvoke.Convert(add, thing);

object[] argv = { 3, 4 };

Time("CIL emit invoke", 1_000_000, () => cilInvoke(argv));
Time("Reflection invoke", 1_000_000, () => add.Invoke(thing, argv));
```

### Speed of 1,000,000 calls in ms (i5-4670k 4.4ghz):
```
dotnet run
CIL emit invoke: 23.6595
Reflection invoke: 213.8327
```
