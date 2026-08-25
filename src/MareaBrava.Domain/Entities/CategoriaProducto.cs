namespace MareaBrava.Domain.Entities;

public class CategoriaProducto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Prenda> Prendas { get; set; } = new List<Prenda>();
}