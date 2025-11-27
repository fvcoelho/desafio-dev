namespace DesafioDev.Api.Models.Entities;

/// <summary>
/// Represents the type of financial transaction in the CNAB file
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Débito - Income (+)
    /// </summary>
    Debit = 1,

    /// <summary>
    /// Boleto - Expense (-)
    /// </summary>
    Boleto = 2,

    /// <summary>
    /// Financiamento - Expense (-)
    /// </summary>
    Financing = 3,

    /// <summary>
    /// Crédito - Income (+)
    /// </summary>
    Credit = 4,

    /// <summary>
    /// Recebimento Empréstimo - Income (+)
    /// </summary>
    LoanReceipt = 5,

    /// <summary>
    /// Vendas - Income (+)
    /// </summary>
    Sales = 6,

    /// <summary>
    /// Recebimento TED - Income (+)
    /// </summary>
    TedReceipt = 7,

    /// <summary>
    /// Recebimento DOC - Income (+)
    /// </summary>
    DocReceipt = 8,

    /// <summary>
    /// Aluguel - Expense (-)
    /// </summary>
    Rent = 9
}

/// <summary>
/// Extension methods for TransactionType business rules
/// </summary>
public static class TransactionTypeExtensions
{
    /// <summary>
    /// Determines if the transaction type represents income (positive value)
    /// </summary>
    public static bool IsIncome(this TransactionType type) =>
        type is TransactionType.Debit or
                TransactionType.Credit or
                TransactionType.LoanReceipt or
                TransactionType.Sales or
                TransactionType.TedReceipt or
                TransactionType.DocReceipt;

    /// <summary>
    /// Determines if the transaction type represents an expense (negative value)
    /// </summary>
    public static bool IsExpense(this TransactionType type) => !type.IsIncome();

    /// <summary>
    /// Gets the description of the transaction type
    /// </summary>
    public static string GetDescription(this TransactionType type) => type switch
    {
        TransactionType.Debit => "Débito",
        TransactionType.Boleto => "Boleto",
        TransactionType.Financing => "Financiamento",
        TransactionType.Credit => "Crédito",
        TransactionType.LoanReceipt => "Recebimento Empréstimo",
        TransactionType.Sales => "Vendas",
        TransactionType.TedReceipt => "Recebimento TED",
        TransactionType.DocReceipt => "Recebimento DOC",
        TransactionType.Rent => "Aluguel",
        _ => "Unknown"
    };
}
