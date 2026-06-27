using System.Diagnostics.CodeAnalysis;
using ES2_SistemaPedidos.LambdaConsumerSQS;

Console.WriteLine("Lambda Consumer SQS configurado.");
Console.WriteLine(
    "Handler: ES2-SistemaPedidos.LambdaConsumerSQS::ES2_SistemaPedidos.LambdaConsumerSQS.Function::FunctionHandler");
Console.WriteLine("Function entrypoint: " + typeof(Function).FullName);

[ExcludeFromCodeCoverage]
public partial class Program;