using System.Collections;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;
using Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Application.Tests.Configuracion;

public sealed class RegistroAplicacionTests
{
    [Fact]
    public void Debe_RegistrarDespachadoresYValidadores()
    {
        var servicios = new ColeccionServicios();

        servicios.AgregarAplicacion();

        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandDispatcher));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IQueryDispatcher));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<EmitirOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<AceptarOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<RechazarOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<AnularOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<ExpirarOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<CargarCompromisoBecarioCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<RevisarCompromisoBecarioCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<ConsultarBecasActivasQuery>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<ObtenerOfertaQuery>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IValidator<ObtenerBecaQuery>));
    }

    [Fact]
    public void Debe_RegistrarTodosLosHandlersDelEnsamblado()
    {
        var servicios = new ColeccionServicios();

        servicios.AgregarAplicacion();

        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<EmitirOfertaBecaCommand, OfertaEmitidaDto>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<AceptarOfertaBecaCommand, OfertaRespondidaDto>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<RechazarOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<AnularOfertaBecaCommand>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<ExpirarOfertaBecaCommand, bool>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<CargarCompromisoBecarioCommand, CompromisoCargadoDto>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<RevisarCompromisoBecarioCommand, CompromisoRevisadoDto>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IQueryHandler<ObtenerOfertaQuery, OfertaBecaDto>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IQueryHandler<ObtenerBecaQuery, BecaDto>));
        servicios.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IQueryHandler<ConsultarBecasActivasQuery, IReadOnlyList<BecaActivaDto>>));
    }

    private sealed class ColeccionServicios : IServiceCollection
    {
        private readonly List<ServiceDescriptor> _descriptores = [];

        public ServiceDescriptor this[int index]
        {
            get => _descriptores[index];
            set => _descriptores[index] = value;
        }

        public int Count => _descriptores.Count;

        public bool IsReadOnly => false;

        public void Add(ServiceDescriptor item) => _descriptores.Add(item);

        public void Clear() => _descriptores.Clear();

        public bool Contains(ServiceDescriptor item) => _descriptores.Contains(item);

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
            => _descriptores.CopyTo(array, arrayIndex);

        public IEnumerator<ServiceDescriptor> GetEnumerator()
            => _descriptores.GetEnumerator();

        public int IndexOf(ServiceDescriptor item) => _descriptores.IndexOf(item);

        public void Insert(int index, ServiceDescriptor item)
            => _descriptores.Insert(index, item);

        public bool Remove(ServiceDescriptor item) => _descriptores.Remove(item);

        public void RemoveAt(int index) => _descriptores.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
