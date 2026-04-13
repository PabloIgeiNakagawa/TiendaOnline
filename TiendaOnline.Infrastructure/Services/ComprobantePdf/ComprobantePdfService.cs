using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.DTOs;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Infrastructure.Services.ComprobantePdf
{
    public class ComprobantePdfService : IComprobantePdfService
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public ComprobantePdfService(IAppSettingsService appSettings, IHttpClientFactory httpClientFactory)
        {
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<byte[]> GenerarComprobanteAsync(ComprobantePedidoDto datos)
        {
            // Leer datos de la empresa desde AppSettings
            var razonSocial = _appSettings.GetValue("Empresa:RazonSocial");
            var cuit = _appSettings.GetValue("Empresa:Cuit");
            var condicionIva = _appSettings.GetValue("Empresa:CondicionIva");
            var ingresosBrutos = _appSettings.GetValue("Empresa:IngresosBrutos");
            var inicioActividades = _appSettings.GetValue("Empresa:InicioActividades");
            var logoUrl = _appSettings.GetValue("Diseno:LogoUrl");

            // Cargar logo si existe
            Image? logoImage = null;
            if (!string.IsNullOrEmpty(logoUrl))
            {
                try
                {
                    if (logoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        using var httpClient = _httpClientFactory.CreateClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(10);

                        var response = await httpClient.GetAsync(logoUrl, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                        if (!contentType.StartsWith("image/"))
                        {
                            logoImage = null;
                        }
                        else
                        {
                            var contentLength = response.Content.Headers.ContentLength;
                            if (contentLength.HasValue && contentLength > 2 * 1024 * 1024)
                            {
                                logoImage = null;
                            }
                            else
                            {
                                var bytes = await response.Content.ReadAsByteArrayAsync();
                                logoImage = Image.FromBinaryData(bytes);
                            }
                        }
                    }
                    else if (File.Exists(logoUrl))
                    {
                        logoImage = Image.FromFile(logoUrl);
                    }
                }
                catch
                {
                    logoImage = null;
                }
            }

            // Generar QR como PNG
            var qrData = $"Pedido #{datos.PedidoId:D6} | Total: ${datos.Total:N2} | {datos.NumeroComprobante}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrImageBytes = qrCode.GetGraphic(20);
            var qrImage = Image.FromBinaryData(qrImageBytes);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Repeat().Element(Encabezado);
                    page.Content().Element(c => Cuerpo(c, datos));
                    page.Footer().Repeat().Element(PiePagina);
                });
            });

            return document.GeneratePdf();

            void Encabezado(IContainer container)
            {
                container.Row(row =>
                {
                    // Columna izquierda: Logo + Nombre empresa
                    row.RelativeItem(2).Column(col =>
                    {
                        if (logoImage != null)
                        {
                            col.Item().Row(logoRow =>
                            {
                                logoRow.AutoItem().PaddingRight(10).Container().Width(60).Height(60).Image(logoImage).FitArea();
                                logoRow.RelativeItem().Column(nombreCol =>
                                {
                                    if (!string.IsNullOrEmpty(razonSocial))
                                    {
                                        nombreCol.Item().Text(razonSocial)
                                            .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                                    }
                                    nombreCol.Item().Text("Comprobante de Pago")
                                        .FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2);
                                });
                            });
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(razonSocial))
                            {
                                col.Item().Text(razonSocial)
                                    .FontSize(22).Bold().FontColor(Colors.Blue.Medium);
                            }
                            col.Item().Text("Comprobante de Pago")
                                .FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2);
                        }
                    });

                    // Columna derecha: Datos fiscales + comprobante (solo si existen)
                    row.RelativeItem(2).AlignRight().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(cuit))
                            col.Item().Text($"CUIT: {cuit}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrEmpty(condicionIva))
                            col.Item().Text(condicionIva)
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrEmpty(ingresosBrutos))
                            col.Item().Text($"Ingresos Brutos: {ingresosBrutos}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrEmpty(inicioActividades))
                            col.Item().Text($"Inicio de Actividades: {inicioActividades}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);

                        col.Item().PaddingTop(5).Text(datos.NumeroComprobante)
                            .FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Emisi\u00f3n: {datos.FechaEmision:dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }

            void PiePagina(IContainer container)
            {
                container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
                {
                    row.AutoItem().Width(50).Height(50).Image(qrImage);
                    row.RelativeItem().PaddingLeft(10).Column(col =>
                    {
                        col.Item().Text("Comprobante interno \u2014 No v\u00e1lido como factura")
                            .FontSize(8).Bold().FontColor(Colors.Red.Medium);
                        col.Item().Text($"Generado el {datos.FechaEmision:dd/MM/yyyy HH:mm} | Pedido #{datos.PedidoId:D6}")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                    row.AutoItem().AlignRight().Text(t =>
                    {
                        t.Span("P\u00e1gina ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }

        void Cuerpo(IContainer container, ComprobantePedidoDto datos)
        {
            container.Column(col =>
            {
                col.Item().PaddingVertical(15).Element(InfoCliente);
                col.Item().PaddingBottom(10).Element(InfoPago);
                col.Item().PaddingVertical(10).Element(TablaProductos);
                col.Item().PaddingTop(10).Element(Totales);
            });

            void InfoCliente(IContainer container)
            {
                container.Background(Colors.Grey.Lighten4).Padding(15).Column(c =>
                {
                    c.Item().Text("Datos del Cliente").FontSize(12).Bold();
                    c.Item().PaddingTop(5).Text($"Nombre: {datos.UsuarioNombre}").FontSize(10);
                    c.Item().Text($"Email: {datos.UsuarioEmail}").FontSize(10);
                    c.Item().Text($"Tel\u00e9fono: {datos.UsuarioTelefono}").FontSize(10);
                    c.Item().Text($"Direcci\u00f3n: {datos.DireccionCompleta ?? "Retiro en local"}").FontSize(10);
                });
            }

            void InfoPago(IContainer container)
            {
                container.Background(Colors.Green.Lighten5).Padding(15).Column(c =>
                {
                    c.Item().Text("Informaci\u00f3n del Pago").FontSize(12).Bold();
                    c.Item().PaddingTop(5).Text($"M\u00e9todo: {datos.MetodoPago}").FontSize(10);
                    c.Item().Text($"Estado: {((EstadoPago)datos.EstadoPagoId).ToString()}").FontSize(10).Bold();
                    if (!string.IsNullOrEmpty(datos.TransaccionPagoId))
                    {
                        c.Item().Text($"Transacci\u00f3n: {datos.TransaccionPagoId}").FontSize(9).FontColor(Colors.Grey.Medium);
                    }
                });
            }

            void TablaProductos(IContainer container)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Medium).Padding(8)
                            .Text("Producto").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Medium).Padding(8)
                            .Text("Cant.").FontSize(10).Bold().FontColor(Colors.White).AlignCenter();
                        header.Cell().Background(Colors.Blue.Medium).Padding(8)
                            .Text("P. Unitario").FontSize(10).Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Blue.Medium).Padding(8)
                            .Text("Subtotal").FontSize(10).Bold().FontColor(Colors.White).AlignRight();
                    });

                    for (int i = 0; i < datos.Items.Count; i++)
                    {
                        var item = datos.Items[i];
                        var colorFila = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        table.Cell().Background(colorFila).Padding(8)
                            .Text(item.ProductoNombre).FontSize(9);
                        table.Cell().Background(colorFila).Padding(8)
                            .Text(item.Cantidad.ToString()).FontSize(9).AlignCenter();
                        table.Cell().Background(colorFila).Padding(8)
                            .Text($"$ {item.PrecioUnitario:N2}").FontSize(9).AlignRight();
                        table.Cell().Background(colorFila).Padding(8)
                            .Text($"$ {item.Subtotal:N2}").FontSize(9).Bold().AlignRight();
                    }
                });
            }

            void Totales(IContainer container)
            {
                container.AlignRight().Column(c =>
                {
                    c.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal:").FontSize(11);
                        row.RelativeItem().Text($"$ {datos.Subtotal:N2}").FontSize(11).AlignRight();
                    });
                    c.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Env\u00edo:").FontSize(11);
                        row.RelativeItem().Text(datos.CostoEnvio == 0 ? "Gratis" : $"$ {datos.CostoEnvio:N2}")
                            .FontSize(11).AlignRight();
                    });
                    c.Item().PaddingTop(8).BorderTop(2).BorderColor(Colors.Blue.Medium).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL:").FontSize(14).Bold();
                        row.RelativeItem().Text($"$ {datos.Total:N2}").FontSize(14).Bold().AlignRight();
                    });
                });
            }
        }
    }
}
