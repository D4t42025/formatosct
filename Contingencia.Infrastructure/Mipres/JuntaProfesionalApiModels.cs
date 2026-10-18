using System.Text.Json.Serialization;

namespace Contingencia.Infrastructure.Mipres;

// DTOs de la API real del gobierno (wsmipres.sispro.gov.co/WSMIPRESNOPBS), tal como las
// deserializaba JuntaProfesional.aspx.vb con Newtonsoft.Json — mismos nombres de propiedad
// (las clases "JuntaProfesional"/"JUNTAS"/"MIPRES" y sus hijas, definidas dentro de
// ConsultaMIPRES.aspx.vb y JuntaProfesional.aspx.vb en el legado). Solo se incluyen los
// campos que el legado realmente lee — el resto del payload se ignora, no se pierde nada
// porque System.Text.Json descarta silenciosamente las propiedades no mapeadas.

public class JuntaProfesionalDetalle
{
    public string NoPrescripcion { get; set; } = "";
    public string FPrescripcion { get; set; } = "";
    public string TipoTecnologia { get; set; } = "";
    public string Consecutivo { get; set; } = "";
    public string EstJM { get; set; } = "";
    public string CodEntProc { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public string JustificacionTecnica { get; set; } = "";
    public string Modalidad { get; set; } = "";
    public string NoActa { get; set; } = "";
    public string FechaActa { get; set; } = "";
    public string FProceso { get; set; } = "";
    public string TipoIDPaciente { get; set; } = "";
    public string NroIDPaciente { get; set; } = "";
    public string CodEntJM { get; set; } = "";
}

// El wrapper JSON usa la llave "junta_profesional" (snake_case, distinto al resto de la
// API que es PascalCase) — confirmado por el nombre de propiedad exacto en el legado.
public class JuntaProfesionalApiItem
{
    [JsonPropertyName("junta_profesional")]
    public JuntaProfesionalDetalle? JuntaProfesional { get; set; }
}

public class MipresPrescripcion
{
    public MipresDatosPrescripcion? Prescripcion { get; set; }
    public List<MipresMedicamento> Medicamentos { get; set; } = [];
    public List<MipresProcedimiento> Procedimientos { get; set; } = [];
    public List<MipresDispositivo> Dispositivos { get; set; } = [];
    public List<MipresProductoNutricional> Productosnutricionales { get; set; } = [];
    public List<MipresServicioComplementario> ServiciosComplementarios { get; set; } = [];
}

public class MipresDatosPrescripcion
{
    // Fecha de la prescripción (solo fecha, hora siempre 00:00:00 en esta API) — se combina
    // con HPrescripcion (ver abajo) para la "Fecha y hora Expedición" del acta.
    public string FPrescripcion { get; set; } = "";
    // Pese al nombre (la "H" sugiere "Historia"), este campo trae la HORA real de la
    // prescripción (ej. "12:31:53") — confirmado contra la API en vivo. El legado lo usaba
    // por error como "Historia Clínica" (campo que esta API no expone en absoluto).
    public string HPrescripcion { get; set; } = "";
    public string CodHabIPS { get; set; } = "";
    public string NroIDIPS { get; set; } = "";
    public string CodDANEMunIPS { get; set; } = "";
    public string DirSedeIPS { get; set; } = "";
    public string TelSedeIPS { get; set; } = "";
    public string TipoIDProf { get; set; } = "";
    public string NumIDProf { get; set; } = "";
    public string PNProfS { get; set; } = "";
    public string SNProfS { get; set; } = "";
    public string PAProfS { get; set; } = "";
    public string SAProfS { get; set; } = "";
    public string RegProfS { get; set; } = "";
    public string TipoIDPaciente { get; set; } = "";
    public string NroIDPaciente { get; set; } = "";
    public string PNPaciente { get; set; } = "";
    public string SNPaciente { get; set; } = "";
    public string PAPaciente { get; set; } = "";
    public string SAPaciente { get; set; } = "";
    public string CodAmbAte { get; set; } = "";
    public string CodDxPpal { get; set; } = "";
}

public class MipresMedicamento
{
    public string TipoPrest { get; set; } = "";
    public string DescMedPrinAct { get; set; } = "";
    public string CodFF { get; set; } = "";
    public string Dosis { get; set; } = "";
    // Unidad de medida de la Dosis (ej. "0168"→mg), campo separado que la API SIEMPRE
    // entrega junto a Dosis — se traduce contra DQS_MIP_CRUZAR (Campo='DosisUM').
    public string DosisUM { get; set; } = "";
    public string CodVA { get; set; } = "";
    public string NoFAdmon { get; set; } = "";
    public string CodFreAdmon { get; set; } = "";
    public string IndEsp { get; set; } = "";
    public string CanTrat { get; set; } = "";
    public string DurTrat { get; set; } = "";
    public string IndRec { get; set; } = "";
    public string CantTotalF { get; set; } = "";
}

public class MipresProcedimiento
{
    public string TipoPrest { get; set; } = "";
    public string CodCUPS { get; set; } = "";
    public string IndRec { get; set; } = "";
    public string CanForm { get; set; } = "";
    public string CadaFreUso { get; set; } = "";
    public string CodFreUso { get; set; } = "";
    public string CodPerDurTrat { get; set; } = "";
    public string Cant { get; set; } = "";
    public string CantTotal { get; set; } = "";
}

public class MipresDispositivo
{
    public string TipoPrest { get; set; } = "";
    public string CodDisp { get; set; } = "";
    public string IndRec { get; set; } = "";
    public string CanForm { get; set; } = "";
    public string CadaFreUso { get; set; } = "";
    public string CodFreUso { get; set; } = "";
    public string CodPerDurTrat { get; set; } = "";
    public string Cant { get; set; } = "";
    public string CantTotal { get; set; } = "";
}

public class MipresProductoNutricional
{
    public string TipoPrest { get; set; } = "";
    // Pese al nombre, este campo no es una descripción legible sino un CÓDIGO que se
    // resuelve contra DQS_MIP_CRUZAR (Campo='DescProdNutr') — así lo usaba el legado.
    public string DescProdNutr { get; set; } = "";
    public string Dosis { get; set; } = "";
    // Unidad de medida de la Dosis, mismo catálogo que Medicamentos (Campo='DosisUM').
    public string DosisUM { get; set; } = "";
    public string CodViaAdmon { get; set; } = "";
    public string NoFAdmon { get; set; } = "";
    public string CodFreAdmon { get; set; } = "";
    public string IndEsp { get; set; } = "";
    public string CanTrat { get; set; } = "";
    public string DurTrat { get; set; } = "";
    public string IndRec { get; set; } = "";
    public string CantTotalF { get; set; } = "";
}

public class MipresServicioComplementario
{
    public string TipoPrest { get; set; } = "";
    public string CodSerComp { get; set; } = "";
    public string DescSerComp { get; set; } = "";
    public string IndRec { get; set; } = "";
    public string CanForm { get; set; } = "";
    public string CadaFreUso { get; set; } = "";
    public string CodFreUso { get; set; } = "";
    public string CodPerDurTrat { get; set; } = "";
    public string Cant { get; set; } = "";
    public string CantTotal { get; set; } = "";
}
