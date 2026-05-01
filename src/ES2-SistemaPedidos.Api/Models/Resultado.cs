namespace ES2_SistemaPedidos.Api;

public sealed class Resultado<TSucesso>
{
    private Resultado(TSucesso? valor, RespostaErroValidacao? erro)
    {
        Valor = valor;
        Erro = erro;
    }

    private TSucesso? Valor { get; }

    private RespostaErroValidacao? Erro { get; }

    public static Resultado<TSucesso> Success(TSucesso valor) => new(valor, null);

    public static Resultado<TSucesso> ValidationFailed(RespostaErroValidacao erro) => new(default, erro);

    public TResultado Match<TResultado>(Func<TSucesso, TResultado> quandoSucesso, Func<RespostaErroValidacao, TResultado> quandoValidacaoFalhou)
    {
        return Erro is null ? quandoSucesso(Valor!) : quandoValidacaoFalhou(Erro);
    }
}
