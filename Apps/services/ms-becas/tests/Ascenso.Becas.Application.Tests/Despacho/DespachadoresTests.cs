using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace Ascenso.Becas.Application.Tests.Despacho;

public sealed class DespachadoresTests
{
    [Fact]
    public async Task Debe_DespacharUnComandoYDevolverSuResultado()
    {
        var comando = new ComandoPrueba(Guid.CreateVersion7());
        var handler = Substitute.For<ICommandHandler<ComandoPrueba, int>>();
        handler.HandleAsync(comando, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(7));
        var proveedor = CrearProveedor();
        proveedor.GetService(typeof(ICommandHandler<ComandoPrueba, int>)).Returns(handler);

        var resultado = await new CommandDispatcher(proveedor)
            .EnviarAsync<ComandoPrueba, int>(comando);

        resultado.Should().Be(7);
        await handler.Received(1).HandleAsync(comando, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_DespacharUnComandoSinResultado()
    {
        var comando = new ComandoSinResultado(Guid.CreateVersion7());
        var handler = Substitute.For<ICommandHandler<ComandoSinResultado>>();
        var proveedor = CrearProveedor();
        proveedor.GetService(typeof(ICommandHandler<ComandoSinResultado>)).Returns(handler);

        await new CommandDispatcher(proveedor).EnviarAsync(comando);

        await handler.Received(1).HandleAsync(comando, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ValidarAntesDeDespacharYNoInvocarElHandler()
    {
        var comando = new ComandoPrueba(Guid.Empty);
        var validador = Substitute.For<IValidator<ComandoPrueba>>();
        validador.ValidateAsync(Arg.Any<ComandoPrueba>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(
            [
                new ValidationFailure(nameof(ComandoPrueba.Id), "El identificador es obligatorio.")
            ])));
        var handler = Substitute.For<ICommandHandler<ComandoPrueba, int>>();
        var proveedor = CrearProveedor();
        proveedor.GetService(typeof(IValidator<ComandoPrueba>)).Returns(validador);
        proveedor.GetService(typeof(ICommandHandler<ComandoPrueba, int>)).Returns(handler);

        var accion = () => new CommandDispatcher(proveedor)
            .EnviarAsync<ComandoPrueba, int>(comando);

        var excepcion = await accion.Should().ThrowAsync<ValidacionAplicacionException>();

        excepcion.Which.Codigo.Should().Be("SOLICITUD_INVALIDA");
        excepcion.Which.Errores.Should().ContainKey(nameof(ComandoPrueba.Id));
        await handler.DidNotReceiveWithAnyArgs()
            .HandleAsync(default!, default);
    }

    [Fact]
    public async Task Debe_FallarConUnErrorClaro_Cuando_NoExisteElHandlerDelComando()
    {
        var proveedor = CrearProveedor();

        var accion = () => new CommandDispatcher(proveedor)
            .EnviarAsync<ComandoPrueba, int>(new ComandoPrueba(Guid.CreateVersion7()));

        var excepcion = await accion.Should().ThrowAsync<HandlerNoEncontradoException>();

        excepcion.Which.TipoSolicitud.Should().Be<ComandoPrueba>();
        excepcion.Which.Codigo.Should().Be("HANDLER_NO_ENCONTRADO");
        excepcion.Which.EstadoHttp.Should().Be(500);
    }

    [Fact]
    public async Task Debe_DespacharUnaConsultaYDevolverSuResultado()
    {
        var consulta = new ConsultaPrueba(Guid.CreateVersion7());
        var handler = Substitute.For<IQueryHandler<ConsultaPrueba, string>>();
        handler.HandleAsync(consulta, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("resultado"));
        var proveedor = CrearProveedor();
        proveedor.GetService(typeof(IQueryHandler<ConsultaPrueba, string>)).Returns(handler);

        var resultado = await new QueryDispatcher(proveedor)
            .ConsultarAsync<ConsultaPrueba, string>(consulta);

        resultado.Should().Be("resultado");
        await handler.Received(1).HandleAsync(consulta, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Debe_ValidarAntesDeConsultarYNoInvocarElHandler()
    {
        var consulta = new ConsultaPrueba(Guid.Empty);
        var validador = Substitute.For<IValidator<ConsultaPrueba>>();
        validador.ValidateAsync(Arg.Any<ConsultaPrueba>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(
            [
                new ValidationFailure(nameof(ConsultaPrueba.Id), "El identificador es obligatorio.")
            ])));
        var handler = Substitute.For<IQueryHandler<ConsultaPrueba, string>>();
        var proveedor = CrearProveedor();
        proveedor.GetService(typeof(IValidator<ConsultaPrueba>)).Returns(validador);
        proveedor.GetService(typeof(IQueryHandler<ConsultaPrueba, string>)).Returns(handler);

        var accion = () => new QueryDispatcher(proveedor)
            .ConsultarAsync<ConsultaPrueba, string>(consulta);

        await accion.Should().ThrowAsync<ValidacionAplicacionException>();
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
    }

    [Fact]
    public async Task Debe_FallarConUnErrorClaro_Cuando_NoExisteElHandlerDeLaConsulta()
    {
        var proveedor = CrearProveedor();

        var accion = () => new QueryDispatcher(proveedor)
            .ConsultarAsync<ConsultaPrueba, string>(new ConsultaPrueba(Guid.CreateVersion7()));

        var excepcion = await accion.Should().ThrowAsync<HandlerNoEncontradoException>();

        excepcion.Which.TipoSolicitud.Should().Be<ConsultaPrueba>();
    }

    private static IServiceProvider CrearProveedor()
        => Substitute.For<IServiceProvider>();

    public sealed record ComandoPrueba(Guid Id) : ICommand<int>;

    public sealed record ComandoSinResultado(Guid Id) : ICommand;

    public sealed record ConsultaPrueba(Guid Id) : IQuery<string>;
}
