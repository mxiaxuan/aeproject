using System;
using System.Collections.Generic;

namespace aeproject.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string? ShippingAddress { get; set; }
    
    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ContactPhone { get; set; }

    public string? Payment_method { get; set; }

    public string? Orderer_name { get; set; }

    public string? Orderer_phone { get; set; }

    public string? Orderer_address { get; set; }

    public DateOnly? Orderer_birthday { get; set; }

    public string? Order_notes { get; set; }

    public string? Invoice_type { get; set; }

    public string? Vehicle_type { get; set; }

    public string? Phone_barcode { get; set; }

    public string? Tax_id { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;
}
