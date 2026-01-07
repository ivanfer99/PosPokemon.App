namespace PosPokemon.App.Models;

public enum PaymentMethod
{
    Cash,
    Card,
    Yape,
    Plin,
    BankTransfer,
    Multiple
}

public static class PaymentMethodExtensions
{
    public static string ToDisplayString(this PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "Efectivo",
            PaymentMethod.Card => "Tarjeta",
            PaymentMethod.Yape => "Yape",
            PaymentMethod.Plin => "Plin",
            PaymentMethod.BankTransfer => "Transferencia",
            PaymentMethod.Multiple => "Mixto",
            _ => "Desconocido"
        };
    }

    public static PaymentMethod FromString(string value)
    {
        return value switch
        {
            "Efectivo" => PaymentMethod.Cash,
            "Tarjeta" => PaymentMethod.Card,
            "Yape" => PaymentMethod.Yape,
            "Plin" => PaymentMethod.Plin,
            "Transferencia" => PaymentMethod.BankTransfer,
            "Mixto" => PaymentMethod.Multiple,
            _ => PaymentMethod.Cash
        };
    }
}