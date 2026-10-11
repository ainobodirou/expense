namespace ExpenseApi.Models;
using ExpenseApi.Enums;

public class ExpenseItemDTO
{
    public long Id { get; set;}
    public string PurchaseName { get; set;}
    public DateTime PurchaseDate {get; set;}
    public PurchaseCategory Category {get; set;}

    public decimal value {get; set;}
}
