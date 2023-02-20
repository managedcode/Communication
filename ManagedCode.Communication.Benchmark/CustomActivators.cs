using System.Linq.Expressions;
using System.Reflection.Emit;

namespace ManagedCode.Communication.Benchmark;

public static class DynamicModuleLambdaCompiler
{
    public static Func<T> GenerateFactory<T>() where T : new()
    {
        Expression<Func<T>> expr = () => new T();
        NewExpression newExpr = (NewExpression)expr.Body;

        var method = new DynamicMethod(
            name: "lambda",
            returnType: newExpr.Type,
            parameterTypes: new Type[0],
            m: typeof(DynamicModuleLambdaCompiler).Module,
            skipVisibility: true);

        ILGenerator ilGen = method.GetILGenerator();
        // Constructor for value types could be null
        if (newExpr.Constructor != null)
        {
            ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
        }
        else
        {
            LocalBuilder temp = ilGen.DeclareLocal(newExpr.Type);
            ilGen.Emit(OpCodes.Ldloca, temp);
            ilGen.Emit(OpCodes.Initobj, newExpr.Type);
            ilGen.Emit(OpCodes.Ldloc, temp);
        }

        ilGen.Emit(OpCodes.Ret);

        return (Func<T>)method.CreateDelegate(typeof(Func<T>));
    }
    
    public static Func<object> GenerateFactory(Type type)
    {
        var method = new DynamicMethod(
            name: "lambda",
            returnType: type,
            parameterTypes: new Type[0],
            m: typeof(DynamicModuleLambdaCompiler).Module,
            skipVisibility: true);

        ILGenerator ilGen = method.GetILGenerator();
        // Constructor for value types could be null
        
        //if (newExpr.Constructor != null)
        //{
        //    ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
        //}
        //else
        {
            LocalBuilder temp = ilGen.DeclareLocal(type);
            ilGen.Emit(OpCodes.Ldloca, temp);
            ilGen.Emit(OpCodes.Initobj, type);
            ilGen.Emit(OpCodes.Ldloc, temp);
        }

        ilGen.Emit(OpCodes.Ret);

        return (Func<object>)method.CreateDelegate(typeof(Func<object>));
    }
}