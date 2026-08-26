using MareaBrava.Domain.Enums;

namespace MareaBrava.Domain.Entities;

public class CategoriaProducto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    // TipoPieza interno de la categoría: se asigna automáticamente a las prendas creadas con esta categoría.
    public TipoPieza TipoPieza { get; set; } = TipoPieza.Accesorio;
    public ICollection<Prenda> Prendas { get; set; } = new List<Prenda>();
}