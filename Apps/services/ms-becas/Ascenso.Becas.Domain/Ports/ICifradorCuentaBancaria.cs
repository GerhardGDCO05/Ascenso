namespace Ascenso.Becas.Domain.Ports;

public interface ICifradorCuentaBancaria
{
    string Cifrar(string numeroCuenta);
}
