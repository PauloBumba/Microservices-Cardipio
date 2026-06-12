namespace Product.Domain.Exceptions;
public sealed class InsufficientStockException(Guid id, int requested, int available)
    : Exception($"Estoque insuficiente para {id}. Solicitado: {requested}, disponível: {available}.");
