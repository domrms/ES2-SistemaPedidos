namespace ES2_SistemaPedidos.Api;

public sealed class Resultado<TSucesso>
{
    private Resultado(TSucesso? value, RespostaErroValidacao? error)
    {
        Valor = value;
        Erro = error;
    }

    private TSucesso? Valor { get; }

    private RespostaErroValidacao? Erro { get; }

    public static Resultado<TSucesso> Success(TSucesso value)
    {
        return new Resultado<TSucesso>(value, null);
    }

    public static Resultado<TSucesso> ValidationFailed(RespostaErroValidacao error)
    {
        return new Resultado<TSucesso>(default, error);
    }

    public TResultado Match<TResultado>(Func<TSucesso, TResultado> onSuccess,
        Func<RespostaErroValidacao, TResultado> onValidationFailed)
    {
        return Erro is null ? onSuccess(Valor!) : onValidationFailed(Erro);
    }
}
