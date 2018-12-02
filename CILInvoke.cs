using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Wander {
    public static class CILInvoke {
        readonly static Dictionary<(MethodInfo, object), Func<object[], object>> cache = 
            new Dictionary<(MethodInfo, object), Func<object[], object>>();

        public static Func<object[], object> Convert(MethodInfo method, object target = null) {
            // Instead of caching it might be easier to try grab the DynamicMethod out of the target object again.
            var cacheKey = (method, target);
            if (cache.ContainsKey(cacheKey)) {
                return cache[cacheKey];
            }

            var dynamicMethod = new DynamicMethod(
                method.Name + "_DYN",
                typeof(object),
                target != null ? new [] { typeof(object), typeof(object[]) } : new [] { typeof(object[]) },
                target.GetType()
            );

            var cil = dynamicMethod.GetILGenerator();
            var arrayLength = typeof(object[]).GetMethod("get_Length");
            var methodParams = method.GetParameters().Select((x, i) => (type:x.ParameterType, index:i)).ToList();
            var returnType = method.ReturnType;
            var exception = typeof(ArgumentException).GetConstructor(new [] {typeof(string)});
            var main = cil.DefineLabel();
            var isStatic = target == null;
            var hasReturn = returnType != typeof(void);       

            // Check the size of the input array
            cil.Emit(isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            cil.EmitCall(OpCodes.Call, arrayLength, null);
            cil.Emit(OpCodes.Ldc_I4, methodParams.Count);
            cil.Emit(OpCodes.Beq, main);
            cil.Emit(OpCodes.Ldstr, "Invalid number of arguments.");
            cil.Emit(OpCodes.Newobj, exception);
            cil.Emit(OpCodes.Throw);

            cil.MarkLabel(main);

            // Load the instance if one exists.
            if (!isStatic) cil.Emit(OpCodes.Ldarg_0);

            // Load the arguments onto the stack, unbox them if they're primitives.
            foreach (var param in methodParams) {
                cil.Emit(isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                cil.Emit(OpCodes.Ldc_I4, param.index);
                cil.Emit(OpCodes.Ldelem_Ref);
                if (param.type.IsValueType) {
                    cil.Emit(OpCodes.Unbox_Any, param.type);
                }
            }

            // Call the method   
            cil.EmitCall(OpCodes.Call, method, null);

            // Return
            if (hasReturn) {
                if (returnType.IsValueType) {
                    cil.Emit(OpCodes.Box, returnType);
                }
            } else {
                cil.Emit(OpCodes.Ldnull);                
            }
            cil.Emit(OpCodes.Ret);

            var del = (Func<object[], object>) dynamicMethod.CreateDelegate(typeof(Func<object[], object>), target);
            cache.Add(cacheKey, del);

            return del;
        }
    }
}
