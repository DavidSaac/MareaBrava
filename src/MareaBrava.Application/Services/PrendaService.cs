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

    public async Task<IEnumerable<Prenda>> ObtenerCatalogoActivoAsync()
    {
        return await _prendaRepository.ObtenerTodosAsync();
    }

    public async Task<Prenda?> ObtenerPorIdAsync(int id)
    {
        return await _prendaRepository.ObtenerPorIdAsync(id);
    }

    public async Task<IEnumerable<Prenda>> ObtenerAlertasBajoStockAsync()
    {
        return await _prendaRepository.ObtenerBajoStockAsync();
    }

    public async Task<Prenda> RegistrarNuevaPrendaAsync(CrearPrendaDto dto)
    {
        var skuExistente = await _prendaRepository.ObtenerPorSkuAsync(dto.Sku);
        if (skuExistente != null)
        {
            throw new InvalidOperationException($"El SKU '{dto.Sku}' ya está registrado.");
        }

        var prenda = new Prenda
        {
            Sku = dto.Sku.ToUpperInvariant(),
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            TipoPieza = dto.TipoPieza,
            Talla = dto.Talla,
            Color = dto.Color,
            PrecioCosto = dto.PrecioCosto,
            PrecioVenta = dto.PrecioVenta,
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo,
            ImagenUrl = dto.ImagenUrl,
            Activo = true
        };

        return await _prendaRepository.AgregarAsync(prenda);
    }

    public async Task<Prenda> ActualizarPrendaAsync(int id, CrearPrendaDto dto)
    {
        var prenda = await _prendaRepository.ObtenerPorIdAsync(id);
        if (prenda == null)
        {
            throw new InvalidOperationException("La prenda solicitada no existe.");
        }

        prenda.Nombre = dto.Nombre;
        prenda.Descripcion = dto.Descripcion;
        prenda.TipoPieza = dto.TipoPieza;
        prenda.Talla = dto.Talla;
        prenda.Color = dto.Color;
        prenda.PrecioCosto = dto.PrecioCosto;
        prenda.PrecioVenta = dto.PrecioVenta;
        prenda.StockActual = dto.StockActual;
        prenda.StockMinimo = dto.StockMinimo;
        if (!string.IsNullOrEmpty(dto.ImagenUrl))
        {
            prenda.ImagenUrl = dto.ImagenUrl;
        }

        await _prendaRepository.ActualizarAsync(prenda);
        return prenda;
    }

    public async Task<bool> DarDeBajaPrendaAsync(int id)
    {
        var prenda = await _prendaRepository.ObtenerPorIdAsync(id);
        if (prenda == null) return false;

        prenda.Activo = false;
        await _prendaRepository.ActualizarAsync(prenda);
        return true;
    }
}