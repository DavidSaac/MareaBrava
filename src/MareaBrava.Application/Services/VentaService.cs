using MareaBrava.Application.DTOs;
using MareaBrava.Application.Interfaces;
using MareaBrava.Domain.Entities;

namespace MareaBrava.Application.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IPrendaRepository _prendaRepository;

    public VentaService(IVentaRepository ventaRepository, IPrendaRepository prendaRepository)
    {
        _ventaRepository = ventaRepository;
        _prendaRepository = prendaRepository;
    }

    public async Task<IEnumerable<VentaDto>> ObtenerHistorialVentasAsync()
    {
        var ventas = await _ventaRepository.ObtenerTodasAsync();
        return ventas.Select(MapearADto);
    }

    public async Task<VentaDto?> ObtenerPorIdAsync(int id)
    {
        var venta = await _ventaRepository.ObtenerPorIdAsync(id);
        return venta == null ? null : MapearADto(venta);
    }

    public async Task<VentaDto?> ObtenerPorTicketAsync(string numeroTicket)
    {
        var venta = await _ventaRepository.ObtenerPorTicketAsync(numeroTicket);
        return venta == null ? null : MapearADto(venta);
    }

    public async Task<IEnumerable<VentaDto>> ObtenerVentasPorFechaAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var ventas = await _ventaRepository.ObtenerPorRangoFechasAsync(fechaInicio, fechaFin);
        return ventas.Select(MapearADto);
    }

    public async Task<VentaDto> ProcesarVentaAsync(RegistrarVentaDto dto)
    {
        if (dto.Lineas == null || !dto.Lineas.Any())
        {
            throw new InvalidOperationException("No se pueden procesar ventas sin artículos.");
        }

        var nuevaVenta = new Venta
        {
            UsuarioId = dto.UsuarioId,
            MetodoPago = dto.MetodoPago,
            FechaVenta = DateTime.UtcNow,
            Total = 0
        };

        // Generar número de ticket único correlativo
        var conteoHoy = await _ventaRepository.ObtenerConteoVentasHoyAsync();
        nuevaVenta.NumeroTicket = $"TCK-{DateTime.UtcNow:yyyyMMdd}-{(conteoHoy + 1):D4}";

        foreach (var linea in dto.Lineas)
        {
            var prenda = await _prendaRepository.ObtenerPorIdAsync(linea.PrendaId);
            if (prenda == null)
            {
                throw new KeyNotFoundException($"Prenda con ID {linea.PrendaId} no encontrada.");
            }

            if (prenda.StockActual < linea.Cantidad)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{prenda.Nombre}'. Disponible: {prenda.StockActual}, Solicitado: {linea.Cantidad}");
            }

            // Descontar inventario
            prenda.StockActual -= linea.Cantidad;
            await _prendaRepository.ActualizarAsync(prenda);

            var detalle = new DetalleVenta
            {
                PrendaId = prenda.Id,
                Cantidad = linea.Cantidad,
                PrecioUnitario = prenda.PrecioVenta,
                Prenda = prenda
            };

            nuevaVenta.Detalles.Add(detalle);
            nuevaVenta.Total += detalle.PrecioUnitario * detalle.Cantidad;
        }

        var ventaGuardada = await _ventaRepository.RegistrarVentaAsync(nuevaVenta);
        return MapearADto(ventaGuardada);
    }

    private static VentaDto MapearADto(Venta v) => new()
    {
        Id = v.Id,
        NumeroTicket = v.NumeroTicket,
        FechaVenta = v.FechaVenta,
        MetodoPago = v.MetodoPago,
        Total = v.Total,
        UsuarioId = v.UsuarioId,
        Detalles = v.Detalles.Select(d => new DetalleVentaDto
        {
            PrendaId = d.PrendaId ?? 0,
            NombrePrenda = d.Prenda?.Nombre ?? string.Empty,
            SkuPrenda = d.Prenda?.Sku ?? string.Empty,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Cantidad * d.PrecioUnitario
        }).ToList()
    };
}