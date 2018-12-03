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


Func<int, int, int> addFunc = (a, b) => a + b;
            
var method = addFunc.Method;
var target = addFunc.Target;

Func<object[], object> invokable = CILInvoke.Convert(method, target);            

// Creating args during call.
Time("CILInvoke.Invoke", 1_000_000, () => invokable(new object[] { 5, 7 }));
Time("MethodInvoke.Invoke", 1_000_000, () => method.Invoke(target, new object[] { 5, 7 }));
Time("Delegate.DynamicInvoke", 1_000_000, () => addFunc.DynamicInvoke(new object[] { 5, 7 }));

object[] prefilledArgs = { 7, 8 };

Console.WriteLine();
// Storing args somewhere else.
Time("CILInvoke.Invoke", 1_000_000, () => invokable(prefilledArgs));
Time("MethodInvoke.Invoke", 1_000_000, () => method.Invoke(target, prefilledArgs));
Time("Delegate.DynamicInvoke", 1_000_000, () => addFunc.DynamicInvoke(prefilledArgs));
```

### Results (ms/1mil):
```
CILInvoke.Invoke: 44.1909
MethodInvoke.Invoke: 221.4021
Delegate.DynamicInvoke: 361.603

CILInvoke.Invoke: 21.9491
MethodInvoke.Invoke: 197.5116
Delegate.DynamicInvoke: 352.7306
```
