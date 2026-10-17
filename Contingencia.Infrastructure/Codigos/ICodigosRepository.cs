namespace Contingencia.Infrastructure.Codigos;

public record CodigoFila(string Codigo, string Descripcion);

public interface ICodigosRepository
{
    // @tipoServicio: código corto real de DQS_MIP_CODIGOS.TipoServicio (P=Procedimiento,
    // D=Dispositivos médicos, N=Producto Nutricional, S=Servicios, M=Medicamento).
    // @ciaCatalogo: valor de DQS_MIP_CODIGOS.CIA a consultar (parámetro CODIGOS_CATALOGO de la CIA; por defecto "NEPS").
    Task<IReadOnlyList<CodigoFila>> BuscarAsync(string ciaCatalogo, string tipoServicio, string prefijo, int cantidad);
}
