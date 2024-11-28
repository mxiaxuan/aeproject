using System;
using System.Collections.Generic;

namespace aeproject.Models;

public partial class CartItem
{
    public int CartItemId { get; set; } // Primary key

    public int CartId { get; set; } // Foreign key to Cart

    public int ProductId { get; set; } // Foreign key to Product

    public int Quantity { get; set; }

    public DateTime? AddedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Cart Cart { get; set; } = null!; // Navigation property to Cart

    public virtual Product Product { get; set; } = null!; // Navigation property to Product

    // 總價（計算屬性）
    public decimal Total => Product.Price * Quantity;

}

