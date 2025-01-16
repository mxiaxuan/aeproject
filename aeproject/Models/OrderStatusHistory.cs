using System;
using System.ComponentModel.DataAnnotations;

namespace aeproject.Models
{
    public class OrderStatusHistory
    {
        [Key] // 標註為主鍵
        public int Status_history_id { get; set; }

        public int OrderId { get; set; }

        public string? Status { get; set; }

        public DateTime? Status_date { get; set; }

        public string? Comments { get; set; }
    }
}
