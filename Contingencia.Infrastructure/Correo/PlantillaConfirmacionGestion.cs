namespace Contingencia.Infrastructure.Correo;

// Versión modernizada/simplificada del cuerpo de correo embebido en crearcorreo()
// (JuntasFormatoCntCO2.aspx.vb, tras Guardar en el modal de gestión) — mismo criterio que
// PlantillaRecuperacionClave: mismo mensaje real, misma paleta, sin copiar los ~400 líneas
// de boilerplate de compatibilidad con Outlook/MSO del legado.
public static class PlantillaConfirmacionGestion
{
    public static string Generar(string radicado, string estado, string noPrescripcion) =>
        Plantilla
            .Replace("{{RADICADO}}", radicado)
            .Replace("{{ESTADO}}", estado)
            .Replace("{{NOPRESCRIPCION}}", noPrescripcion)
            .Replace("{{ANIO}}", DateTime.Now.Year.ToString());

    private const string Plantilla = """
        <!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional //EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'><html xmlns='http://www.w3.org/1999/xhtml'><head>
          <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
          <meta name='viewport' content='width=device-width'>
          <title>Template Base</title>
        </head>
        <body style='width: 100% !important;min-width: 100%;-webkit-text-size-adjust: 100%;-ms-text-size-adjust: 100% !important;margin: 0;padding: 0;background-color: #FFFFFF'>
          <table cellpadding='0' cellspacing='0' width='100%' class='body' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;height: 100%;width: 100%;table-layout: fixed'>
              <tbody><tr style='vertical-align: top'>
                  <td class='center' align='center' valign='top' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;text-align: center;background-color: #FFFFFF'>
                      <table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                        <tbody><tr style='vertical-align: top'>
                          <td width='100%' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: #F3FDFF'>
                            <table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' class='container' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;max-width: 500px;margin: 0 auto;text-align: inherit'><tbody><tr style='vertical-align: top'><td width='100%' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top'><table cellpadding='0' cellspacing='0' width='100%' bgcolor='#F3FDFF' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;width: 100%;max-width: 500px;color: #000000;background-color: #F3FDFF'><tbody><tr style='vertical-align: top'><td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: #F3FDFF;text-align: center;font-size: 0'><div style='display: inline-block;vertical-align: top;width: 100%'><table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'><tbody><tr style='vertical-align: top'><td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: transparent;padding: 0'><table cellpadding='0' cellspacing='0' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                                <tbody><tr style='vertical-align: top'>
                                    <td align='center' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;width: 100%;padding: 0'>
                                        <div align='center' style='font-size:12px'>
                                           <img class='center fullwidth' align='center' width='100' border='0' src='https://dqualitysolutions.com.co/assets/imperium-logo.png' alt='Image' title='Image' style='outline: none;text-decoration: none;-ms-interpolation-mode: bicubic;clear: both;display: block;border: 0;height: auto;line-height: 100%;margin: 0 auto;float: none;width: 100% !important;max-width: 200px' width='200'> </div>
                                    </td>
                                </tr>
                            </tbody></table>
                          </td></tr></tbody></table></div></td></tr></tbody></table></td></tr></tbody></table>
                          </td></tr></tbody></table>
                      <table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                        <tbody><tr style='vertical-align: top'>
                          <td width='100%' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: #124F69'>
                            <table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' class='container' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;max-width: 500px;margin: 0 auto;text-align: inherit'><tbody><tr style='vertical-align: top'><td width='100%' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top'><table cellpadding='0' cellspacing='0' width='100%' bgcolor='transparent' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;width: 100%;max-width: 500px;color: #000000;background-color: transparent'><tbody><tr style='vertical-align: top'><td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: transparent;text-align: center;font-size: 0'><div style='display: inline-block;vertical-align: top;width: 100%'><table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'><tbody><tr style='vertical-align: top'><td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: transparent;padding: 0'><table cellpadding='0' cellspacing='0' width='100%' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                              <tbody><tr style='vertical-align: top'>
                                <td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;padding-top: 20px;padding-right: 0px;padding-bottom: 10px;padding-left: 0px'>
                                  <div style='color:#FFFFFF;line-height:120%;font-family:Trebuchet MS, Lucida Grande, Lucida Sans Unicode, Lucida Sans, Tahoma, sans-serif;'>
                                    <div style='font-size:12px;line-height:14px;color:#FFFFFF;text-align:left;'><p style='margin: 0;font-size: 14px;line-height: 17px;text-align: center'><span style='font-size: 26px; line-height: 31px;'><strong>Respuesta de formato de contingencia</strong></span></p></div>
                                  </div>
                                </td>
                              </tr>
                            </tbody></table>
                          </td></tr></tbody></table></div></td></tr></tbody></table></td></tr></tbody></table>
                          </td></tr></tbody></table>
                      <table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                        <tbody><tr style='vertical-align: top'>
                          <td width='100%' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: #F6FCFF'>
                            <table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' class='container' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;max-width: 500px;margin: 0 auto;text-align: inherit'><tbody><tr style='vertical-align: top'><td width='100%' style='word-break: break-word;border-collapse: collapse !important;vertical-align: top'><table cellpadding='0' cellspacing='0' width='100%' bgcolor='transparent' style='border-spacing: 0;border-collapse: collapse;vertical-align: top;width: 100%;max-width: 500px;color: #333;background-color: transparent'><tbody><tr style='vertical-align: top'><td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: transparent;text-align: center;font-size: 0'><div style='display: inline-block;vertical-align: top;width: 100%'><table cellpadding='0' cellspacing='0' align='center' width='100%' border='0' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'><tbody><tr style='vertical-align: top'><td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;background-color: transparent;padding-top: 20px;padding-bottom: 0;padding-left:0;padding-right:0'><table cellpadding='0' cellspacing='0' width='100%' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                              <tbody><tr style='vertical-align: top'>
                                <td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;padding-top: 0px;padding-right: 10px;padding-bottom: 15px;padding-left: 10px'>
                                  <div style='color:#092835;line-height:150%;font-family:Trebuchet MS, Lucida Grande, Lucida Sans Unicode, Lucida Sans, Tahoma, sans-serif;'>
                                    <div style='font-size:12px;line-height:18px;color:#092835;text-align:left;'><p style='margin: 0;font-size: 12px;line-height: 18px;text-align: center'><span style='font-size: 14px; line-height: 21px;'>Su radicado: {{RADICADO}} ha sido <strong>{{ESTADO}}</strong>. No Prescripción: {{NOPRESCRIPCION}}. Para validar los detalles ingrese al aplicativo Imperium.</span></p></div>
                                  </div>
                                </td>
                              </tr>
                            </tbody></table>
                          </td></tr></tbody></table></div></td></tr></tbody></table></td></tr></tbody></table>
                          </td></tr></tbody></table>
                      <table cellpadding='0' cellspacing='0' width='100%' style='border-spacing: 0;border-collapse: collapse;vertical-align: top'>
                        <tbody><tr style='vertical-align: top'>
                          <td style='word-break: break-word;border-collapse: collapse !important;vertical-align: top;padding-top: 15px;padding-right: 10px;padding-bottom: 10px;padding-left: 10px'>
                            <div style='color:#959595;line-height:150%;font-family:Arial, Helvetica Neue, Helvetica, sans-serif;'>
                              <div style='font-size:12px;line-height:18px;color:#959595;text-align:left;'><p style='margin: 0;font-size: 14px;line-height: 21px;text-align: center'><span style='background-color: transparent; font-size: 14px; line-height: 21px;'>© {{ANIO}} DQS MIPRES. All rights reserved | Design by DATA QUALITY SOLUTIONS &nbsp;</span><br></p></div>
                            </div>
                          </td>
                        </tr>
                      </tbody></table>
                  </td>
              </tr>
          </tbody></table>
        </body></html>
        """;
}
