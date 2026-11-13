using BlackHellHeim.Aplicacion.Arte;
using BlackHellHeim.Infraestructura.Almacen;
using Testcontainers.Azurite;
using Xunit;

namespace BlackHellHeim.Pruebas.Arte;

/// <summary>Adaptador contra Azurite real en contenedor.</summary>
public class AlmacenBlobAzurePruebas : IAsyncLifetime
{
    private readonly AzuriteContainer _azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0").WithCommand("--skipApiVersionCheck").Build();

    public Task InitializeAsync() => _azurite.StartAsync();

    public Task DisposeAsync() => _azurite.DisposeAsync().AsTask();

    private AlmacenBlobAzure Crear() => new(new OpcionesAlmacen { CadenaConexion = _azurite.GetConnectionString(), Contenedor = "arte" });

    [Fact]
    public async Task GuardaExisteYElimina()
    {
        var almacen = Crear();
        var url = await almacen.GuardarAsync("prueba.png", "image/png", new MemoryStream([1, 2, 3]), default);

        Assert.EndsWith("/arte/prueba.png", url.AbsoluteUri);
        Assert.True(await almacen.ExisteAsync("prueba.png", default));
        await almacen.EliminarAsync("prueba.png", default);
        Assert.False(await almacen.ExisteAsync("prueba.png", default));
        await almacen.EliminarAsync("prueba.png", default); // idempotente
    }

    [Fact]
    public async Task SinCadenaOSinServicioLanza503()
    {
        await Assert.ThrowsAsync<AlmacenNoDisponibleException>(() =>
            new AlmacenBlobAzure(new OpcionesAlmacen()).ExisteAsync("x", default));

        const string apagado = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;BlobEndpoint=http://127.0.0.1:1/devstoreaccount1;AccountKey=AAAA";
        await Assert.ThrowsAsync<AlmacenNoDisponibleException>(() =>
            new AlmacenBlobAzure(new OpcionesAlmacen { CadenaConexion = apagado }).ExisteAsync("x", default));
    }
}
