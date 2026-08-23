using MareaBrava.Application.DTOs;
using MareaBrava.Application.Interfaces;
using MareaBrava.Domain.Entities;

namespace MareaBrava.Application.Services;

public class PrendaService : IPrendaService
{
    private readonly IPrendaRepository _prendaRepository;

    public PrendaService(IPrendaRepository prendaRepository)
    {
        _prendaRepository = prendaRepository;
    }

    public async Task<IEnumerable<PrendaDto>> ObtenerCatalogoAsync()
    {
        var prendas = await _prendaRepository.ObtenerTodasAsync();
        return prendas.Select(MapearADto);
    }

    public async Task<PrendaDto?> ObtenerPorIdAsync(int id)
    {
        var prenda = await _prendaRepository.ObtenerPorIdAsync(id);
        return prenda == null ? null : MapearADto(prenda);
    }

    public async Task<PrendaDto?> ObtenerPorSkuAsync(string sku)
    {
        var prenda = await _prendaRepository.ObtenerPorSkuAsync(sku);
        return prenda == null ? null : MapearADto(prenda);
    }

    public async Task<IEnumerable<PrendaDto>> ObtenerPrendasBajoStockAsync()
    {
        var prendas = await _prendaRepository.ObtenerBajoStockAsync();
        return prendas.Select(MapearADto);
    }

    public async Task<PrendaDto> CrearPrendaAsync(CrearPrendaDto dto)
    {
        var existente = await _prendaRepository.ObtenerPorSkuAsync(dto.Sku);
        if (existente != null)
        {
            throw new InvalidOperationException($"Ya existe una prenda registrada con el SKU '{dto.Sku}'.");
        }

        var nuevaPrenda = new Prenda
        {
            Sku = dto.Sku.Trim().ToUpperInvariant(),
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            TipoPieza = dto.TipoPieza,
            Talla = dto.Talla,
            Color = dto.Color.Trim(),
            PrecioCosto = dto.PrecioCosto,
            PrecioVenta = dto.PrecioVenta,
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo,
            ImagenUrl = dto.ImagenUrl?.Trim()
        };

        var creada = await _prendaRepository.AgregarAsync(nuevaPrenda);
        return MapearADto(creada);
    }

    public async Task ActualizarPrendaAsync(int id, CrearPrendaDto dto)
    {
        var prenda = await _prendaRepository.ObtenerPorIdAsync(id);
        if (prenda == null)
        {
            throw new KeyNotFoundException($"No se encontró la prenda con ID {id}.");
        }

        var duplicadoSku = await _prendaRepository.ObtenerPorSkuAsync(dto.Sku);
        if (duplicadoSku != null && duplicadoSku.Id != id)
        {
            throw new InvalidOperationException($"El SKU '{dto.Sku}' ya está en uso por otra prenda.");
        }

        prenda.Sku = dto.Sku.Trim().ToUpperInvariant();
        prenda.Nombre = dto.Nombre.Trim();
        prenda.Descripcion = dto.Descripcion?.Trim();
        prenda.TipoPieza = dto.TipoPieza;
        prenda.Talla = dto.Talla;
        prenda.Color = dto.Color.Trim();
        prenda.PrecioCosto = dto.PrecioCosto;
        prenda.PrecioVenta = dto.PrecioVenta;
        prenda.StockActual = dto.StockActual;
        prenda.StockMinimo = dto.StockMinimo;
        prenda.ImagenUrl = dto.ImagenUrl?.Trim();

        await _prendaRepository.ActualizarAsync(prenda);
    }

    public async Task EliminarPrendaAsync(int id)
    {
        await _prendaRepository.EliminarAsync(id);
    }

    private static PrendaDto MapearADto(Prenda p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        TipoPieza = p.TipoPieza,
        Talla = p.Talla,
        Color = p.Color,
        PrecioCosto = p.PrecioCosto,
        PrecioVenta = p.PrecioVenta,
        StockActual = p.StockActual,
        StockMinimo = p.StockMinimo,
        ImagenUrl = p.ImagenUrl
    };
}