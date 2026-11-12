using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace BlackHellHeim.Pruebas.Infraestructura.Persistencia;

/// <summary>
/// Contenedor desechable de SQL Server para las pruebas de persistencia. La contraseña se
/// genera en tiempo de ejecución y nunca se versiona.
/// </summary>
public sealed class ContenedorSqlServer : IAsyncLifetime
{
    private readonly string _contrasena = "Aa1!" + Guid.NewGuid().ToString("N");
    private readonly MsSqlContainer _contenedor;

    public ContenedorSqlServer()
    {
        _contenedor = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(_contrasena)
            .Build();
    }

    public string CadenaMaster => _contenedor.GetConnectionString();

    public Task InitializeAsync() => _contenedor.StartAsync();

    public async Task DisposeAsync() => await _contenedor.DisposeAsync();

    /// <summary>Crea una base vacía con nombre único y devuelve su cadena de conexión.</summary>
    public async Task<string> CrearBaseAsync()
    {
        var nombre = "BHH_" + Guid.NewGuid().ToString("N");
        await using (var conexion = new SqlConnection(CadenaMaster))
        {
            await conexion.OpenAsync();
            await using var comando = conexion.CreateCommand();
            comando.CommandText = $"CREATE DATABASE [{nombre}]";
            await comando.ExecuteNonQueryAsync();
        }

        return new SqlConnectionStringBuilder(CadenaMaster) { InitialCatalog = nombre }.ConnectionString;
    }

    public async Task<T> ConsultarEscalarAsync<T>(string cadena, string sql)
    {
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        var valor = await comando.ExecuteScalarAsync();
        return valor is null or DBNull ? default! : (T)Convert.ChangeType(valor, typeof(T));
    }

    /// <summary>Congela el contenedor: acepta conexiones TCP pero no responde.</summary>
    public Task PausarAsync() => EjecutarDockerAsync("pause");

    public Task ReanudarAsync() => EjecutarDockerAsync("unpause");

    private async Task EjecutarDockerAsync(string accion)
    {
        using var proceso = Process.Start(new ProcessStartInfo("docker", $"{accion} {_contenedor.Id}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        await proceso.WaitForExitAsync();
        if (proceso.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker {accion} falló: {await proceso.StandardError.ReadToEndAsync()}");
        }
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionSqlServer : ICollectionFixture<ContenedorSqlServer>
{
    public const string Nombre = "SqlServer";
}
