namespace Contingencia.Infrastructure.Juntas;

public interface IActaPdfBuilder
{
    byte[] Generar(DatosActaPdf datos);
}
