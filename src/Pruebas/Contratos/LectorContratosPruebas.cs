using Xunit;

namespace BlackHellHeim.Pruebas.Contratos;

public class LectorContratosPruebas
{
    [Fact]
    public void LeeTodosLosContratosDeLasSpecs()
    {
        var raiz = LectorContratos.BuscarRaiz(AppContext.BaseDirectory);

        var contratos = LectorContratos.LeerTodos(raiz);

        Assert.Contains("002-estructura-base-api", contratos.Keys);
        Assert.Contains("002b-casos-de-uso-mediator", contratos.Keys);
        Assert.Contains("003-sql-server-efcore", contratos.Keys);
        Assert.Contains("004-openapi-scalar", contratos.Keys);
        Assert.StartsWith("3.1", contratos["004-openapi-scalar"]["openapi"]!.ToString());
    }

    [Fact]
    public void ConvierteTiposEscalaresDelYaml()
    {
        var json = LectorContratos.ConvertirYaml("a: 1\nb: true\nc: '200'\nd: texto\n");

        Assert.Equal(1L, json["a"]!.GetValue<long>());
        Assert.True(json["b"]!.GetValue<bool>());
        Assert.Equal("200", json["c"]!.GetValue<string>());
        Assert.Equal("texto", json["d"]!.GetValue<string>());
    }

    [Fact]
    public void FallaConUnMensajeClaroSiNoEncuentraLaRaiz()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() => LectorContratos.BuscarRaiz(Path.GetTempPath()));

        Assert.Contains("BlackHellHeim.sln", excepcion.Message);
    }
}
